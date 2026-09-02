using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
//using System.Xml.Serialization;
using System.Runtime.Serialization;
using Windows.Storage;
using System.IO;
using System.Xml;
using Windows.UI.Popups;

namespace Rotetris
{


    public class RotetrisEventArgs : EventArgs
    {
        public readonly Point P; // Row, Col
        public readonly char[,] Contents;
        public readonly long Ticks;
        public readonly string ActivePieceName;

       
        public RotetrisEventArgs()
        {
            this.P = new Point(-1, -1);
            this.Contents = null;
            this.Ticks = -1;
        }


        public RotetrisEventArgs(Point P, char[,] Contents = null, long Ticks = -1, string Name="")
        {
            this.P = P; // Convert point into virtual grid point in the handler (internal was 2x2 squares)
            this.Contents = Contents;
            this.ActivePieceName = Name;
            this.Ticks = Ticks;
        }
    }

   [DataContract(Name="Engine")]
    public partial class Rotetris_Engine
    {

       [DataMember()]
        public long Ticks;

        public int MinRowValidPosition
        {
            get { return topRowAllowed; }
            set { ; }
        }

         [DataMember()]
        private bool GameEnded = false;

         public bool GameHasEnded
         {
             get { return GameEnded; }
         }


        // A delegate type for hooking up change notifications.
        //public delegate void ChangedEventHandler(object sender, EventArgs e);

        public event EventHandler<RotetrisEventArgs> GamePieceMoved; // done
        public event EventHandler<RotetrisEventArgs> GamePieceRemoved; // done
        public event EventHandler<RotetrisEventArgs> GamePieceAdded; // done
        public event EventHandler<RotetrisEventArgs> GamePieceMerged; // done
        
        
        public event EventHandler<RotetrisEventArgs> GameRotatedClockwise;
        public event EventHandler<RotetrisEventArgs> GameRotatedAnticlockwise;

        public event EventHandler<RotetrisEventArgs> GameWon; // done
        public event EventHandler<RotetrisEventArgs> GameLost; // done
        public event EventHandler<RotetrisEventArgs> GameStaticBlockRemoved;
        public event EventHandler<RotetrisEventArgs> GameStaticBlockAdded;
        public event EventHandler<RotetrisEventArgs> GameBoardChanged;
        public event EventHandler<RotetrisEventArgs> GameTick;


         [DataMember()]
        int Matches = 0;

        #region Initial Condition
         [DataMember()]
        public  readonly int Cols = 24;

         [DataMember()]
        public readonly int Rows = 26;

         [DataMember()]
         public readonly int StartRows = 3;

         [DataMember()]
         public readonly int Tick_ms = 1000;

       [DataMember()]
         public readonly string ColorAlphabet = "ABCDEFGHIJKLMNOPQSTUVWXY";

         [DataMember()]
         public readonly int NumColorsUsed = 4;

        #endregion

       
        char[,] Model;

        private string _FlatModel;

        [DataMember()]
        public string Z_FlatModel
        {
            get {

                if (Model != null)
                {
                    var retVal = new StringBuilder();
                    for (int i = 0; i < Rows; i++)
                    {
                        for (int j = 0; j < Cols; j++)
                        {
                            retVal.Append(Model[i, j]);
                        }
                    }
                    _FlatModel = retVal.ToString();
                }
                return _FlatModel;
            
            
            }
            set { _FlatModel = value; }
        }


        [DataMember()]
        public int topRowAllowed = 0;

       [DataContract(Name = "Block")]
        public class ActiveBlock
        {
             [DataMember()]
            public int posRow = 0;

             [DataMember()]
            public int posCol = 0;

            
            public char[,] block = new char[2, 2];

            private string _Pattern;

            [DataMember()]
            public string Pattern
            {
                get { 
                   return String.Format("{0}{1}{2}{3}", block[0,0], block[0,1], block[1,0], block[1,1]); }

                set {

                    block = new char[2, 2];

                        block[0, 0] = value[0];
                        block[0, 0 + 1] = value[1];
                        block[0 + 1, 0] = value[2];
                        block[0 + 1, 0 + 1] = value[3];

                        _Pattern = value;
                    

                }
            }

             [DataMember()]
            public string Name;

             [DataMember()]
            static public int Rows = 0;

             [DataMember()]
            static public int Cols = 0;


            public ActiveBlock(string Pattern)
            {
                //block[0, 0] = Pattern[0];
                //block[0, 0 + 1] = Pattern[1];
                //block[0 + 1, 0] = Pattern[2];
                //block[0 + 1, 0 + 1] = Pattern[3];

                this.Pattern = Pattern;

                this.Name = System.Guid.NewGuid().ToString();
            }

            public void Move(int offsetRow, int offsetCol)
            {
                if ((posCol + offsetCol < Cols - 1) && (posCol + offsetCol >= 0))
                {
                    posCol += offsetCol;
                }

                if ((posRow + offsetRow < Rows - 1) && (posRow + offsetRow >= 0))
                {
                    posRow += offsetRow;
                }


            }
        }

         [DataMember()]
        bool Running = false;

        DispatcherTimer EngineTimer = null;

        // Refactor this out later if possible and use the array and function parameters
        //ActiveBlock CurrentBlock = null;

        [DataMember()]
        ActiveBlock UserControllerBlock = null;

        public string UserBlockName
        {
            get { return UserControllerBlock != null ? UserControllerBlock.Name : null; }
        }

        [DataMember()]
        public List<ActiveBlock> FallingBlocks = new List<ActiveBlock>();

        //public async void SaveState()
        //{

        //    return; // Disabled for now
          
        //    bool cleanup = false;

        //    //try
        //    //{
        //        StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("SaveState.xml", CreationCollisionOption.ReplaceExisting);

        //        using (Stream s = await file.OpenStreamForWriteAsync())
        //        {
        //            DataContractSerializer ser = new DataContractSerializer(typeof(Rotetris_Engine));

        //            ser.WriteObject(s, this);

        //            System.IO.MemoryStream TempStream = new MemoryStream();

