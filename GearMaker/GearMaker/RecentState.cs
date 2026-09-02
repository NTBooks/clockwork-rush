using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.Storage;

namespace Rotetris
{
     [DataContract(Name = "EngineState")]
    class RecentState 
    {
         [DataMember()]
        public readonly int Rows;

          [DataMember()]
         public readonly int Cols;

          [DataMember()]
          public readonly int Tick_ms;

          [DataMember()]
          public readonly int NumColorsUsed;

          [DataMember()]
          public readonly int StartRows;

          [DataMember()]
          public readonly bool GameEnded;

          [DataMember()]
          public readonly long Ticks;

          [DataMember()]
          public readonly string UserControllerBlockName;

          [DataMember()]
          public readonly List<Rotetris.Rotetris_Engine.ActiveBlock> FallingBlocks;

          [DataMember()]
          public readonly int topRowAllowed;

          [DataMember()]
          public readonly string FlatModel;

          [DataMember()]
          public readonly bool Initialized = false;

          [DataMember()]
          public readonly string StateName;


         
        private static volatile RecentState _instance;
        private static object syncRoot = new Object();

         // create new state
        public RecentState(string StateName, int Rows, int Cols, int StartRows, bool GameEnded, long Ticks, int Tick_ms, int NumColorsUsed, string UserControllerBlockName, List<Rotetris.Rotetris_Engine.ActiveBlock> FallingBlocks, int topRowAllowed, string FlatModel)
        {
            this.Rows = Rows;
            this.Cols = Cols;
            this.StartRows = StartRows;
            this.GameEnded = GameEnded;
            this.Ticks = Ticks;
            this.UserControllerBlockName = UserControllerBlockName;
            this.FallingBlocks = FallingBlocks;
            this.topRowAllowed = topRowAllowed;
            this.FlatModel = FlatModel;
            this.StateName = StateName;

            this.Tick_ms = Tick_ms;


            this.NumColorsUsed = NumColorsUsed;

            Initialized = true;

        }

         // copy engine state
        public RecentState(string StateName, Rotetris_Engine Eng)
        {
            this.Rows = Eng.Rows;
            this.Cols = Eng.Cols;
            this.StartRows = Eng.StartRows;
            this.GameEnded = Eng.GameHasEnded;
            this.Ticks = Eng.Ticks;
            this.UserControllerBlockName = Eng.UserBlockName;
            this.FallingBlocks = Eng.FallingBlocks;
            this.topRowAllowed = Eng.topRowAllowed;
            this.FlatModel = Eng.Z_FlatModel;
            this.StateName = StateName;

            this.Tick_ms = Eng.Tick_ms;


            this.NumColorsUsed = Eng.NumColorsUsed;

            Initialized = true;

        }

        private RecentState()
        {
            Initialized = false;
        }


        public async static Task<bool> ClearRecentState()
        {
            try
            {
                if (LastCreated.Initialized)
                {
                    StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(_instance.StateName);
                    await file.DeleteAsync();

                    _instance = null;

                    return true;
                }


                _instance = null;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return false;

            }
        }

        public static RecentState LastCreated
        {
            get
            {
                if (_instance == null)
                {
                    lock (syncRoot)
                    {
                        if (_instance == null)
                            _instance = new RecentState();
                    }
                }
                return _instance;
            }

            set
            {
               
                _instance = value;
                
            }

        }

        public async Task<bool> SaveState()
        {

            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(StateName, CreationCollisionOption.ReplaceExisting);

                using (Stream s = await file.OpenStreamForWriteAsync())
                {
                    DataContractSerializer ser = new DataContractSerializer(typeof(RecentState));

                    ser.WriteObject(s, this);

                    lock (syncRoot)
                    {
                        RecentState._instance = this;
                    }

                    await s.FlushAsync();

                    return true;

                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return false;

            }
        }

        public static async Task<RecentState> LoadState(string StateName)
        {

            if (LastCreated.Initialized && LastCreated.StateName.Equals(StateName))
            {
                return LastCreated;
            }
            else
            {
                try {
                    StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(StateName);

                    using (Stream s = await file.OpenStreamForReadAsync())
                    {

                        DataContractSerializer ser = new DataContractSerializer(typeof(RecentState));

                        using (XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(s, XmlDictionaryReaderQuotas.Max))
                        { 
                            var Eng = (RecentState)ser.ReadObject(reader, true);

                            lock (syncRoot)
                            {
                                LastCreated = Eng;
                            }

                            return LastCreated;

                        
                        }

                  
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    return new RecentState(); // initialized will be false

                }
            }
        }
                              
    }
}
