using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GearMaker
{
    public class GameOptions
    {
        public static bool SFX
        {
            get { return Instance._SFX; }
            set { Instance._SFX = value; }
        }
        public static string Music
        {
            get { return Instance._Music; }
            set { Instance._Music = value; }
        }
        public static int Rows
        {
            get { return Instance._Rows; }
            set { Instance._Rows = value; }
        }
        public static int Cols
        {
            get { return Instance._Cols; }
            set { Instance._Cols = value; }
        }
        public static int Colors
        {
            get { return Instance._Colors; }
            set { Instance._Colors = value; }
        }
        public static int StartingRows
        {
            get { return Instance._StartingRows; }
            set { Instance._StartingRows = value; }
        }
        public static int Speed
        {
            get { return Instance._Speed; }
            set { Instance._Speed = value; }
        }

        private static volatile GameOptions _instance;
        private static object syncRoot = new Object();

        private GameOptions()
        {

        }

        public static GameOptions Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (syncRoot)
                    {
                        if (_instance == null)
                            _instance = new GameOptions();
                    }
                }
                return _instance;
            }

           
        }


        private bool _SFX = true;
        private string _Music = "A";
        private int _Rows = 20;
        private int _Cols = 40;
        private int _Colors = 4;
        private int _StartingRows = 3;
        private int _Speed = 1000;
    }
}