        //            // Store the state in memory so we can read it back while the program still runs.
        //            ser.WriteObject(TempStream, this);

        //            TempStream.Flush();

        //            GearMaker.RecentState.LastSavedState = TempStream;

        //            s.Flush();
        //            s.Dispose();
        //        }

               
        //    //}
        //    //catch (SerializationException sex)
        //    //{
        //    //    // Couldn't save state... it's just a game, so oh well
        //    //    cleanup = true;
        //    //    System.Diagnostics.Debug.WriteLine(sex.ToString());
        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    // File exception or some other such thing, don't worry about it

        //    //    System.Diagnostics.Debug.WriteLine(ex.ToString());
        //    //}

        //    //try
        //    //{
        //    //    // Delete the file if the exception was based on the serializer
        //    //    if (cleanup)
        //    //    {
        //    //        var V = await ApplicationData.Current.LocalFolder.GetFileAsync("SaveState.xml");
        //    //        await V.DeleteAsync(StorageDeleteOption.PermanentDelete);
        //    //    }
        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    // File exception or some other such thing, don't worry about it

        //    //    System.Diagnostics.Debug.WriteLine(ex.ToString());
        //    //}
                
        //        //await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///EngineSaveState.xml"));
        //   // var stream = await file.OpenAsync(FileAccessMode.ReadWrite);

        ////BUG: Stream isn't closing for some reason
         

        //}

        //public static async Task<Stream> GetStateFile(string LoadState)
        //{


        //    Stream file;
        //    //try
        //    //{
        //        using (file = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(LoadState))
        //        {


        //            if (file.Length > 0)
        //                return file;
        //        }

        //        return null;
        //    //}
        //    //catch (Exception fnf)
        //    //{
        //    //   return null;
        //    //}

           


        //    //return null;

        //    //try
        //    //{
        //    //    var v = await ApplicationData.Current.LocalFolder.GetFilesAsync();
               

        //    //    foreach (var f in v) {
        //    //        if (f.Name.ToUpper().Equals(LoadState.ToUpper())) {
        //    //            var g = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(LoadState);
        //    //            return g;

        //    //        }
        //    //    }
                
                
                
               
        //    //}
        //    //catch
        //    //{
        //    //    // File IO Error
        //    //    return null;
        //    //}

        //    //return null;
            

        //}

        public static async Task<Rotetris_Engine> LoadEngine(string LoadState)
        {

            var Recent = await Rotetris.RecentState.LoadState(LoadState);

          

            var Eng = Recent;

            

            if (Eng.Initialized)
            {

                var NewEng = new Rotetris_Engine(Eng.Rows, Eng.Cols, Eng.StartRows, Eng.Tick_ms, Eng.NumColorsUsed);
                //this.Rows = Eng.Rows;
                //this.Cols = Eng.Cols;


                //this.StartRows = Eng.StartRows;

                //this.Tick_ms = Eng.Tick_ms;


                //this.NumColorsUsed = Eng.NumColorsUsed;

                NewEng.Model = new char[Eng.Rows, Eng.Cols];


                NewEng.GameEnded = Eng.GameEnded;
                NewEng.Ticks = Eng.Ticks;


                NewEng.FallingBlocks = Eng.FallingBlocks;

                for (int i = 0; i < Eng.FallingBlocks.Count; i++)
                {
                    if (Eng.FallingBlocks[i].Name.Equals(Eng.UserControllerBlockName))
                        NewEng.UserControllerBlock = Eng.FallingBlocks[i];
                }


                NewEng.topRowAllowed = Eng.topRowAllowed;


                NewEng.Z_FlatModel = Eng.FlatModel;

                if (NewEng.Model == null)
                    throw new Exception("Model Null");

                for (int i = 0; i < Eng.Rows; i++)
                {
                    for (int j = 0; j < Eng.Cols; j++)
                    {
                        NewEng.Model[i, j] = Eng.FlatModel[i * Eng.Cols + j];




                    }
                }

                return NewEng;
            }

            return null;
            
        }


        async void Message(string Mess)
        {
            
                        MessageDialog dlg = new MessageDialog(Mess);
                        await dlg.ShowAsync();
        }
       


        // States are falling, settled, matching

        // Falling: Update position, check for collisions, adjust

        // Settled: Check for matches, create new block

        // pattern is: top left, top right, bottom left, bottom right
        void AddBlock(int Row, int Col, string Pattern)
        {
            Model[Row, Col] = Pattern[0];
            Model[Row, Col + 1] = Pattern[1];
            Model[Row + 1, Col] = Pattern[2];
            Model[Row + 1, Col + 1] = Pattern[3];
            Fire(GameStaticBlockAdded, new RotetrisEventArgs(new Point(Row, Col), new char[2, 2] { { Pattern[0], Pattern[1] }, { Pattern[2], Pattern[3] } }, Ticks));
            Fire(GameBoardChanged, new RotetrisEventArgs(new Point(Row, Col), new char[2, 2] { { Pattern[0], Pattern[1] }, { Pattern[2], Pattern[3] } }, Ticks));
        }

        void Fire(EventHandler<RotetrisEventArgs> E, RotetrisEventArgs A)
        {
            if (E != null)
                E(this, A);
        }

        Random R = new Random();

        string RandomBlock(string Set)
        {

            var c1 = Set[R.Next() % Set.Length];
            var c2 = Set[R.Next() % Set.Length];

            var c3 = Set[R.Next() % Set.Length];
            var c4 = Set[R.Next() % Set.Length];

            return String.Join("", new char[] { c1, c2, c3, c4 });
        }


        void WriteWin()
        {
            // Wipe
            for (int i = Rows - 6; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    var xOffset = ((int)(i/2) )% 2 == 1 ? 0 : 1;

                    if (j+xOffset < Cols)
                         Model[i, j+xOffset] = 'Z';
                }
            }

            // Write
            WriteChar_W(Rows - 6, 2, 'A');
            WriteChar_I(Rows - 6, 8, 'B');
            WriteChar_N(Rows - 6, 10, 'C');

