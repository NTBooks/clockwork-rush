using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace GearMaker
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class GamePage : GearMaker.Common.LayoutAwarePage
    {

        Rotetris.Rotetris_Engine Engine;

        // last captured tick number from engine
        long GameTickResponsedTo = 0;

        int Rows = 20;
        int Cols = 40;

        // Gear size in pixels along with padding
        const double GearSize = 24;
        const double hSpace = 3;
        const double vSpace = 0;

        DispatcherTimer physicsTimer;

        // Music core
        MediaElement rootMediaElement;

        List<PhysicsObject> PhysicsObjects = new List<PhysicsObject>();

        Dictionary<char, SolidColorBrush> LetterColorMap = new Dictionary<char, SolidColorBrush>();

        TextBlock textTimer;

        // TextBlock currentScore;

        Rectangle topRow;

        string LoadStateValue;


        int trueScore = 0;

        List<Tuple<ItemsControl, int, int>> StaticBlocks = new List<Tuple<ItemsControl, int, int>>();

        bool StaticAdded = false;

        bool GameOver = false;

        bool UserPaused = false;
        string oldTextTimerVal = "";

        class PhysicsObject
        {
            public double rotationDelta;
            public double initialVLeft;
            public double initialVTop;
            public long ticksStarted;
            public double Left;
            public double Top;
            public double constAccelTop;
            public double constAccelLeft;
            public double TopFloor; //Value must be below this floor
            public double TopCeiling = -1500; // Value must be above this ceiling (when out of bounds, object is destroyed)
            public FrameworkElement Control;
        }

      
       

        private async Task<bool> ConfigEnv(string LoadStateVal = null) {


                if (GameOver == true)
                    return false; // Game is in win/loss state

                Engine = null;

              
            // Check and see if this was a load request, default is a new engine request
                if (String.IsNullOrEmpty(LoadStateVal))
                {

                    Rows = GameOptions.Rows;
                    Cols = GameOptions.Cols;

                }
                else
                {
                    Engine = await Rotetris.Rotetris_Engine.LoadEngine(LoadStateVal);
                    Rows = Engine.Rows;
                    Cols = Engine.Cols;
                }


            // Done to prevent loss of keydown event when other viewbox controls get focus
                Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;

            // Window is minimized, so pause/unpause engine
                Window.Current.CoreWindow.VisibilityChanged += CoreWindow_VisibilityChanged;

            // Supported color codes with their brushes
                LetterColorMap.Add('A', new SolidColorBrush(Windows.UI.Color.FromArgb(255, 109, 126, 255)));
                LetterColorMap.Add('B', new SolidColorBrush(Windows.UI.Color.FromArgb(255, 130, 204, 109)));
                LetterColorMap.Add('C', new SolidColorBrush(Windows.UI.Color.FromArgb(255, 224, 163, 62)));
                LetterColorMap.Add('D', new SolidColorBrush(Windows.UI.Color.FromArgb(255, 233, 119, 95)));

                LetterColorMap.Add('Z', new SolidColorBrush(Windows.UI.Colors.Black));

            // If a block is made containing an empty square (bug) show it in bright pink
                LetterColorMap.Add('*', new SolidColorBrush(Windows.UI.Colors.HotPink));


            // Set background size
                GameBoardBackground.Width = GameBoard.Width = (GearSize + hSpace) * Cols;
                GameBoardBackground.Height = GameBoard.Height = (GearSize + vSpace) * Rows;



            // Add random number of large background gears
                for (var v = 0; v < (new Random()).Next() % 5 + 1; v++)
                {
                    GameBoardBackground.Children.Add(AddBackgroundGear(""));
                }

            // Add descending time bar
                if (topRow == null)
                {
                    topRow = new Rectangle();
                    GameBoardBackground.Children.Add(topRow);
                    Canvas.SetLeft(topRow, 0);
                    Canvas.SetTop(topRow, 0);
                    topRow.Fill = new SolidColorBrush(Windows.UI.Colors.Gray);
                    topRow.Width = GameBoardBackground.Width;
                    topRow.Height = 2;
                    topRow.Opacity = 0.2;
                }

            // Add timer
                textTimer = new TextBlock();
                textTimer.Opacity = 0.3;
                textTimer.FontSize = 200;
                textTimer.Text = "00:00:00";
                textTimer.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Right;
                Canvas.SetLeft(textTimer, (int)GameBoardBackground.Width - 620);
                Canvas.SetTop(textTimer, -100);
                textTimer.Foreground = new SolidColorBrush(Windows.UI.Colors.DimGray);
                GameBoardBackground.Children.Add(textTimer);


              // Add score
                currentScore.FontSize = 96;
                currentScore.Text = "0";
                currentScore.Opacity = 0.5;
                currentScore.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;       
                currentScore.Foreground = new SolidColorBrush(Windows.UI.Colors.White);


            // Create engine
                if (Engine == null)
                {
                    Engine = new Rotetris.Rotetris_Engine(Rows, Cols, GameOptions.StartingRows, GameOptions.Speed, GameOptions.Colors);
                }



                // Draw background rectangle for game board
                var E = new Rectangle();
                E.Width = (GearSize + hSpace) * Cols;
                E.Height = (GearSize + vSpace) * Rows;
                E.Stroke = new SolidColorBrush(Windows.UI.Colors.Gray);
                E.Fill = new SolidColorBrush(Windows.UI.Colors.DimGray);
                E.Opacity = 0.1;
                Canvas.SetLeft(E, 0);
                Canvas.SetTop(E, 0);
                GameBoard.Children.Add(E);

            // Add grid dots in background
                for (int i = 0; i < Rows; i++)
                {
                    for (int j = 0; j < Cols + 1; j++)
                    {
                        var G = new Rectangle();
                        G.Width = 2;
                        G.Height = 2;

                        GameBoard.Children.Add(G);
                        G.Stroke = new SolidColorBrush(Windows.UI.Colors.Gray);

                        Canvas.SetLeft(G, j * (GearSize + hSpace));
                        Canvas.SetTop(G, i * (GearSize + vSpace));

                    }
                }


            // wire up event handlers
                Engine.GameBoardChanged += Engine_GameBoardChanged;
                Engine.GameLost += Engine_GameLost;
                Engine.GameWon += Engine_GameWon;
                Engine.GamePieceAdded += Engine_GamePieceAdded;
                Engine.GamePieceMoved += Engine_GamePieceMoved;
                Engine.GamePieceRemoved += Engine_GamePieceRemoved;
                Engine.GameStaticBlockAdded += Engine_GameStaticBlockAdded;
                Engine.GameStaticBlockRemoved += Engine_GameStaticBlockRemoved;
                Engine.GameRotatedClockwise += Engine_GameRotatedClockwise;
                Engine.GameRotatedAnticlockwise += Engine_GameRotatedAnticlockwise;
                Engine.GamePieceMerged += Engine_GamePieceMerged;
                Engine.GameTick += Engine_GameTick;


            // Track physics animations
                physicsTimer = new DispatcherTimer();
                physicsTimer.Tick += physicsTimer_Tick;
                physicsTimer.Interval = new TimeSpan(0, 0, 0, 0, 33);
                physicsTimer.Start();

                if (String.IsNullOrEmpty(LoadStateVal))
                    Engine.ConfigAndStart(); // Start new engine
                else
                    Engine.ForceStart(); // Resume an engine that was restored from a snapshot

                return true;

        }


       

        private async void pageRoot_Loaded_1(object sender, RoutedEventArgs e)
        {
           

            Windows.Graphics.Display.DisplayProperties.AutoRotationPreferences = Windows.Graphics.Display.DisplayOrientations.Landscape;

            var Res = await ConfigEnv(LoadStateValue);

            DependencyObject rootGrid = VisualTreeHelper.GetChild(Window.Current.Content, 0);
            rootMediaElement = (MediaElement)VisualTreeHelper.GetChild(rootGrid, 0);


        }

      
     
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

            base.OnNavigatedTo(e);

            LoadStateValue = null;

            var Param = (string) e.Parameter;

            // Load the state with name Param if it was passed in
            if (!Param.Equals("New") && !String.IsNullOrEmpty(Param))
            {
                LoadStateValue = Param;
            }

         

        }

        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

          
            // Stop engine and remove all listeners
            Engine.Pause();

            if (GameOver == false)
            {// Game not over {
                Rotetris.RecentState State = new Rotetris.RecentState("SaveState.xml", Engine);
                var saved = await State.SaveState();
            }
            else
            {

                // The game is over, so delete the last saved state
               await Rotetris.RecentState.ClearRecentState();

                
            }

         
            // Remove listeners
            Engine.GameBoardChanged -= Engine_GameBoardChanged;
            Engine.GameLost -= Engine_GameLost;
            Engine.GameWon -= Engine_GameWon;
            Engine.GamePieceAdded -= Engine_GamePieceAdded;
            Engine.GamePieceMoved -= Engine_GamePieceMoved;
            Engine.GamePieceRemoved -= Engine_GamePieceRemoved;
            Engine.GameStaticBlockAdded -= Engine_GameStaticBlockAdded;
            Engine.GameStaticBlockRemoved -= Engine_GameStaticBlockRemoved;
            Engine.GameRotatedClockwise -= Engine_GameRotatedClockwise;
            Engine.GameRotatedAnticlockwise -= Engine_GameRotatedAnticlockwise;
            Engine.GamePieceMerged -= Engine_GamePieceMerged;
            Engine.GameTick -= Engine_GameTick;

            Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;

            Window.Current.CoreWindow.VisibilityChanged -= CoreWindow_VisibilityChanged;

            // Remove reference to engine so it will be collected
            Engine = null;

            // Remove all Game GUI elements
            GameBoard.Children.Clear();
            GameBoardBackground.Children.Clear();

            // Stop and remove physics timer
            if (physicsTimer != null)
            {
                physicsTimer.Stop();
                physicsTimer.Tick -= physicsTimer_Tick;
            }
            physicsTimer = null;

            // Signal collection since ths is a non-play state (good time for a delay)
            GC.Collect();
        }

     

        public GamePage()
        {
            this.InitializeComponent();


        }


        /// <summary>
        /// Duplicate key function for when the main listener loses focus. The arguments are different types so ithad to be copied
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        async void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            if (Engine != null && (new TimeSpan(DateTime.Now.Ticks - lastKeyTick).TotalSeconds > 0.09))
            {
                lastKeyTick = DateTime.Now.Ticks;

                // Let the engine handle the actuall keypress
                Engine.SendKeyUpperCase(args.VirtualKey.ToString().ToUpper().ToCharArray()[0]);

                // Animate the screen buttons on R and Q (the only buttons shown)
                if (args.VirtualKey.ToString().ToUpper().Equals("R"))
                {
                    await AddDoubleAnimation(RotateR, 0, 0.5, 0.2, "Opacity");

                }

                if (args.VirtualKey.ToString().ToUpper().Equals("Q"))
                {
                    await AddDoubleAnimation(RotateQ, 0, 0.5, 0.2, "Opacity");

                }
            }

            args.Handled = true;
        }



        void CoreWindow_VisibilityChanged(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.VisibilityChangedEventArgs args)
        {
            // Pause the game when the game is minimized and unpause it as long as the user has not left it paused
            if (sender.Visible == true)
            {
                if (!UserPaused)
                {
                    Engine.Resume();

                    if (rootMediaElement != null)
                        rootMediaElement.Play();
                }
            }
            else
            {
                Engine.Pause();

                if (rootMediaElement != null)
                rootMediaElement.Pause();
            }
        }


        void physicsTimer_Tick(object sender, object e)
        {
            var CurrTime = DateTime.Now.Ticks;

            List<PhysicsObject> Trash = new List<PhysicsObject>();

            foreach (var V in PhysicsObjects)
            {
                long elapsedTicks = CurrTime - V.ticksStarted;
                TimeSpan elapsedSpan = new TimeSpan(elapsedTicks); // time based animation, not keyframes


                var CUrrVal = new Point(V.Top, V.Left);

                // Standard kinematics equation in screen coordinates (top left is 0,0)

                var newVTop = V.initialVTop * elapsedSpan.TotalSeconds + 0.5 * V.constAccelTop * elapsedSpan.TotalSeconds * elapsedSpan.TotalSeconds;
                var newVLeft =  V.initialVLeft * elapsedSpan.TotalSeconds + 0.5 * V.constAccelLeft * elapsedSpan.TotalSeconds * elapsedSpan.TotalSeconds;

             
              
                    RotateTransform rotateTransform1 = new RotateTransform();


                    rotateTransform1.CenterX = V.Control.Width / 2.0;
                    rotateTransform1.CenterY = V.Control.Height / 2.0;

                    rotateTransform1.Angle = V.rotationDelta * elapsedSpan.TotalSeconds;

                    V.Control.RenderTransform = rotateTransform1; 
             



                Canvas.SetTop(V.Control, V.Top+newVTop);
                Canvas.SetLeft(V.Control, V.Left+newVLeft);

                if ( (V.Top + newVTop > V.TopFloor) && (V.Top + newVTop > V.TopCeiling))
                {
                    GameBoard.Children.Remove(V.Control);

                    Trash.Add(V);
                 
                    // Can't modify collection in a for-each, so mark it for removal later. Can refactor this out eventually.
                }

               
            }

            for (int i = Trash.Count - 1; i >= 0; i--)
            {
                PhysicsObjects.Remove(Trash[i]);
            }

            // Animate the score updating by having the number gradually approach the true value
            // I could put this in a separate timer but it seemed wasteful
            var DisplayedScore = Convert.ToInt32(currentScore.Text);
            if (trueScore != DisplayedScore)
            {
                double diff = trueScore - DisplayedScore;
                DisplayedScore += (int) (diff / 10);

                currentScore.Text = ((int)DisplayedScore).ToString();

            }
        }

        async void Engine_GameTick(object sender, Rotetris.RotetrisEventArgs e)
        {

            GameTickResponsedTo = e.Ticks;

            // Rotate the large background gears when the engine updates
            foreach (var V in GameBoardBackground.Children)
            {
                if (V.GetType().Equals(typeof(ItemsControl)))
                    await RotateBlock(new Tuple<ItemsControl, int, int>((ItemsControl)V, 0, 0), Convert.ToInt32(((ItemsControl)V).Width) % 2 == 0 ? 1 : -1, 0.15);

            }


            // Animate the time bar
                       var myDoubleAnimation = new DoubleAnimation();
                myDoubleAnimation.From = (GearSize + vSpace) * Engine.MinRowValidPosition - 1;
                myDoubleAnimation.To = (GearSize + vSpace) * Engine.MinRowValidPosition;
                myDoubleAnimation.Duration = new Duration(TimeSpan.FromSeconds(1));
                Storyboard storyboard = new Storyboard();
                storyboard.Children.Add(myDoubleAnimation);
                Storyboard.SetTarget(myDoubleAnimation, topRow);
                Storyboard.SetTargetProperty(myDoubleAnimation, "(Canvas.Top)");
                storyboard.Begin();
           

            // Read game time and display it
            var TS = new TimeSpan(0, 0, Convert.ToInt32(e.Ticks * GameOptions.Speed / 1000.0));
            textTimer.Text = TS.ToString();


           // Deduct points for slowness
            ModScore(-5, 0, 0);
        }

      
        // Deprecated, behavior was not fun. Event will not fire.
        async void Engine_GamePieceMerged(object sender, Rotetris.RotetrisEventArgs e)
        {
             var RotatedBlock = StaticBlocks.Where<Tuple<ItemsControl, int, int>>(x => { return x.Item2 == e.P.X && x.Item3 == e.P.Y; }); // X, Item2 is row, Y,Item3 is col

             foreach (var v in RotatedBlock)
             {
                

                 var R1 = (R.Next() % 1000) - 500;
                 var R2 = -1 * ((R.Next() % 200) + 100);

              
                 PlayMerge();

                 GameBoard.Children.Remove(v.Item1);
                StaticBlocks.Remove(v);

                 var IC = await AddGameBlock(String.Format("{0}{1}{2}{3}", e.Contents[0, 0], e.Contents[0, 1], e.Contents[1, 0], e.Contents[1, 1]));

                GameBoard.Children.Add(IC);

                Canvas.SetLeft(IC, Convert.ToInt32(e.P.Y) * (GearSize + hSpace));
                Canvas.SetTop(IC, Convert.ToInt32(e.P.X) * (GearSize + vSpace));

                StaticBlocks.Add(new Tuple<ItemsControl, int, int>(IC, Convert.ToInt32(e.P.X), Convert.ToInt32(e.P.Y)));

                ModScore(50, Convert.ToInt32(e.P.X), Convert.ToInt32(e.P.Y));

             

          
             
                 break;
             }
        }

        private async void PlayGrind()
        {
            if (GameOptions.SFX == true)
            {
                MediaElement snd = new MediaElement();
                StorageFolder folder = await Package.Current.InstalledLocation.GetFolderAsync("Sounds");
                StorageFile file = await folder.GetFileAsync("grind.wav");
                var stream = await file.OpenAsync(FileAccessMode.Read);
                snd.SetSource(stream, file.ContentType);
                snd.Play();
            }

        }

        private async void PlayMerge()
        {

            if (GameOptions.SFX == true)
            {

                MediaElement snd = new MediaElement();
                StorageFolder folder = await Package.Current.InstalledLocation.GetFolderAsync("Sounds");
                StorageFile file = await folder.GetFileAsync("merge.wav");
                var stream = await file.OpenAsync(FileAccessMode.Read);
                snd.SetSource(stream, file.ContentType);
                snd.Play();
            }

        }

        async void Engine_GameRotatedAnticlockwise(object sender, Rotetris.RotetrisEventArgs e)
        {
            var RotatedBlock = StaticBlocks.Where<Tuple<ItemsControl, int, int>>(x => { return x.Item2 == e.P.X && x.Item3 == e.P.Y; }); // X, Item2 is row, Y,Item3 is col

            foreach (var v in RotatedBlock)
            {

                await RotateBlock(v, -90, 0.15);


                break;
            }
        }

        private async Task<bool> RotateBlock(Tuple<ItemsControl, int, int> v, double offset, double duration)
        {
            RotateTransform rotateTransform1 = new RotateTransform();



            rotateTransform1.CenterX = v.Item1.Width / 2.0;
            rotateTransform1.CenterY = v.Item1.Height / 2.0;

            var oldRot = v.Item1.Tag != null ? ((RotateTuple)v.Item1.Tag).FinalValue : 0;
            

            var OldRotProp = v.Item1.Tag != null ? ((RotateTuple)v.Item1.Tag).Transform.Angle : 0;


            v.Item1.Tag = new RotateTuple(oldRot + offset, rotateTransform1);


            v.Item1.RenderTransform = rotateTransform1; 


            await AddRotationAnimation((RotateTuple)v.Item1.Tag, OldRotProp, duration);

            return true;
        }



        async void Engine_GameRotatedClockwise(object sender, Rotetris.RotetrisEventArgs e)
        {

           

            var RotatedBlock = StaticBlocks.Where<Tuple<ItemsControl, int, int>>(x => { return x.Item2 == e.P.X && x.Item3 == e.P.Y; }); // X, Item2 is row, Y,Item3 is col

            foreach (var v in RotatedBlock)
            {

                await RotateBlock(v, 90, 0.15);


                break;
            }
        }

        Random R = new Random(Convert.ToInt32(DateTime.Now.Ticks % Int32.MaxValue));

        void ModScore(int Amount, int Row, int Col)
        {
            if (Amount > 0)
            {
                Amount += trueScore / Convert.ToInt32(GameTickResponsedTo + 1) * 5;

                var TB = new TextBlock();
                TB.Foreground = new SolidColorBrush(Windows.UI.Colors.Yellow);
                TB.Opacity = 0.8;
                TB.Text = Amount.ToString();
                TB.FontSize = 25;



                // Add a floating text block for the changed score
                GameBoard.Children.Add(TB);

                trueScore = Math.Max(trueScore + Amount, 0);

            

                AddOpacityAnimation(TB, 0.8, 0, 3);
                PhysicsObjects.Add(new PhysicsObject() { Control = (FrameworkElement)TB, initialVTop = Col, initialVLeft = Row - Rows / 2, constAccelTop = -100, rotationDelta = 0, Top = Row * (GearSize + vSpace), Left = Col * (GearSize + hSpace), constAccelLeft = 0, ticksStarted = DateTime.Now.Ticks, TopFloor = 1500 });
            }
            else
            {
                // Don't let score go negative
                trueScore = Math.Max(trueScore + Amount, 0);
            }
        }

        async void Engine_GameStaticBlockRemoved(object sender, Rotetris.RotetrisEventArgs e)
        {
            var RemovedBlock = StaticBlocks.Where<Tuple<ItemsControl, int, int>>(x => { return x.Item2 == e.P.X && x.Item3 == e.P.Y; }); // X, Item2 is row, Y,Item3 is col
            
            foreach (var v in RemovedBlock)
            {

                PlayGrind();

                // Fade gear out and let it fall
                await AddOpacityAnimation(v.Item1, v.Item1.Opacity, 0, 1);
               

                var R1 = (R.Next() % 1000) - 500;
                var R2 = -1*((R.Next() % 200) + 100);
               
                // This collection should be the last reference to the piece
                PhysicsObjects.Add(new PhysicsObject() { Control = v.Item1, Top = Canvas.GetTop(v.Item1), Left = Canvas.GetLeft(v.Item1), constAccelTop = 500, constAccelLeft = 0, initialVLeft = R1, initialVTop =R2, TopFloor = 1300, rotationDelta = R1, ticksStarted = DateTime.Now.Ticks });


                ModScore(200, Convert.ToInt32(e.P.X), Convert.ToInt32(e.P.Y));
             

                StaticBlocks.Remove(v);
                break;
            }

        }

        async void Engine_GamePieceRemoved(object sender, Rotetris.RotetrisEventArgs e)
        {
            // Get the piece we want if it still exists
            if (GamePieces.ContainsKey(e.ActivePieceName))
            {

                var temp = GamePieces[e.ActivePieceName]; ;

                // If this event is firing after a static piece was added, fade the piece out, otherwise have it fall away
                if (StaticAdded == true)
                {
                    (await AddOpacityAnimation(temp, temp.Opacity, 0, 0.1)).Completed += (o, o2) =>
                    {

                        GameBoard.Children.Remove(temp);
                    };


                
                }
                else
                {

                   await  AddOpacityAnimation(temp, temp.Opacity, 0, 1);

                    var R1 = (R.Next() % 1000) - 500;
                    var R2 = -1 * ((R.Next() % 200) + 100);

                    PhysicsObjects.Add(new PhysicsObject() { Control = temp, Top = Canvas.GetTop(temp), Left = Canvas.GetLeft(temp), constAccelTop = 500, constAccelLeft = 0, initialVLeft = R1, initialVTop = R2, TopFloor = 1300, rotationDelta = R1, ticksStarted = DateTime.Now.Ticks });

                }

                StaticAdded = false;


                GamePieces.Remove(e.ActivePieceName);


            }
           
        }

        async void Engine_GamePieceMoved(object sender, Rotetris.RotetrisEventArgs e)
        {
            if (GamePieces.ContainsKey(e.ActivePieceName))
            {
              // 555 added await?
               await AddMovementAnimation(GamePieces[e.ActivePieceName], new Point(Canvas.GetTop(GamePieces[e.ActivePieceName]), Canvas.GetLeft(GamePieces[e.ActivePieceName])), new Point(Convert.ToInt32(e.P.X) * (GearSize + vSpace), Convert.ToInt32(e.P.Y) * (GearSize + hSpace)), 0.08);
            }
        }

       

        async void Engine_GameStaticBlockAdded(object sender, Rotetris.RotetrisEventArgs e)
        {

            StaticAdded  = true;

            var IC = await AddGameBlock(String.Format("{0}{1}{2}{3}", e.Contents[0, 0], e.Contents[0, 1],e.Contents[1,0],e.Contents[1,1]));

            GameBoard.Children.Add(IC);
            //Grid.SetRow(IC, Convert.ToInt32(e.P.X)); // X is row
            //Grid.SetColumn(IC, Convert.ToInt32(e.P.Y)); // Y is col

            Canvas.SetLeft(IC, Convert.ToInt32(e.P.Y) * (GearSize + hSpace));
            Canvas.SetTop(IC, Convert.ToInt32(e.P.X) * (GearSize + vSpace));

            StaticBlocks.Add(new Tuple<ItemsControl, int, int>(IC, Convert.ToInt32(e.P.X), Convert.ToInt32(e.P.Y)));

            if (GameOver == true)
            {
                AddOpacityAnimation(IC, 0, 1, 2);
            }
          
        }

        Dictionary<string, ItemsControl> GamePieces = new Dictionary<string,ItemsControl>();

        string LastPieceAddedName;

        async void Engine_GamePieceAdded(object sender, Rotetris.RotetrisEventArgs e)
        {


            var IC = await AddGameBlock(String.Format("{0}{1}{2}{3}", e.Contents[0, 0], e.Contents[0, 1], e.Contents[1, 0], e.Contents[1, 1]));

            GameBoard.Children.Add(IC);
            //Grid.SetRow(IC, Convert.ToInt32(e.P.X)); // X is row
            //Grid.SetColumn(IC, Convert.ToInt32(e.P.Y)); // Y is col

            Canvas.SetLeft(IC, Convert.ToInt32(e.P.Y) * (GearSize + hSpace));
            Canvas.SetTop(IC, Convert.ToInt32(e.P.X) * (GearSize + vSpace));

            if (!GamePieces.ContainsKey(e.ActivePieceName))
                GamePieces[e.ActivePieceName] = IC;

            LastPieceAddedName = e.ActivePieceName;

        }


        private async Task<Storyboard> AddMovementAnimation(ItemsControl Obj, Point currentValue, Point finalValue, double secsDuration)
        {

            

            var myDoubleAnimation = new DoubleAnimation();
            myDoubleAnimation.From = currentValue.X;


            myDoubleAnimation.To = finalValue.X;

            //AddEasing(currentValue.X, finalValue.X, secsDuration, myDoubleAnimation);
            myDoubleAnimation.Duration = new Duration(TimeSpan.FromSeconds(secsDuration));


            var myDoubleAnimation2 = new DoubleAnimation();
            myDoubleAnimation2.From = currentValue.Y;


            myDoubleAnimation2.To = finalValue.Y;
            myDoubleAnimation2.Duration = new Duration(TimeSpan.FromSeconds(secsDuration));


            //AddEasing(currentValue.Y, finalValue.Y, secsDuration, myDoubleAnimation2);


            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(myDoubleAnimation);
            storyboard.Children.Add(myDoubleAnimation2);


            Storyboard.SetTarget(myDoubleAnimation2, Obj);
            Storyboard.SetTargetProperty(myDoubleAnimation2, "(Canvas.Left)");

            Storyboard.SetTarget(myDoubleAnimation, Obj);
            Storyboard.SetTargetProperty(myDoubleAnimation, "(Canvas.Top)");





            storyboard.Begin();

            return storyboard;

        }

        async private static void AddEasing(double currentValue, double finalValue, double secsDuration, DoubleAnimationUsingKeyFrames myDoubleAnimation)
        {
            var Easing1 = new EasingDoubleKeyFrame();
            Easing1.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0));
            Easing1.Value = currentValue;

            var Easing2 = new EasingDoubleKeyFrame();
            Easing2.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(secsDuration));
            Easing2.Value = finalValue;

            var E1 = new CircleEase();

            Easing2.EasingFunction = E1;
          //  E1. = 1;
           // E1.Springiness = 2;
            Easing2.EasingFunction.EasingMode = EasingMode.EaseIn;



            myDoubleAnimation.KeyFrames.Add(Easing1);
            myDoubleAnimation.KeyFrames.Add(Easing2);
        }



        private async Task<Storyboard> AddOpacityAnimation(FrameworkElement Obj, double currentValue, double finalValue, double secsDuration)
        {
            DoubleAnimation myDoubleAnimation = new DoubleAnimation();
            myDoubleAnimation.From = currentValue;


            myDoubleAnimation.To = finalValue;
            myDoubleAnimation.Duration = new Duration(TimeSpan.FromSeconds(secsDuration));


       





            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(myDoubleAnimation);
         

            Storyboard.SetTarget(myDoubleAnimation, Obj);
            Storyboard.SetTargetProperty(myDoubleAnimation, "Opacity");





            storyboard.Begin();

            return storyboard;

        }

        private async Task<Storyboard> AddDoubleAnimation(Viewbox Obj, double currentValue, double finalValue, double secsDuration, string propname)
        {
            DoubleAnimation myDoubleAnimation = new DoubleAnimation();
            myDoubleAnimation.From = currentValue;


            myDoubleAnimation.To = finalValue;
            myDoubleAnimation.Duration = new Duration(TimeSpan.FromSeconds(secsDuration));








            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(myDoubleAnimation);


            Storyboard.SetTarget(myDoubleAnimation, Obj);
            Storyboard.SetTargetProperty(myDoubleAnimation, propname);





            storyboard.Begin();

            return storyboard;

        }

        private async Task<Storyboard> AddRotationAnimation(RotateTuple R, double OldVal, double secsDuration)
        {
            DoubleAnimation myDoubleAnimation = new DoubleAnimation();
            myDoubleAnimation.From = OldVal;

            if (R.FinalValue == 360.00)
            {
                R.FinalValue = 359.99;
                R.Transform.Angle = 0;
            }

            if (R.FinalValue == -360.00)
            {
                R.FinalValue = -359.99;
                R.Transform.Angle = 0;
            }

            myDoubleAnimation.To = R.FinalValue;
            myDoubleAnimation.Duration = new Duration(TimeSpan.FromSeconds(secsDuration));


           



          
            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(myDoubleAnimation);


            Storyboard.SetTarget(myDoubleAnimation, R.Transform);
            Storyboard.SetTargetProperty(myDoubleAnimation, "Angle");

           



            storyboard.Begin();

            return storyboard;


        }



        private async Task<ItemsControl> AddGameBlock(string BlockContents)
        {
            var IC = new ItemsControl();
            ObservableCollection<UIGear> OC = new ObservableCollection<UIGear>();
            var newGear = new UIGear();

            IC.Name = newGear.Name; // Set name to guid for animation target

           

            newGear.Fills = LetterColorMap[BlockContents[3]];
            newGear.Fills1 = LetterColorMap[BlockContents[2]];
            newGear.Fills2 = LetterColorMap[BlockContents[0]];
            newGear.Fills3 = LetterColorMap[BlockContents[1]];

            newGear.FillsOutline = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));
            newGear.FillsOutline2 = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 22, 22, 22));

            newGear.Width = 48;
            newGear.Height = 48;

            IC.Width = newGear.Width;
            IC.Height = newGear.Height;

            newGear.SetGeometry(43, 50, 0, 8, 0.15);

            // Bind the new gear to the placeholder control
            OC.Add(newGear);
            IC.ItemsSource = OC;
            IC.ItemTemplate = Application.Current.Resources["GearViewBox"] as DataTemplate;
      
            
            return IC;
        }


        private ItemsControl AddBackgroundGear(string BlockContents)
        {

            // draw a random background gear

            Random R = new Random();



            var IC = new ItemsControl();
            ObservableCollection<UIGear> OC = new ObservableCollection<UIGear>();
            var newGear = new UIGear();

            IC.Name = newGear.Name; // Set name to guid for animation target

          

            newGear.Fills = new SolidColorBrush(Windows.UI.Colors.Black);
            newGear.Fills1 = new SolidColorBrush(Windows.UI.Colors.Black);
            newGear.Fills2 = new SolidColorBrush(Windows.UI.Colors.Black);
            newGear.Fills3 = new SolidColorBrush(Windows.UI.Colors.Black);

            newGear.FillsOutline = new SolidColorBrush(Windows.UI.Colors.Black);
            newGear.FillsOutline2 = new SolidColorBrush(Windows.UI.Colors.Black);


            double W = GameBoard.Width / 2;
            double H = GameBoard.Height / 2;

            newGear.Width = R.Next() % (int)GameBoard.Width + 200;
            newGear.Height = newGear.Width;

            IC.Opacity = Math.Min(newGear.Width / GameBoard.Width, 1.0);

            IC.Width = newGear.Width;
            IC.Height = newGear.Height;

            var Left = R.Next() % 3 / 10.0 + 0.1;
            var Top = R.Next() % 4 / 10.0 + Left;
            var Right = R.Next() % 4 / 10.0 + Top;

            newGear.SetGeometry(R.Next() % 10 + 39, 50, R.Next() % 20, R.Next() % 50, 0.001, Left,Top ,Right);

            OC.Add(newGear);
            IC.ItemsSource = OC;
            IC.ItemTemplate = Application.Current.Resources["GearViewBox"] as DataTemplate;
           

            Canvas.SetLeft(IC, R.Next() % W);
            Canvas.SetTop(IC, R.Next() % H);

            //IC.Fill = new SolidColorBrush(Windows.UI.Colors.Yellow);
            return IC;
        }

       

        async void  Engine_GameWon(object sender, Rotetris.RotetrisEventArgs e)
        {
           
            await Rotetris.RecentState.ClearRecentState();
            GameOver = true;
        }

        async void Engine_GameLost(object sender, Rotetris.RotetrisEventArgs e)
        {
            
            // delete saved state
            await Rotetris.RecentState.ClearRecentState();


            // Cause game to "explode"
            foreach (var v in StaticBlocks)
            {
               
                var R1 = (R.Next() % 1000) - 500;
                var R2 = -1 * ((R.Next() % 200) + 100);

                PhysicsObjects.Add(new PhysicsObject() { Control = v.Item1, Top = Canvas.GetTop(v.Item1), Left = Canvas.GetLeft(v.Item1), constAccelTop = 500, constAccelLeft = 0, initialVLeft = R1, initialVTop = R2, TopFloor = 1300, rotationDelta = R1, ticksStarted = DateTime.Now.Ticks });

            }


            GameOver = true;
           
        }

       async void Engine_GameBoardChanged(object sender, Rotetris.RotetrisEventArgs e)
        {
            // Ignore if the state isn't current
            if (GameTickResponsedTo < e.Ticks)
            {
                GameTickResponsedTo = e.Ticks;
              
            }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
        }

        long lastKeyTick = DateTime.Now.Ticks;

        // Copy of other key down function. Refactor this out eventually
        private async void pageRoot_KeyDown_1(object sender, KeyRoutedEventArgs e)
        {
            if ((new TimeSpan(DateTime.Now.Ticks - lastKeyTick).TotalSeconds > 0.09))
            {
                lastKeyTick = DateTime.Now.Ticks;
                Engine.SendKeyUpperCase(e.Key.ToString().ToUpper().ToCharArray()[0]);

                if (e.Key.ToString().ToUpper().Equals("R")) {
                    await AddDoubleAnimation(RotateR, 0, 0.5, 0.2, "Opacity");
                    
                }

                if (e.Key.ToString().ToUpper().Equals("Q"))
                {
                    await AddDoubleAnimation(RotateQ, 0, 0.5, 0.2, "Opacity");

                }
            }

            e.Handled = true;
        }

        // Handle touch events for click and drag
        private void GameBoard_PointerMoved_1(object sender, PointerRoutedEventArgs e)
        {
            if ((lastPieceTouched != null) && (lastPieceTouched.Equals(LastPieceAddedName) && (GamePieces.ContainsKey(lastPieceTouched))))
            {
                var CurrPiece = GamePieces[lastPieceTouched];

                if (e.Pointer.PointerDeviceType != Windows.Devices.Input.PointerDeviceType.Mouse ||  (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse && e.GetCurrentPoint(GameBoard).Properties.IsLeftButtonPressed))
                {
                    var col = (int)Math.Floor( (e.GetCurrentPoint(GameBoard).Position.X) / (GearSize+hSpace));
                    var row = (int) Math.Floor(e.GetCurrentPoint(GameBoard).Position.Y / (GearSize + vSpace));

                  

                    var Res = Engine.MoveUserBlock(row, col);


                    // Timebar turns red if they try to drag above it
                    if (!Res)
                        topRow.Fill = new SolidColorBrush(Windows.UI.Colors.Red);
                    else
                    {
                        Canvas.SetLeft(CurrPiece, col * (GearSize + hSpace));
                        Canvas.SetTop(CurrPiece, row * (GearSize + vSpace));
                        topRow.Fill = new SolidColorBrush(Windows.UI.Colors.DimGray);
                    }

                    
                }

               
            }

            e.Handled = true;
        }

        
        string lastPieceTouched = null;
        private void GameBoard_PointerPressed_1(object sender, PointerRoutedEventArgs e)
        {
           
            topRow.Opacity = 1;

            lastPieceTouched = LastPieceAddedName;

            GameBoard.PointerMoved += GameBoard_PointerMoved_1;

            e.Handled = true;
        }

        private void GameBoard_PointerReleased_1(object sender, PointerRoutedEventArgs e)
        {
           
            topRow.Opacity = 0.1;

            lastPieceTouched = null;

            GameBoard.PointerMoved -= GameBoard_PointerMoved_1;

            e.Handled = true;
        }

        private void Viewbox_PointerPressed_1(object sender, PointerRoutedEventArgs e)
        {
            AddDoubleAnimation(RotateQ, 0.1, 0.5, 0.1, "Opacity");
            lastKeyTick = DateTime.Now.Ticks;
            Engine.SendKeyUpperCase('Q');
          
           
        }

        private void Viewbox_PointerPressed_2(object sender, PointerRoutedEventArgs e)
        {
            AddDoubleAnimation(RotateR, 0.1, 0.5, 0.1, "Opacity");
            lastKeyTick = DateTime.Now.Ticks;
            Engine.SendKeyUpperCase('R');
           
        }

       
        // Pause button
        private async void Pause_PointerPressed_1(object sender, PointerRoutedEventArgs e)
        {
            if (UserPaused)
            {

                    UserPaused = false;

                   // Pause.Opacity = 0.5;

                    AddOpacityAnimation(Pause, 1, 0.5, 0.5);
                    textTimer.Text = oldTextTimerVal;
               

                     if (!GameOver) // not in win state
                         Engine.Resume();
                    if (rootMediaElement != null)
                        rootMediaElement.Play();
              
               
            }
            else
            {
                UserPaused = true;

                (await AddOpacityAnimation(Pause, 0.5, 1, 0.5)).RepeatBehavior = RepeatBehavior.Forever;
                
                Engine.Pause();

                oldTextTimerVal = textTimer.Text;
                textTimer.Text = "Paused";

                if (rootMediaElement != null)
                    rootMediaElement.Pause();
            }

        }

      
    }
}
