using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Rotetris_Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        int Matches = 0;
       

        const int Cols = 24;
        const int Rows = 24;

        char[,] Model = new char[Rows, Cols];
        Shape[,] ModelView = new Shape[Rows, Cols];

        // Add events for:
        // Glance Left, Glance Right, Merge, Tick, Remove

        class ActiveBlock {
            public int posRow = 0;
            public int posCol = 0;
            public char[,] block = new char[2, 2];
            public string Pattern;

            public ActiveBlock(string Pattern)
            {
                block[0, 0] = Pattern[0];
                block[0, 0 + 1] = Pattern[1];
                block[0 + 1, 0] = Pattern[2];
                block[0 + 1, 0 + 1] = Pattern[3];
                this.Pattern = Pattern;
            }

            public void Move(int offsetRow, int offsetCol) {
                if ((posCol+offsetCol < Cols-1)&&(posCol+offsetCol >= 0)) {
                    posCol += offsetCol;
                }

                if ((posRow+offsetRow < Rows-1)&&(posRow+offsetRow >= 0)) {
                    posRow += offsetRow;
                }
            }
        }

        Brush MapColor(char C)
        {
            switch (C)
            {
                case 'A': return Brushes.Red;
                case 'B': return Brushes.Blue;
                case 'C': return Brushes.Green;
                case 'D': return Brushes.Black;

                default: return Brushes.Transparent;
            }
        }

       

        // States are falling, settled, matching

        // Falling: Update position, check for collisions, adjust

        // Settled: Check for matches, create new block

        // pattern is: top left, top right, bottom left, bottom right
        void AddBlock(int Row, int Col, string Pattern)
        {
            Model[Row, Col] = Pattern[0];
            Model[Row, Col+1] = Pattern[1];
            Model[Row+1, Col] = Pattern[2];
            Model[Row + 1, Col+1] = Pattern[3];
        }

        Random R = new Random();

        string RandomBlock(string Set)
        {
            
            var c1 = Set[R.Next() % Set.Length];
            var c2 = Set[R.Next() % Set.Length];

            var c3 = Set[R.Next() % Set.Length];
            var c4 = Set[R.Next() % Set.Length];

            return String.Join("",new char[]{c1, c2, c3, c4});
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {


            PlayArea.RowDefinitions.Clear();
            PlayArea.ColumnDefinitions.Clear();
            PlayArea.Children.Clear();

            for (int i = 0; i < Rows; i++)
            {
                var StandardRowDef = new RowDefinition();
                StandardRowDef.Height = new GridLength(15);
                PlayArea.RowDefinitions.Add(StandardRowDef);

               
            }

            for (int i = 0; i < Cols; i++)
            {
                var StandardColDef = new ColumnDefinition();
                StandardColDef.Width = new GridLength(15);
                PlayArea.ColumnDefinitions.Add(StandardColDef);
            }

            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {


                    //PathGeometry pathGeometry = new PathGeometry();
                    //PathFigure figure = new PathFigure();
                    ////figure.StartPoint = new Point(150, 200);
                    //figure.Segments.Add(
                    //new ArcSegment(
                    //new Point(30, 30),
                    //new Size(15, 15), 0,
                    //false,
                    //SweepDirection.Clockwise,
                    //true
                    //)
                    //);
                    //pathGeometry.Figures.Add(figure);
                    //Path path = new Path();
                    //path.Data = pathGeometry;
                    //path.Fill = Brushes.Pink;
                    //path.Stroke = Brushes.Green;



                    var ReportTextbox = new TextBlock();


                   
                    SolidColorBrush mySolidColorBrush = new SolidColorBrush();


                
                    Ellipse myEllipse = new Ellipse();

                    // Create a SolidColorBrush with a red color to fill the  
                    // Ellipse with.
                  

                    // Describes the brush's color using RGB values.  
                    // Each value has a range of 0-255.
                    mySolidColorBrush.Color = Color.FromArgb(255, 255, 255, 0);
                    myEllipse.Fill = mySolidColorBrush;
                    myEllipse.StrokeThickness = 0;
                    myEllipse.Stroke = Brushes.Transparent;
                   

                    // Set the width and height of the Ellipse.
                    myEllipse.Width = 30;
                    myEllipse.Height = 30;
                    

                    Grid.SetRow(myEllipse, i);
                    Grid.SetColumn(myEllipse, j);

                    Model[i, j] = '*';

                    ReportTextbox.Width = 15;
                    ReportTextbox.Height = 15;
                    ReportTextbox.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                    ReportTextbox.VerticalAlignment = System.Windows.VerticalAlignment.Center;

                    PlayArea.Children.Add(myEllipse);

                    ModelView[i, j] = myEllipse;

                   
                }
            }

            //CurrentBlock = new ActiveBlock("ABCD");
            //CurrentBlock.Move(0, 1); // Offset from Even row


            for (int i = 0; i < Cols; i += 2)
            {
                AddBlock(Rows - 2, i, RandomBlock("ABCD"));

            }

            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0,0,1);
            dispatcherTimer.Start();



        }

        ActiveBlock CurrentBlock = null;

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            lock (this.GetType()) {
                if (CurrentBlock != null)
                {
                    if (!CheckCollision(1, 0))
                    {
                        CurrentBlock.Move(1, 0);
                    }
                    else
                    {
                        //MessageBox.Show(String.Join("",GetCollision(1,0)));

                        var Coll = GetCollision(1, 0);

                        if (Coll.Substring(2).Equals("@@"))
                        {
                            HitBottom();
                        }
                       
                        else if (Coll.StartsWith("***")) // hit on right side
                        {
                            GlanceRight();
                        }
                        else if (Coll.StartsWith("**") && Coll.EndsWith("*")) // hit on left side 
                        {
                            GlanceLeft();
                        //else { 

                        //    AddBlock(CurrentBlock.posRow, CurrentBlock.posCol, CurrentBlock.Pattern);
                        //}
                        }
                        else if (CurrentBlock.posRow % 2 == CurrentBlock.posCol % 2 && CurrentBlock.Pattern.Substring(2).Equals(Coll.Substring(2))) // Same row match
                        {
                            // Fuse blocks

                            Matches++;
                            // Set the collision points to the first two characters in the current block's set
                            SetCollision(1, 0, CurrentBlock.Pattern);


                            CurrentBlock = null;
                        }
                        else if (CurrentBlock.Pattern.Substring(2).Equals(Coll.Substring(2))) // Opposite row match
                        {
                            Matches += 3;
                            for (int i = CurrentBlock.posRow + 2; i < CurrentBlock.posRow + 4; i++)
                            {
                                for (int j = Math.Max(CurrentBlock.posCol - 1, 0); j < Math.Min(CurrentBlock.posCol + 3, Cols); j++)
                                {
                                    Model[i, j] = '*';
                                }
                            }

                                CurrentBlock = null;
                        }
                        else if (CurrentBlock.posRow/2 % 2 == CurrentBlock.posCol % 2) { // Same row, non match
                            if ( CurrentBlock.posCol < Cols - 1)
                            {
                                GlanceLeft();
                            }
                            else
                            {
                                GlanceRight();
                            }
                        }
                        else if (Coll.StartsWith("**")) // on non match
                        {
                            AddBlock(CurrentBlock.posRow, CurrentBlock.posCol, CurrentBlock.Pattern);
                            CurrentBlock = null;
                        }

                        
                    }
                }
                else
                {
                    StringBuilder SB = new StringBuilder();
                    for (int i = 0; i < Rows; i++ )
                        for (int j = 0; j < Cols; j++)
                        {
                            if (!Model[i, j].Equals('*'))
                            {
                                SB.Append(Model[i, j]);
                            }
                        }

                    if (SB.Length == 0)
                    {
                        MessageBox.Show("You win!!");
                        ((System.Windows.Threading.DispatcherTimer)sender).Stop();
                        return;
                    }

                        CurrentBlock = new ActiveBlock(RandomBlock(SB.ToString()));
                }
            }
            Draw();
        }


        private void GlanceLeft()
        {
            if (CurrentBlock.posCol < Cols - 2)
            {
                CurrentBlock.posCol++;
            }
            else
            {
                AddBlock(CurrentBlock.posRow, CurrentBlock.posCol, CurrentBlock.Pattern);
                CurrentBlock = null;
            }
        }

        private void GlanceRight()
        {
            if (CurrentBlock.posCol > 1)
            {
                CurrentBlock.posCol--;
            }
            else
            {
                AddBlock(CurrentBlock.posRow, CurrentBlock.posCol, CurrentBlock.Pattern);
                CurrentBlock = null;
            }
        }

        private void HitBottom()
        {
            if (CurrentBlock.posCol % 2 == 1)
            {
                CurrentBlock.posCol++;

            }
            AddBlock(CurrentBlock.posRow, CurrentBlock.posCol, CurrentBlock.Pattern);
            CurrentBlock = null;
        }

        private void Draw()
        {

            MatchesCount.Text = Matches.ToString();
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                  //  ModelView[i, j].Text = Model[i, j].ToString();
                    ModelView[i, j].Fill = MapColor(Model[i, j]);

                    var colOffset = (i/2)%2;


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

                    ModelView[i, j].RenderTransform = new RotateTransform(Angle, 7.5, 7.5);
                }
            }

            if (CurrentBlock != null)
            {
                for (int i = 0; i < 2; i++)
                    for (int j = 0; j < 2; j++)
                    {
                       // ModelView[i + CurrentBlock.posRow, j + CurrentBlock.posCol].Text = CurrentBlock.block[i, j].ToString();
                        ModelView[i + CurrentBlock.posRow, j + CurrentBlock.posCol].Fill = MapColor(CurrentBlock.block[i, j]);
                        var Angle = 0;
                         if (((j ) % 2 == 0) && (i % 2 == 0))
                    {
                        Angle = 0;
                    }
                    if (((j ) % 2 == 1) && (i % 2 == 0))
                    {
                        Angle = 90;
                    }
                    if (((j ) % 2 == 0) && (i % 2 == 1))
                    {
                        Angle = 270;
                    }
                    if (((j ) % 2 == 1) && (i % 2 == 1))
                    {
                        Angle = 180;
                    }
                    ModelView[i + CurrentBlock.posRow, j + CurrentBlock.posCol].RenderTransform = new RotateTransform(Angle, 7.5, 7.5);
                    }
            }
        }

        private bool CheckBounds(int Row, int Col)
        {
            if ((Col >= Cols) || (Col < 0) || (Row >= Rows) || (Row < 0))
            {
                return false;
            }

            return true;
        }

        private bool CheckCollision(int offsetRow, int offsetCol)
        {

            if (CurrentBlock != null)
            {
                for (int i = 0; i < 2; i++)
                    for (int j = 0; j < 2; j++)
                    {


                        if (!CheckBounds(i + CurrentBlock.posRow + offsetRow, j + CurrentBlock.posCol+offsetCol))
                        {
                            return true;
                        }

                        if (Model[i + CurrentBlock.posRow + offsetRow, j + CurrentBlock.posCol + offsetCol] != '*')
                        {
                            return true;
                        }

                       
                    }
            }

            return false;
        }

        private string GetCollision(int offsetRow, int offsetCol)
        {
            char[] retVal = ("****").ToCharArray();
            if (CurrentBlock != null)
            {
                for (int i = 0; i < 2; i++)
                    for (int j = 0; j < 2; j++)
                    {

                        var index = i + (i * 1) + j;
                        if (!CheckBounds(i + CurrentBlock.posRow + offsetRow, j + CurrentBlock.posCol + offsetCol))
                        {
                            retVal[index] = '@';
                            continue;
                        }

                        retVal[index] = Model[i + CurrentBlock.posRow + offsetRow, j + CurrentBlock.posCol + offsetCol];

                    }
            }

            return String.Join("",retVal);
        }

        private void SetCollision(int offsetRow, int offsetCol, string Vals)
        {
            char[] retVal = ("****").ToCharArray();
            if (CurrentBlock != null)
            {
               int i = 1;

                    for (int j = 0; j < 2; j++)
                    {

                     
                        if (!CheckBounds(i + CurrentBlock.posRow + offsetRow, j + CurrentBlock.posCol + offsetCol))
                        {
                           
                            continue;
                        }

                        Model[i + CurrentBlock.posRow + offsetRow, j + CurrentBlock.posCol + offsetCol] = Vals[j];

                    }
            }

           
        }

        // Add Nudge

        private void Window_KeyDown_1(object sender, KeyEventArgs e)
        {
            lock (this.GetType()){
            switch (e.Key){
              case  Key.R:
                    
                    for (int i = Rows - 2; i >= 0; i-=2)
                    {
                        RotateRow(i, i % 2 == 0);
                    }
            
                break;
              case Key.Q:

                for (int i = Rows - 2; i >= 0; i -= 2)
                {
                    RotateRow(i, i % 2 == 1);
                }
                break;
              case Key.W:

                    //if (!CheckCollision(-1, 0))
                    //    CurrentBlock.Move(-2, 0);
                
                break;
              case Key.A:
                if (CurrentBlock != null && !CheckCollision(0, -1))
                    CurrentBlock.Move(0, -1);
                break;
              case Key.S:
                if (CurrentBlock != null && !CheckCollision(1, 0))
                    CurrentBlock.Move(1, 0);
                break;
              case Key.D:
                if (CurrentBlock != null && !CheckCollision(0, 1))
                     CurrentBlock.Move(0, 1);
               
                break;
            }
            }
            Draw();
        

           
        }

        private void RotateRow(int RowNum, bool Clockwise) {

           // if the real row (RowNum / 2) is even, the rotate is off by 1

            var offSet = 0;
            var RealRow = RowNum / 2;
            if ((RealRow+1) % 2 == 0)
            {
                offSet = 1;
            }

            for (int i = (RealRow + 1) % 2; i < Cols - 1; i += 2)
            {

                var C = Model[RowNum, i];
                if (Clockwise)
                {
                    
                    Model[RowNum, i] = Model[RowNum + 1, i];
                    Model[RowNum + 1, i] = Model[RowNum + 1, i + 1];
                    Model[RowNum + 1, i + 1] = Model[RowNum, i + 1];
                    Model[RowNum, i + 1] = C;

                }
                else
                {
                    // 0, 0
                    Model[RowNum, i] = Model[RowNum, i+1];
                    // 0,1 
                    Model[RowNum, i + 1] = Model[RowNum + 1, i+1];
                    // 1, 1
                    Model[RowNum + 1, i + 1] = Model[RowNum + 1, i];

                    // 1, 0
                    Model[RowNum + 1, i] = C;
                }
            }
                
        }
    
    }


}