            if (Cols >= 30)
            {

                WriteChar_N(Rows - 6, 16, 'D');
                WriteChar_E(Rows - 6, 22, 'A');
                WriteChar_R(Rows - 6, 27, 'B');
                WriteChar_Ex(Rows - 6, 32, 'C');
                WriteChar_Ex(Rows - 6, 36, 'D');
            }
            else
            {
                
                WriteChar_Ex(Rows - 6, 16, 'D');
                WriteChar_Ex(Rows - 6, 20, 'C');
                WriteChar_Ex(Rows - 6, 24, 'D');
                WriteChar_Ex(Rows - 6, 28, 'D');
            }


            // Notify for draw
            for (int Row = Rows - 6; Row < Rows; Row+=2)
            {
                for (int Col = 0; Col <= Cols-2; Col+=2)
                {
                    var xOffset = ((int)(Row / 2)) % 2 == 1 ? 0 : 1;

                    if (xOffset + Col + 1 < Cols)
                        Fire(GameStaticBlockAdded, new RotetrisEventArgs(new Point(Row, Col + xOffset), new char[2, 2] { { Model[Row, Col + xOffset], Model[Row, Col + xOffset + 1] }, { Model[Row + 1, Col + xOffset], Model[Row + 1, Col + xOffset + 1] } }, Ticks));

                }
            }
           
        }

        void WriteLose()
        {
            // Wipe
            for (int i = Rows - 6; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    var xOffset = ((int)(i / 2)) % 2 == 1 ? 0 : 1;

                    if (j + xOffset < Cols)
                        Model[i, j + xOffset] = 'Z';

                    
                }
            }

            // Write
            WriteChar_L(Rows - 6, 2, 'A');
            WriteChar_O(Rows - 6, 7, 'B');
            WriteChar_S(Rows - 6, 12, 'C');
            WriteChar_E(Rows - 6, 17, 'D');
            WriteChar_Ex(Rows - 6, 22, 'C');
            WriteChar_Ex(Rows - 6, 26, 'D');


