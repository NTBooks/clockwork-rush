using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Media.Animation;
using Windows.Storage;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Popups;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace GearMaker
{

    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class MainPage : GearMaker.Common.LayoutAwarePage
    {

        public MediaElement rootMediaElement; 



        public MainPage()
        {
            this.InitializeComponent();

            Windows.Graphics.Display.DisplayProperties.AutoRotationPreferences = Windows.Graphics.Display.DisplayOrientations.Landscape;



          
        }


        private void RotateBlock(ItemsControl v, double offset, double duration)
        {
            RotateTransform rotateTransform1 = new RotateTransform();

            rotateTransform1.CenterX = v.Width / 2.0;
            rotateTransform1.CenterY = v.Height / 2.0;

            var oldRot = v.Tag != null ? ((RotateTuple)v.Tag).FinalValue : 0;
           

            var OldRotProp = v.Tag != null ? ((RotateTuple)v.Tag).Transform.Angle : 0;

            v.Tag = new RotateTuple(oldRot + offset, rotateTransform1);


            v.RenderTransform = rotateTransform1; //


            AddRotationAnimation((RotateTuple)v.Tag, OldRotProp, duration);
        }

        private Storyboard AddRotationAnimation(RotateTuple R, double OldVal, double secsDuration)
        {
            DoubleAnimation myDoubleAnimation = new DoubleAnimation();
            myDoubleAnimation.From = OldVal;

            //if (R.FinalValue == 360.00)
            //{
            //    R.FinalValue = 359.99;
            //    R.Transform.Angle = 0;
            //}

            //if (R.FinalValue == -360.00)
            //{
            //    R.FinalValue = -359.99;
            //    R.Transform.Angle = 0;
            //}

            myDoubleAnimation.To = R.FinalValue;
            myDoubleAnimation.Duration = new Duration(TimeSpan.FromSeconds(secsDuration));







            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(myDoubleAnimation);


            Storyboard.SetTarget(myDoubleAnimation, R.Transform);
            Storyboard.SetTargetProperty(myDoubleAnimation, "Angle");





            storyboard.Begin();

            storyboard.RepeatBehavior = new RepeatBehavior(100);

            return storyboard;


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

        //private void DrawGear_Click_1(object sender, RoutedEventArgs e)
        //{
        //    var innerRadius = double.Parse(InnerRadius.Text);
        //    var outerRadius = double.Parse(OuterRadius.Text); ;
        //    var holeRadius = double.Parse(HoleRadius.Text); ;
        //    var teeth = int.Parse(Teeth.Text); ;

        //    //var holeRadius = 10.0;

        //    // DrawSurface.Children.Clear();

        //    //  Windows.UI.Xaml.Shapes.Path path = new Windows.UI.Xaml.Shapes.Path();


        //    var myPathGeo = MakeGear(innerRadius, outerRadius, holeRadius, teeth);

        //    //PathFigure StartPoint="10,50">
        //    //             <PathFigure.Segments>
        //    //                 <BezierSegment Point1="100,0"
        //    //                                Point2="200,200"
        //    //                                Point3="300,100"/>
        //    //                 <LineSegment Point="400,100" />
        //    //                 <ArcSegment Size="50,50" 
        //    //                             RotationAngle="45"
        //    //                             IsLargeArc="True" 
        //    //                             SweepDirection="Clockwise"
        //    //                             Point="200,100"/>
        //    //             </PathFigure.Segments>
        //    //myPathGeo.Figures.Add(myPath);
        //    //path.Data = myPathGeo;
        //    SinglePath.Data = myPathGeo;
        //    //DrawSurface.Children.Add(path);

        //    AnimateRectangle.Begin();
        //    spin.Begin();
        //}


        //private static PathGeometry MakeGear(double innerRadius, double outerRadius, double holeRadius, int teeth, double gearLeftAngleStop = 0.1, double gearTopStop = 0.4, double gearRightAngleStop = 0.5)
        //{
        //    var myPathGeo = new Windows.UI.Xaml.Media.PathGeometry();


        //    Func<double, double, Point> Translate = (x, y) =>
        //    {
        //        return new Point(x + 50, y + 50);
        //    };



        //    for (int i = 0; i < teeth; i++)
        //    {
        //        var myPath = new PathFigure();


        //        var angle = (i * 360.0 / teeth) * Math.PI / 180.0; // starting angle
        //        var endAngle = ((i + 1) * 360.0 / teeth) * Math.PI / 180.0;
        //        var startPos = Translate(Math.Cos(angle) * innerRadius, Math.Sin(angle) * innerRadius);
        //        var endPos = Translate(Math.Cos(endAngle) * innerRadius, Math.Sin(endAngle) * innerRadius);


        //        myPath.StartPoint = Translate(Math.Cos(angle) * holeRadius, Math.Sin(angle) * holeRadius);

        //        var angleLengths = new { Q1 = (endAngle - angle) * 0.1, Q2 = (endAngle - angle) * 0.4, Q3 = (endAngle - angle) * 0.5 };

               
        //            var LineUp0 = new LineSegment();
        //            LineUp0.Point = Translate(Math.Cos(angle) * innerRadius, Math.Sin(angle) * innerRadius);
        //            myPath.Segments.Add(LineUp0);
              

        //        var LineUp = new LineSegment();
        //        LineUp.Point = Translate(Math.Cos(angle + angleLengths.Q1) * outerRadius, Math.Sin(angle + angleLengths.Q1) * outerRadius);
        //        myPath.Segments.Add(LineUp);

        //        var QuarterArc = new ArcSegment();
        //        QuarterArc.IsLargeArc = false;
        //        QuarterArc.Point = Translate(Math.Cos(angle + angleLengths.Q2) * outerRadius, Math.Sin(angle + angleLengths.Q2) * outerRadius); ;
        //        QuarterArc.SweepDirection = SweepDirection.Clockwise;
        //        QuarterArc.Size = new Size(outerRadius, outerRadius); // Circle arc segment
        //        myPath.Segments.Add(QuarterArc);

        //        var LineDown = new LineSegment();
        //        LineDown.Point = Translate(Math.Cos(angle + angleLengths.Q3) * innerRadius, Math.Sin(angle + angleLengths.Q3) * innerRadius);
        //        myPath.Segments.Add(LineDown);


        //        var HalfArc = new ArcSegment();
        //        HalfArc.IsLargeArc = false;
        //        HalfArc.Point = Translate(Math.Cos(endAngle) * innerRadius, Math.Sin(endAngle) * innerRadius); ;
        //        HalfArc.SweepDirection = SweepDirection.Clockwise;
        //        HalfArc.Size = new Size(innerRadius, innerRadius); // Circle arc segment
        //        myPath.Segments.Add(HalfArc);

        //        if (holeRadius > 0)
        //        {
        //            var LineDown2 = new LineSegment();
        //            LineDown2.Point = Translate(Math.Cos(endAngle) * holeRadius, Math.Sin(endAngle) * holeRadius);
        //            myPath.Segments.Add(LineDown2);



        //            var HalfArc2 = new ArcSegment();
        //            HalfArc2.IsLargeArc = false;
        //            HalfArc2.Point = Translate(Math.Cos(angle) * holeRadius, Math.Sin(angle) * holeRadius); ;
        //            HalfArc2.SweepDirection = SweepDirection.Counterclockwise;

        //            HalfArc2.Size = new Size(holeRadius, holeRadius); // Circle arc segment
        //            myPath.Segments.Add(HalfArc2);
        //        }


        //        myPathGeo.Figures.Add(myPath);

        //        //var FullArc = new ArcSegment();
        //        //FullArc.IsLargeArc = false;
        //        //FullArc.Point = endPos;
        //        //FullArc.SweepDirection = SweepDirection.Clockwise;
        //        //FullArc.Size = new Size(innerRadius, innerRadius); // Circle arc segment






        //    }
        //    return myPathGeo;
        //}

       // GameOptions StartingOptions = new GameOptions();

        private void StartGame(object sender, RoutedEventArgs e)
        {

            GameOptions.Rows = Convert.ToInt32(SliderRow.Value) * 2;
            GameOptions.Cols = Convert.ToInt32(SliderCol.Value) * 2;
            GameOptions.Colors = Convert.ToInt32(SliderColors.Value);
            GameOptions.Speed = 1000 + (((3 - Convert.ToInt32(SliderSpeed.Value)) * 200)); // ms value for speed
            GameOptions.StartingRows = Convert.ToInt32(SliderStartRows.Value);

            this.Frame.Navigate(typeof(GamePage),"New");


           // Application.Current.Exit();
        }

      protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            ((ObservableCollection<UIGear>)Gear1.ItemsSource).Clear();
            ((ObservableCollection<UIGear>)Gear2.ItemsSource).Clear();
            ((ObservableCollection<UIGear>)Gear3.ItemsSource).Clear();
            ((ObservableCollection<UIGear>)Gear4.ItemsSource).Clear();

            //Gear1.ItemsSource = null;
            //Gear2.ItemsSource = null;
            //Gear3.ItemsSource = null;
            //Gear4.ItemsSource = null;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            SliderRow.Value = GameOptions.Rows >> 1;
            SliderCol.Value = GameOptions.Cols >> 1;
            SliderColors.Value = GameOptions.Colors;
            SliderSpeed.Value = 3-  ((GameOptions.Speed - 1000) / 200);
            SliderStartRows.Value = GameOptions.StartingRows;

                SetSelected(S_OFF, !GameOptions.SFX);
                SetSelected(S_ON, GameOptions.SFX);

                    SetSelected(M_ATYPE, GameOptions.Music.Equals("A"));
                    SetSelected(M_BTYPE, GameOptions.Music.Equals("B"));
                    SetSelected(M_NONE, GameOptions.Music.Equals(""));




            //var f  =Rotetris.Rotetris_Engine.GetStateFile("SaveState.xml") ;
            //        if (f != null && f.Result.Length > 0)
            //        {
            //            // Show load button
                    //           ResumeButton.Visibility = Visibility.Collapsed;
            //        }
            //        else
            //        {
            //            ResumeButton.Visibility = Visibility.Collapsed;
            //        }
                    ResumeButton.Visibility = Visibility.Collapsed;

            var StateCheck = await Rotetris.RecentState.LoadState("SaveState.xml");

           // StateCheck.RunSynchronously();


                 if (StateCheck.Initialized)
                    {
                        ResumeButton.Visibility = Visibility.Visible;
                    }
                   
            
            //    CheckStateFile().Wait();// Currently not closing streams properly
               // HANGS HERE ^^ **************************************************
        }

        //async Task<StorageFile> CheckStateFile()
        //{
        //    StorageFile file;

        //    Stream v;
        //    try
        //    {
        //        file = await ApplicationData.Current.LocalFolder.GetFileAsync("SaveState.xml");
        //        //var v = await file.OpenStreamForReadAsync();

               

        //        //if (v.Length > 0)
        //        //{
        //        //    ResumeButton.Visibility = Visibility.Visible;
        //        //}
        //        //else
        //        //{
                  
        //        //    file.DeleteAsync();
                   
        //        //}

        //        //v.Dispose();
        //    }
        //    catch (Exception fnf)
        //    {
        //        file = null;
        //        ResumeButton.Visibility = Visibility.Collapsed;
        //    }

        //    return file;
            
        //}

        private void pageRoot_Loaded_1(object sender, RoutedEventArgs e)
        {
            DependencyObject rootGrid = VisualTreeHelper.GetChild(Window.Current.Content, 0);
     rootMediaElement = (MediaElement)VisualTreeHelper.GetChild(rootGrid, 0);


     ObservableCollection<UIGear> OC = new ObservableCollection<UIGear>();
     var newGear = new UIGear();

     ObservableCollection<UIGear> OC1 = new ObservableCollection<UIGear>();
     var newGear1 = new UIGear();

     ObservableCollection<UIGear> OC2 = new ObservableCollection<UIGear>();
     var newGear2 = new UIGear();


     ObservableCollection<UIGear> OC3 = new ObservableCollection<UIGear>();
     var newGear3 = new UIGear();



     // Gear1.Name = newGear.Name; // Set name to guid for animation target

     // Idea -- draw a full gear and reduce the radius for colored parts -- should create an outline around it

     //newGear.Fills = LetterColorMap[BlockContents[3]];
     //newGear.Fills1 = LetterColorMap[BlockContents[2]];
     //newGear.Fills2 = LetterColorMap[BlockContents[0]];
     //newGear.Fills3 = LetterColorMap[BlockContents[1]];

     //newGear.FillsOutline = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));
     //newGear.FillsOutline2 = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 22, 22, 22));

     //newGear.Width = 120;
     //newGear.Height = 120;

     //Gear1.Width = newGear.Width;
     //Gear1.Height = newGear.Height;

     // newGear.SetGeometry(43, 50, 0, 8, 0.15);
     newGear.RandomInverted();
     newGear1.RandomNormal();
     newGear2.RandomSpike();
     newGear3.RandomNormal();

     Gear1.Width = newGear.Width = 100;
     Gear1.Height = newGear.Height = 100;
     newGear.FillsOutline2 = newGear.FillsOutline = newGear.Fills3 = newGear.Fills2 = newGear.Fills1 = newGear.Fills = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 109, 126, 255));
     newGear1.FillsOutline2 = newGear1.FillsOutline = newGear1.Fills3 = newGear1.Fills2 = newGear1.Fills1 = newGear1.Fills = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 130, 204, 109));

     newGear2.FillsOutline2 = newGear2.FillsOutline = newGear2.Fills3 = newGear2.Fills2 = newGear2.Fills1 = newGear2.Fills = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 224, 163, 62));
     newGear3.FillsOutline2 = newGear3.FillsOutline = newGear3.Fills3 = newGear3.Fills2 = newGear3.Fills1 = newGear3.Fills = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 233, 119, 95));

     //LetterColorMap.Add('B', new SolidColorBrush(Windows.UI.Color.FromArgb(255, 130, 204, 109)));
     //LetterColorMap.Add('C', new SolidColorBrush(Windows.UI.Color.FromArgb(255, 224, 163, 62)));
     //LetterColorMap.Add('D', new SolidColorBrush(Windows.UI.Color.FromArgb(255, 233, 119, 95)));

     Gear2.Width = newGear1.Width = 160;
     Gear2.Height = newGear1.Height = 160;

     Gear3.Width = newGear2.Width = 300;
     Gear3.Height = newGear2.Height = 300;
     Gear4.Width = newGear3.Width = 400;
     Gear4.Height = newGear3.Height = 400;

     OC.Add(newGear);
     OC1.Add(newGear1);
     OC2.Add(newGear2);
     OC3.Add(newGear3);
     Gear1.ItemsSource = OC;
     Gear1.ItemTemplate = Application.Current.Resources["GearViewBox"] as DataTemplate;

     RotateBlock(Gear1, 360, 15);

     Gear2.ItemsSource = OC1;
     Gear2.ItemTemplate = Application.Current.Resources["GearViewBox"] as DataTemplate;

     RotateBlock(Gear2, -360, 10);

     Gear3.ItemsSource = OC2;
     Gear3.ItemTemplate = Application.Current.Resources["GearViewBox"] as DataTemplate;

     RotateBlock(Gear3, 360, 30);

     Gear4.ItemsSource = OC3;
     Gear4.ItemTemplate = Application.Current.Resources["GearViewBox"] as DataTemplate;

     RotateBlock(Gear4, -360, 30);

        }

        void SetSelected(Button B, bool Selected)
        {
            if (Selected)
            {
                B.Background = new SolidColorBrush(Windows.UI.Colors.White);
                B.Foreground = new SolidColorBrush(Windows.UI.Colors.Black);
            }
            else
            {
                B.Background = new SolidColorBrush(Windows.UI.Colors.Transparent);
                B.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
            }
        }

        private void S_ON_Click_1(object sender, RoutedEventArgs e)
        {
            //S_OFF.Background = new SolidColorBrush(Windows.UI.Colors.Transparent);
            //S_OFF.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
           
            //S_ON.Background = new SolidColorBrush(Windows.UI.Colors.White);
            //S_ON.Foreground = new SolidColorBrush(Windows.UI.Colors.Black);

            SetSelected(S_OFF, false);
            SetSelected(S_ON, true);

            GameOptions.SFX = true;

        }

        private void S_OFF_Click_1(object sender, RoutedEventArgs e)
        {
            SetSelected(S_OFF, true);
            SetSelected(S_ON, false);

            GameOptions.SFX = false;
        }

        private void M_ATYPE_Click_1(object sender, RoutedEventArgs e)
        {
            SetSelected(M_ATYPE, true);
            SetSelected(M_BTYPE, false);
            SetSelected(M_NONE, false);

            //DependencyObject rootGrid = VisualTreeHelper.GetChild(Window.Current.Content, 0);
            //var _currentTrack = (MediaElement)VisualTreeHelper.GetChild(rootGrid, 0);

            if (rootMediaElement != null)
                rootMediaElement.Source = new Uri("ms-appx:///Sounds/NickMade.mp3");

            GameOptions.Music = "A";
        }

        private void M_BTYPE_Click_1(object sender, RoutedEventArgs e)
        {
            SetSelected(M_ATYPE, false);
            SetSelected(M_BTYPE, true);
            SetSelected(M_NONE, false);

            //var rootGrid = VisualTreeHelper.GetChild(Window.Current.Content, 0);
            //var mediaElement = (MediaElement)VisualTreeHelper.GetChild(rootGrid, 0);


            if (rootMediaElement != null)
                rootMediaElement.Source = new Uri("ms-appx:///Sounds/PowerhouseHouse.mp3");
            //mediaElement.Source = new Uri("ms-appx:///Sounds/PowerhouseHouse.mp3");

            GameOptions.Music = "B";
        }

        private void M_NONE_Click_1(object sender, RoutedEventArgs e)
        {
            SetSelected(M_ATYPE, false);
            SetSelected(M_BTYPE, false);
            SetSelected(M_NONE, true);


            if (rootMediaElement != null)
                rootMediaElement.Source = null;

            GameOptions.Music = "";
        }

        private void Controls_Click_1(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(HelpRoot));
        }

        private void Resume_Click_1(object sender, RoutedEventArgs e)
        {

            this.Frame.Navigate(typeof(GamePage), "SaveState.xml");
        }

        private void D_Beginner_Click_1(object sender, RoutedEventArgs e)
        {
           
            SetDifficulty(sender, 12, 10, 2, 2, 1);


        }

        private void SetDifficulty(object sender, int SRows, int SCols, int SColors, int SSpeed, int SStartRows)
        {

            SliderCol.IsEnabled = (sender == D_Custom);
            SliderRow.IsEnabled = (sender == D_Custom);
            SliderSpeed.IsEnabled = (sender == D_Custom);
            SliderStartRows.IsEnabled = (sender == D_Custom);
            SliderColors.IsEnabled = (sender == D_Custom);

            SetSelected(D_Beginner, sender == D_Beginner);
            SetSelected(D_Normal, sender == D_Normal);
            SetSelected(D_Expert, sender == D_Expert);
            SetSelected(D_Custom, sender == D_Custom);

            SliderCol.Value = SCols;
            SliderRow.Value = SRows;
            SliderColors.Value = SColors;
            SliderSpeed.Value = SSpeed;
            SliderStartRows.Value = SStartRows;
        }

        private void D_Normal_Click_1(object sender, RoutedEventArgs e)
        {
            SetDifficulty(sender, 10, 20, 4, 3, 3);
        }

        private void D_Expert_Click_1(object sender, RoutedEventArgs e)
        {
            // Not recommended for slower devices
            SetDifficulty(sender, 12, 30, 4, 5, 4);
        }

        private void D_Custom_Click_1(object sender, RoutedEventArgs e)
        {
            SetDifficulty(sender, Convert.ToInt32(SliderRow.Value),Convert.ToInt32( SliderCol.Value),Convert.ToInt32( SliderColors.Value),Convert.ToInt32( SliderSpeed.Value),Convert.ToInt32( SliderStartRows.Value));
        }

        private async void pageRoot_KeyDown_1(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key.Equals(VirtualKey.Escape))
            {
                Windows.UI.Popups.MessageDialog dlg = new Windows.UI.Popups.MessageDialog("Are you sure you want to quit the application?", "Exit");

                dlg.Commands.Add(new UICommand("Quit", new UICommandInvokedHandler((o) => { Application.Current.Exit(); })));
                dlg.Commands.Add(new UICommand("Cancel", new UICommandInvokedHandler((o) => {  })));

                await dlg.ShowAsync();

               
            }
        }
    }

    public class DivideBy2Converter : IValueConverter
    {
        

        public object Convert(object value, Type targetType,
              object parameter, string culture)
        {
            return (int)value / 2;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, string culture)
        {
            return null;
        }
    }
}