            // Notify for draw
            for (int Row = Rows - 6; Row < Rows; Row += 2)
            {
                for (int Col = 0; Col <= Cols - 2; Col += 2)
                {
                    var xOffset = ((int)(Row / 2)) % 2 == 1 ? 0 : 1;

                    if (xOffset + Col + 1 < Cols)
                        Fire(GameStaticBlockAdded, new RotetrisEventArgs(new Point(Row, Col + xOffset), new char[2, 2] { { Model[Row, Col + xOffset], Model[Row, Col + xOffset + 1] }, { Model[Row + 1, Col + xOffset], Model[Row + 1, Col + xOffset + 1] } }, Ticks));

                }
            }

        }

        void SetModel(int row, int col, char val)
        {
            if (row < Rows && col < Cols)
            {
                Model[row, col] = val;
            }
        }

        void WriteChar_W(int topRow, int topCol, char Color)
        {

           

                for (int i = 0; i < 6; i++)
                {
                    SetModel(topRow + i, topCol,Color);
                    SetModel(topRow + i, topCol + 4, Color);
                }

                SetModel(topRow + 4, topCol + 1, Color);
                SetModel(topRow + 3, topCol + 2, Color);
                SetModel(topRow + 4, topCol + 3, Color);
           
        }

        void WriteChar_I(int topRow, int topCol, char Color)
        {
            for (int i = 0; i < 6; i++)
            {
                SetModel(topRow + i, topCol, Color);
               
            }

          
        }

        void WriteChar_L(int topRow, int topCol, char Color)
        {
            for (int i = 0; i < 6; i++)
            {
                SetModel(topRow + i, topCol, Color);

            }

            for (int j = 1; j < 4; j++)
            {
                SetModel(topRow + 5, topCol + j, Color);

            }
        }

        void WriteChar_O(int topRow, int topCol, char Color)
        {
            for (int i = 0; i < 6; i++)
            {
                SetModel(topRow + i, topCol, Color);
                SetModel(topRow + i, topCol + 3, Color);

            }

            for (int j = 1; j < 4; j++)
            {
                SetModel(topRow , topCol + j, Color);
                SetModel(topRow+5, topCol + j, Color);
            }
        }

        void WriteChar_S(int topRow, int topCol, char Color)
        {
            
            for (int i = 0; i < 2; i++) {
                SetModel(topRow, topCol + 1 + i, Color);
               SetModel(topRow+2, topCol + 1 + i, Color);
                 SetModel(topRow+5, topCol + 1 + i, Color);
            }

            SetModel(topRow+1, topCol, Color);
            SetModel(topRow + 4, topCol, Color);
            SetModel(topRow + 3, topCol + 3, Color);
            SetModel(topRow + 4, topCol+3, Color);
           
        }


        void WriteChar_Ex(int topRow, int topCol, char Color)
        {
            for (int i = 0; i < 6; i++)
            {
                if (i != 3)
                {
                  
                        SetModel(topRow + i, topCol, Color);

                   
                        SetModel(topRow + i, topCol + 1, Color);
                }

                    
            }


        }

        void WriteChar_E(int topRow, int topCol, char Color)
        {
            for (int i = 0; i < 6; i++)
            {
                SetModel(topRow + i, topCol, Color);

            }

            for (int j = 1; j < 4; j++)
            {
                SetModel(topRow + 5, topCol+j, Color);

                if (j < 3)
                     SetModel(topRow + 2, topCol +j, Color);

                SetModel(topRow, topCol + j, Color);
            }

        }

        void WriteChar_R(int topRow, int topCol, char Color)
        {
            for (int i = 0; i < 6; i++)
            {
                SetModel(topRow + i, topCol, Color);

                if (i != 0 && i != 2)
                {
                    SetModel(topRow + i, topCol+3, Color);
                }
            }

            for (int j = 1; j < 3; j++)
            {
               

                
                    SetModel(topRow, topCol + j, Color);

                SetModel(topRow+2, topCol + j, Color);
            }

        }



        void WriteChar_N(int topRow, int topCol, char Color)
        {
            for (int i = 0; i < 6; i++)
            {
                SetModel(topRow + i, topCol, Color);
                SetModel(topRow + i, topCol + 4, Color);

            }
            SetModel(topRow + 1, topCol + 1, Color);
            SetModel(topRow + 2, topCol + 2, Color);
            SetModel(topRow + 3, topCol + 3, Color);

        }

        public Rotetris_Engine(int Rows, int Cols, int StartRows, int Tick_ms, int ColorCt)
        {
            this.Rows = Rows;
            this.Cols = Cols;


            this.StartRows = StartRows;

           this.Tick_ms = Tick_ms;


           this.NumColorsUsed = ColorCt;

            Model = new char[Rows, Cols];

        }

        
        public void ConfigAndStart()
        {
            if (!Running)
            {

                ActiveBlock.Rows = Rows;
                ActiveBlock.Cols = Cols;

                Running = true;

                for (int i = 0; i < Rows; i++)
                {
                    for (int j = 0; j < Cols; j++)
                    {




                        Model[i, j] = '*';




                    }
                }

            
                //CurrentBlock = new ActiveBlock("ABCD");
                //CurrentBlock.Move(0, 1); // Offset from Even row

                for (int j = 0; (j / 2) < StartRows; j += 2)
                    for (int i = ((j / 2) % 2); i < Cols - ((j / 2) % 2); i += 2)
                {
                    AddBlock(Rows - 2 - j, i, RandomBlock(ColorAlphabet.Substring(0, NumColorsUsed)));

                }

                
               

                EngineTimer = new DispatcherTimer();
                EngineTimer.Tick += dispatcherTimer_Tick;
                EngineTimer.Interval = new TimeSpan(0, 0, 0,0, Tick_ms);
                EngineTimer.Start();

            }

        }

        public string PrintModel()
        {
            StringBuilder SB = new StringBuilder();
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    SB.Append(Model[i, j]);
                }
                SB.AppendLine();
            }

            return (SB.ToString());
        }

        public void ForceStart()
        {

            Running = true;
            GameEnded = false;

            ActiveBlock.Rows = Rows;
            ActiveBlock.Cols = Cols;

            

  
            var OldName = UserControllerBlock==null?"": UserControllerBlock.Name; // Fix user control since reference was broken during deserialization
            UserControllerBlock = null;


            foreach (ActiveBlock B in FallingBlocks)
            {

              


                if (B.Name != OldName)
                    Fire(GamePieceAdded, new RotetrisEventArgs(new Point(B.posRow, B.posCol), B.block, Ticks, B.Name));
                else 
                    UserControllerBlock = B;
            
            
            }

         if ( UserControllerBlock != null)
            Fire(GamePieceAdded, new RotetrisEventArgs(new Point(UserControllerBlock.posRow, UserControllerBlock.posCol), UserControllerBlock.block, Ticks, UserControllerBlock.Name));
          
          

                for (int i = 0; i < Rows; i += 2)
                {
                    for (int j = 0; j <= Cols - 2; j += 2)
                    {
                        var xMod = (i / 2) % 2 == 0 ? 1 : 0;

                        var col = j + xMod;

                        if (xMod == 1 && j == Cols - 2)
                            continue;


                        var PossibleBlock = String.Format("{0}{1}{2}{3}", Model[i, col], Model[i, col + 1], Model[i + 1, col], Model[i + 1, col + 1]);

                        if (!PossibleBlock.Equals("****"))
                        {
                            if (Model[i, col] == '*')
                                break;

                            Fire(GameStaticBlockAdded, new RotetrisEventArgs(new Point(i, col), new char[2, 2] { { Model[i, col], Model[i, col + 1] }, { Model[i + 1, col], Model[i + 1, col + 1] } }, Ticks, ""));
                        }



                    }
                }



                EngineTimer = new DispatcherTimer();
                EngineTimer.Tick += dispatcherTimer_Tick;
                EngineTimer.Interval = new TimeSpan(0, 0, 0, 0, Tick_ms);
                EngineTimer.Start();

            

        }


        public void Pause()
        {
            if (EngineTimer != null)
                 EngineTimer.Stop();
            Running = false;
        }

        public void Resume()
        {
            if (EngineTimer != null && GameEnded == false)
                 EngineTimer.Start();
            Running = true;
        }

        public bool MoveUserBlock(int row, int col)
        {
            if (!Running)
            {
                // Reject user actions
                return false;
            }
    
            if (CheckBounds(row, col))
            {
                if (col < Cols-1 && row < Rows-1 && Model[row, col] == '*' && Model[row + 1, col] == '*' && Model[row, col + 1] == '*' && Model[row + 1, col+1] == '*')
                {
                    if (row >= MinRowValidPosition)
                    {

                        UserControllerBlock.posRow = row;
                        UserControllerBlock.posCol = col;

                        Fire(GamePieceMoved, new RotetrisEventArgs(new Point(UserControllerBlock.posRow, UserControllerBlock.posCol), UserControllerBlock.block, Ticks, UserControllerBlock.Name));
                        return true;
                    }
               }
            }
            return false;
        }

       

        void dispatcherTimer_Tick(object sender, object e)
        {
            lock (this.GetType()) {

                topRowAllowed++;

                for (int i = 0; i < FallingBlocks.Count(); i++ )
                {
                    ActiveBlock CurrentBlock = FallingBlocks[i];

                    if (!CheckCollision(1, 0, CurrentBlock))
                    {
                        CurrentBlock.Move(1, 0);
                        Fire(GamePieceMoved, new RotetrisEventArgs(new Point(CurrentBlock.posRow, CurrentBlock.posCol), CurrentBlock.block, Ticks, CurrentBlock.Name));
                    }
                    else
                    {
                        //MessageBox.Show(String.Join("",GetCollision(1,0)));

                        var Coll = GetCollision(1, 0, CurrentBlock);

                        if (Coll.Substring(2).Equals("@@"))
                        {
                            HitBottom(CurrentBlock);
                        }

                        else if (Coll.StartsWith("***")) // hit on right side
                        {
                            GlanceRight(CurrentBlock);
                        }
                        else if (Coll.StartsWith("**") && Coll.EndsWith("*")) // hit on left side 
                        {
                            GlanceLeft(CurrentBlock);
                            //else { 

                            //    AddBlock(CurrentBlock.posRow, CurrentBlock.posCol, CurrentBlock.Pattern);
                            //}
                        }
                        else if (CurrentBlock.posRow / 2 % 2 == CurrentBlock.posCol % 2 && CurrentBlock.Pattern.Substring(2).Equals(Coll.Substring(2))) // Same row match
                        {
                            FuseBlocks(CurrentBlock);
                        }
                        else if (CurrentBlock.Pattern.Substring(2).Equals(Coll.Substring(2))) // Opposite row match
                        {
                            OppositeRowMatch(CurrentBlock);
                        }
                        else if (CurrentBlock.posRow / 2 % 2 == CurrentBlock.posCol % 2)
                        { // Same row, non match
                            SameRowNonMatch(CurrentBlock);
                        }
                        else if (Coll.StartsWith("**")) // on non match
                        {
                            NonMatch(CurrentBlock);
                        }


                    }
                }

                ScanForGaps();

                    if (FallingBlocks.Count == 0)
                    {
                        StringBuilder SB = new StringBuilder();
                        for (int i = 0; i < Rows; i++)
                            for (int j = 0; j < Cols; j++)
                            {
                                if (!Model[i, j].Equals('*'))
                                {
                                    SB.Append(Model[i, j]);
                                }
                            }

                        if (SB.Length == 0)
                        {
                            // MessageBox.Show("You win!!");
                            Fire(GameWon, new RotetrisEventArgs(new Point(-1, -1), null, Ticks));
                            ((DispatcherTimer)sender).Stop();

                            WriteWin();

                            GameEnded = true;

                            return;
                        }

                        //var newBlock = new ActiveBlock(RandomBlock(SB.ToString()));

                       // EnsureMatch();
                        var newBlock = new ActiveBlock(EnsureMatch()+EnsureMatch());
                     
                        newBlock.posCol = Math.Max((R.Next()%Cols)-2, 0);

                        FallingBlocks.Add(newBlock);
                        UserControllerBlock = newBlock;

                        //// Test
                        //var newBlock2 = new ActiveBlock(RandomBlock(SB.ToString()));
                        //FallingBlocks.Add(newBlock2);
                        //Fire(GamePieceAdded, new RotetrisEventArgs(new Point(newBlock2.posRow, newBlock2.posCol), newBlock2.block, Ticks, newBlock2.Name));
         

                        if (CheckCollision(0, 0, newBlock)) // Refactor this, it's silly now
                        {
                             Fire(GameLost, new RotetrisEventArgs(new Point(newBlock.posRow, newBlock.posCol), newBlock.block, Ticks, newBlock.Name));
                            ((DispatcherTimer)sender).Stop();


                            // Change this so the new block becomes a physics object and falls

                            WriteLose();

                            GameEnded = true;
                        }
                        else
                        {
                            topRowAllowed = newBlock.posRow;
                            Fire(GamePieceAdded, new RotetrisEventArgs(new Point(newBlock.posRow, newBlock.posCol), newBlock.block, Ticks, newBlock.Name));
                        }


                    
                }
            }
            
            Fire(GameTick, new RotetrisEventArgs(new Point(-1,-1), null, Ticks++)); 
           // Draw();
        }


        public string EnsureMatch()
        {
            // UNTESTED PROBABLY NOT WORKING RIGHT
            
            var Rot1 = PreviewRotate(Model, true);
            var Rot2 = PreviewRotate(Rot1, true);
            var Rot3 = PreviewRotate(Rot2, true);
           


            // Harder
            var Lines = new string[] { TopRowFromPreview(Rot1),
            TopRowFromPreview(Rot2),
            TopRowFromPreview(Rot3),
            TopRowFromPreview(Model)};

            // Easier
           // var Lines = new string[] { TopRowFromPreview(Model) };

            // Two chars next to eachother always mean a match somewhere
            
            // Pick a rotation then pick a match


            int Pos = (R.Next() % (Lines[0].Length / 2)) * 2;

            return Lines[R.Next() % (Lines.Length)].Substring(Pos, 2);
        }


        private void NonMatch(ActiveBlock Block)
        {
            AddBlock(Block.posRow, Block.posCol, Block.Pattern);

            Fire(GamePieceRemoved, new RotetrisEventArgs(new Point(Block.posRow, Block.posCol), Block.block, Ticks, Block.Name));


            RemoveFallingBlock(Block);
            //CurrentBlock = null;
        }

        private void SameRowNonMatch(ActiveBlock Block)
        {
            if (Block.posCol < Cols - 1)
            {
                GlanceLeft(Block);
            }
            else
            {
                GlanceRight(Block);
            }
        }

        private void OppositeRowMatch(ActiveBlock Block)
        {
            Matches += 3;
            for (int i = Block.posRow + 2; i < Block.posRow + 4; i++)
            {
                for (int j = Math.Max(Block.posCol - 1, 0); j < Math.Min(Block.posCol + 3, Cols); j++)
                {
                    Model[i, j] = '*';

                    if ((j == Block.posCol - 1) && (i == Block.posRow + 2))
                    {
                        Fire(GameStaticBlockRemoved, new RotetrisEventArgs(new Point(i, j), new char[,] { { Model[i, j], Model[i, j + 1] }, { Model[i + 1, j], Model[i + 1, j + 1] } }, Ticks));
                    }


                    if ((j == Block.posCol + 1) && (i == Block.posRow + 2))
                    {
                        Fire(GameStaticBlockRemoved, new RotetrisEventArgs(new Point(i, j), new char[,] { { Model[i, j], Model[i, j + 1] }, { Model[i + 1, j], Model[i + 1, j + 1] } }, Ticks));
                    }

                    // Check for gap underneath pieces

                }


            }



            Fire(GamePieceRemoved, new RotetrisEventArgs(new Point(Block.posRow, Block.posCol), Block.block, Ticks, Block.Name));

            RemoveFallingBlock(Block);
            //CurrentBlock = null;
        }


        private void ScanForGaps()
        {

            bool found = true;

            while (found == true) // find new orphans that get created after these events fire.
            {
                found = false;


                for (int i = 1; i < Rows - 1; i++)
                    for (int j = 1; j < Cols - 1; j++)
                    {
                        if (Model[i, j] != '*')
                        {

                            var BlockPos = new Point(-1, -1);


                            if (Model[i, j + 1] == '*' && Model[i + 1, j] == '*' && Model[i + 1, j + 1] == '*')
                            { //Lower right edge of block detected
                                BlockPos.X = i - 1;
                                BlockPos.Y = j - 1;
                            }
                            else if (Model[i, j - 1] == '*' && Model[i + 1, j] == '*' && Model[i + 1, j - 1] == '*')
                            { // Lower left edge of block detected
                                BlockPos.X = i - 1;
                                BlockPos.Y = j;
                            }

                            if (BlockPos.X > 0) // Found a gap
                            {
                                found = true;

                                // Speed up temporarily
                                //EngineTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);

                                var row = Convert.ToInt32(BlockPos.X);
                                var col = Convert.ToInt32(BlockPos.Y);

                                var StaticBlock = String.Format("{0}{1}{2}{3}", Model[row, col], Model[row, col + 1], Model[row + 1, col], Model[row + 1, col + 1]);
                                Model[row, col] = '*';
                                Model[row, col + 1] = '*';
                                Model[row + 1, col] = '*';
                                Model[row + 1, col + 1] = '*';

                                Fire(GameStaticBlockRemoved, new RotetrisEventArgs(new Point(row, col), new char[,] { { Model[row, col], Model[row, col + 1] }, { Model[row + 1, col], Model[row + 1, col + 1] } }, Ticks));

                                //var newBlock = new ActiveBlock(StaticBlock);
                                //newBlock.posCol = col;
                                //newBlock.posRow = row;
                                //FallingBlocks.Add(newBlock);


                                //Fire(GamePieceAdded, new RotetrisEventArgs(new Point(newBlock.posRow, newBlock.posCol), newBlock.block, Ticks, newBlock.Name));

                            }

                        }
                    }

                if (found == false)
                {
                    // EngineTimer.Interval = new TimeSpan(0, 0, 0, 1, 0);
                }
                //for (int i = Rows - 4; i >= 0; i-=2)
                //{
                //    for (int j = Cols - 4; j >= 0; j-=2)
                //    {
                //        if (Model[i,j] != '*' && ((Model[i + 2, j + 1] == '*' && Model[i + 2, j + 2] == '*') || (Model[i + 2, j - 1] == '*' && Model[i + 2, j] == '*')))
                //        {
                //            // Need to break out the static item at i,j


                //            var StaticBlock = String.Format("{0}{1}{2}{3}", Model[i, j], Model[i, j+1], Model[i+1, j], Model[i+1, j+1]);
                //            Model[i, j] = '*';
                //            Model[i, j+1] = '*';
                //            Model[i+1, j] = '*';
                //            Model[i + 1, j + 1] = '*';

                //            Fire(GameStaticBlockRemoved, new RotetrisEventArgs(new Point(i, j), new char[,] { { Model[i, j], Model[i, j + 1] }, { Model[i + 1, j], Model[i + 1, j + 1] } }, Ticks));

                //            var newBlock = new ActiveBlock(StaticBlock);
                //            newBlock.posCol = j;
                //            newBlock.posRow = i;
                //            FallingBlocks.Add(newBlock);


                //            Fire(GamePieceAdded, new RotetrisEventArgs(new Point(newBlock.posRow, newBlock.posCol), newBlock.block, Ticks, newBlock.Name));

                //        }
                //    }
                //}
            }
        }

        private void FuseBlocks(ActiveBlock Block)
        {
            // Fuse blocks

            Matches++;


            Fire(GamePieceRemoved, new RotetrisEventArgs(new Point(Block.posRow, Block.posCol), Block.block, Ticks, Block.Name));
// OLD MERGE CODE
            // Set the collision points to the first two characters in the current block's set
            //var r = FusePieces(2, 0, Block.Pattern, Block);

            //var i = Block.posRow + 2;
            //var j = Block.posCol;
            //Fire(GamePieceMerged, new RotetrisEventArgs(new Point(Block.posRow + 2, Block.posCol), new char[,] { { Model[i, j], Model[i, j + 1] }, { Model[i + 1, j], Model[i + 1, j + 1] } }, Ticks, Block.Name));

            //RemoveFallingBlock(Block);


             for (int r = Block.posRow+2; r < Block.posRow + 4; r++)
                 for (int c = Block.posCol; c < Block.posCol + 2; c++)
                 {
                     Model[r, c] = '*';
                 }

            RemoveFallingBlock(Block);

            var i = Block.posRow + 2;
            var j = Block.posCol;

            Fire(GameStaticBlockRemoved, new RotetrisEventArgs(new Point(i, j), new char[,] { { Model[i, j], Model[i, j + 1] }, { Model[i + 1, j], Model[i + 1, j + 1] } }, Ticks));
       

        }


        private void GlanceLeft(ActiveBlock Block)
        {
            if (Block.posCol < Cols - 2)
            {
                Block.posCol++;

                if (!CheckCollision(1, 0, Block))
                    Block.posRow++;

                Fire(GamePieceMoved, new RotetrisEventArgs(new Point(Block.posRow, Block.posCol), Block.block, Ticks, Block.Name));
            }
            else if (Block.posCol == Cols - 2 && (Block.posRow/2 % 2 == 0)) // Move right, since at the edge
            {
                Block.posCol--;

                if (!CheckCollision(1, 0, Block))
                    Block.posRow++;

                Fire(GamePieceMoved, new RotetrisEventArgs(new Point(Block.posRow, Block.posCol), Block.block, Ticks, Block.Name));
            }
            else
            {
               
                AddBlock(Block.posRow, Block.posCol, Block.Pattern);

                Fire(GamePieceRemoved, new RotetrisEventArgs(new Point(Block.posRow, Block.posCol), Block.block, Ticks, Block.Name));


                RemoveFallingBlock(Block);
                //CurrentBlock = null;

            }
        }

        private void GlanceRight(ActiveBlock Block)
        {
            if (Block.posCol > 1)
            {
                Block.posCol--;

                if (!CheckCollision(1, 0, Block))
                    Block.posRow++;


                Fire(GamePieceMoved, new RotetrisEventArgs(new Point(Block.posRow, Block.posCol), Block.block, Ticks, Block.Name));
            }
            else
            {
                

                AddBlock(Block.posRow, Block.posCol, Block.Pattern);

                Fire(GamePieceRemoved, new RotetrisEventArgs(new Point(Block.posRow, Block.posCol), Block.block, Ticks, Block.Name));

                RemoveFallingBlock(Block);
                //CurrentBlock = null;
            }
        }

        private void HitBottom(ActiveBlock Block)
        {
            if (Block.posCol % 2 == 1)
            {
                Block.posCol++;

            }

            AddBlock(Block.posRow, Block.posCol, Block.Pattern);


            Fire(GamePieceRemoved, new RotetrisEventArgs(new Point(Block.posRow, Block.posCol), Block.block, Ticks, Block.Name));


            RemoveFallingBlock(Block);

            //CurrentBlock = null;
        }

        private void RemoveFallingBlock(ActiveBlock Block)
        {
            FallingBlocks.Remove(Block);
        }

        private void Draw()
        {

            // MatchesCount.Text = Matches.ToString();
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    //  ModelView[i, j].Text = Model[i, j].ToString();
                    // ModelView[i, j].Fill = MapColor(Model[i, j]);

                    var colOffset = (i / 2) % 2;


                    var Angle = 0;
                    if (((j + colOffset) % 2 == 0) && (i % 2 == 0))
                    {
                        Angle = 90;
                    }
                    if (((j + colOffset) % 2 == 1) && (i % 2 == 0))
                    {
                        Angle = 0;
                    }
                    if (((j + colOffset) % 2 == 0) && (i % 2 == 1))
                    {
                        Angle = 180;
                    }
                    if (((j + colOffset) % 2 == 1) && (i % 2 == 1))
                    {
                        Angle = 270;
                    }

                    //  ModelView[i, j].RenderTransform = new RotateTransform(Angle, 7.5, 7.5);
                }
            }

            //if (CurrentBlock != null)
            //{
            //    for (int i = 0; i < 2; i++)
            //        for (int j = 0; j < 2; j++)
            //        {
            //           // ModelView[i + CurrentBlock.posRow, j + CurrentBlock.posCol].Text = CurrentBlock.block[i, j].ToString();
            //           // ModelView[i + CurrentBlock.posRow, j + CurrentBlock.posCol].Fill = MapColor(CurrentBlock.block[i, j]);
            //            var Angle = 0;
            //             if (((j ) % 2 == 0) && (i % 2 == 0))
            //        {
            //            Angle = 0;
            //        }
            //        if (((j ) % 2 == 1) && (i % 2 == 0))
            //        {
            //            Angle = 90;
            //        }
            //        if (((j ) % 2 == 0) && (i % 2 == 1))
            //        {
            //            Angle = 270;
            //        }
            //        if (((j ) % 2 == 1) && (i % 2 == 1))
            //        {
            //            Angle = 180;
            //        }
            //      //  ModelView[i + CurrentBlock.posRow, j + CurrentBlock.posCol].RenderTransform = new RotateTransform(Angle, 7.5, 7.5);
            //        }
            //}
        }

        public bool CheckBounds(int Row, int Col)
        {
            if ((Col >= Cols) || (Col < 0) || (Row >= Rows) || (Row < 0))
            {
                return false;
            }

            return true;
        }

        public bool CheckCollision(int offsetRow, int offsetCol, ActiveBlock C)
        {

            

            if (C != null)
            {
                for (int i = 0; i < 2; i++)
                    for (int j = 0; j < 2; j++)
                    {


                        if (!CheckBounds(i + C.posRow + offsetRow, j + C.posCol + offsetCol))
                        {
                            return true;
                        }

                        if (Model[i + C.posRow + offsetRow, j + C.posCol + offsetCol] != '*')
                        {
                           return true;
                        }


                    }
            }

            return false;
        }

        private string GetCollision(int offsetRow, int offsetCol, ActiveBlock Block)
        {
            char[] retVal = ("****").ToCharArray();
            if (Block != null)
            {
                for (int i = 0; i < 2; i++)
                    for (int j = 0; j < 2; j++)
                    {

                        var index = i + (i * 1) + j;
                        if (!CheckBounds(i + Block.posRow + offsetRow, j + Block.posCol + offsetCol))
                        {
                            retVal[index] = '@';
                            continue;
                        }

                        retVal[index] = Model[i + Block.posRow + offsetRow, j + Block.posCol + offsetCol];

                    }
            }

            return String.Join("", retVal);
        }

        private char[,] FusePieces(int offsetRow, int offsetCol, string Vals, ActiveBlock Block)
        {
            char[,] retVal = new char[2, 2];
            if (Block != null)
            {
                int i = 1;



                for (int j = 0; j < 2; j++)
                {


                    if (!CheckBounds(i + Block.posRow + offsetRow, j + Block.posCol + offsetCol))
                    {

                        continue;
                    }

                    Model[i + Block.posRow + offsetRow, j + Block.posCol + offsetCol] = Vals[j+2];
                    retVal[i, j] = Vals[j+2];

                    retVal[0, j] = Model[Block.posRow + offsetRow, j + Block.posCol + offsetCol];
                }
            }

            return retVal;




        }

        // Add Nudge

        public void SendKeyUpperCase(char C)
        {

            if (!Running)
            {
                // Reject user actions except for winning easter egg

                if (GameEnded == true && (C == 'R' || C == 'Q'))
                {
                  // Allow rotation actions after the game ends and while it is not running

                   
                }
                else {

                return;
                
                }
            }

            lock (this.GetType())
            {
                switch (C)
                {
                    case 'R':

                        var model = PrintModel();
                        for (int i = Rows - 2; i >= 0; i -= 2)
                        {
                            RotateRow(i, (i>>1) % 2 == 1);
                        }

                        Fire(GameBoardChanged, new RotetrisEventArgs(new Point(-1,-1), null, Ticks));

                         model = PrintModel();

                        break;
                    case 'Q':

                        for (int i = Rows - 2; i >= 0; i -= 2)
                        {
                            RotateRow(i, (i>>1) % 2 == 0);
                        }

                        Fire(GameBoardChanged, new RotetrisEventArgs(new Point(-1,-1), null, Ticks));
                        break;
                    case 'W':

                        //if (!CheckCollision(-1, 0))
                        //    CurrentBlock.Move(-2, 0);

                        break;
                    case 'A':
                        if (UserControllerBlock != null && !CheckCollision(0, -1, UserControllerBlock))
                        {
                            UserControllerBlock.Move(0, -1);
                            Fire(GamePieceMoved, new RotetrisEventArgs(new Point(UserControllerBlock.posRow, UserControllerBlock.posCol), UserControllerBlock.block, Ticks, UserControllerBlock.Name));
                    }
                        break;
                    case 'S':
                        if (UserControllerBlock != null && !CheckCollision(1, 0, UserControllerBlock))
                        {
                            UserControllerBlock.Move(1, 0);
                            Fire(GamePieceMoved, new RotetrisEventArgs(new Point(UserControllerBlock.posRow, UserControllerBlock.posCol), UserControllerBlock.block, Ticks, UserControllerBlock.Name));
                    }
                        break;
                    case 'D':
                        if (UserControllerBlock != null && !CheckCollision(0, 1, UserControllerBlock))
                        {
                            UserControllerBlock.Move(0, 1);
                            Fire(GamePieceMoved, new RotetrisEventArgs(new Point(UserControllerBlock.posRow, UserControllerBlock.posCol), UserControllerBlock.block, Ticks, UserControllerBlock.Name));
                    }

                        break;
                }
            }
            //Draw();



        }


        private string TopRowFromPreview(char[,] FauxModel)
        {
            var SB = new StringBuilder();

            //for (int i = 0; i < Cols; i++)
            //{
            //    for (int j = 0; j < Rows; j++)
            //    {
            //        if (FauxModel[j, i] != '*')
            //        {
            //            SB.Append(FauxModel[j,i]);
            //            break;
            //        }

            //    }
            //}

             char LastMatch = '\0';
                int lastHeight = 0;

            for (int i = 0; i < Cols; i++)
            {

                for (int j = 0; j < Rows; j++)
                {
                    if (FauxModel[j, i] != '*')
                    {

                        if ((j == lastHeight) && LastMatch != '\0')
                        {
                            SB.Append(LastMatch);
                            SB.Append(FauxModel[j, i]);
                        }

                        LastMatch = FauxModel[j, i];
                        lastHeight = j;
                        //SB.Append(FauxModel[j, i]);
                        break;
                    }

                }
            }


            return SB.ToString();

        }

        private char[,] PreviewRotate(char[,] Model, bool Clockwise)
        {

            var FauxModel = new char[Rows, Cols];
            for (int k = 0; k < Rows; k++)
                for (int j = 0; j < Cols; j++)
                {
                    FauxModel[k, j] = Model[k, j];
                }


            for (int r = Rows - 2; r >= 0; r -= 2)
            {

                var RealRow = r / 2;


                var i = r;

                for (int j = (RealRow + 1) % 2; j < Cols - 1; j += 2)
                {

                    var C = FauxModel[i, j];
                    if (Clockwise)
                    {

                        FauxModel[i, j] = FauxModel[i + 1, j];
                        FauxModel[i + 1, j] = FauxModel[i + 1, j + 1];
                        FauxModel[i + 1, j + 1] = FauxModel[i, j + 1];
                        FauxModel[i, j + 1] = C;


                    }
                    else
                    {
                        // 0, 0
                        FauxModel[i, j] = FauxModel[i, j + 1];
                        // 0,1 
                        FauxModel[i, j + 1] = FauxModel[i + 1, j + 1];
                        // 1, 1
                        FauxModel[i + 1, j + 1] = FauxModel[i + 1, j];

                        // 1, 0
                        FauxModel[i + 1, j] = C;



                    }
                }
            }
            return FauxModel;
        }

        private void RotateRow(int RowNum, bool Clockwise)
        {

            // if the real row (RowNum / 2) is even, the rotate is off by 1

            var offSet = 0;
            var RealRow = RowNum / 2;
            if ((RealRow + 1) % 2 == 0)
            {
                offSet = 1;
            }

            var i = RowNum;

            for (int j = (RealRow + 1) % 2; j < Cols - 1; j += 2)
            {

                var C = Model[RowNum, j];
                if (Clockwise)
                {

                    Model[RowNum, j] = Model[RowNum + 1, j];
                    Model[RowNum + 1, j] = Model[RowNum + 1, j + 1];
                    Model[RowNum + 1, j + 1] = Model[RowNum, j + 1];
                    Model[RowNum, j+ 1] = C;

                    
                    Fire(GameRotatedClockwise, new RotetrisEventArgs(new Point(i, j), new char[,] { { Model[i, j], Model[i, j + 1] }, { Model[i + 1, j], Model[i + 1, j + 1] } }, Ticks));


                }
                else
                {
                    // 0, 0
                    Model[RowNum, j] = Model[RowNum, j + 1];
                    // 0,1 
                    Model[RowNum, j + 1] = Model[RowNum + 1, j + 1];
                    // 1, 1
                    Model[RowNum + 1, j + 1] = Model[RowNum + 1, j];

                    // 1, 0
                    Model[RowNum + 1, j] = C;


                    Fire(GameRotatedAnticlockwise, new RotetrisEventArgs(new Point(i, j), new char[,] { { Model[i, j], Model[i, j + 1] }, { Model[i + 1, j], Model[i + 1, j + 1] } }, Ticks));



                }
            }

        }

    }


}
