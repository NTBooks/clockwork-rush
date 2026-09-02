using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace GearMaker
{
    public class UIGear
    {
        int _Width = 400;
        int _Height = 400;
        int _Angle = 0;

        string _Name;

        PathGeometry[] _GearPaths = new PathGeometry[6];

        SolidColorBrush[] _Fills = new SolidColorBrush[6] { new SolidColorBrush(Windows.UI.Colors.White), new SolidColorBrush(Windows.UI.Colors.White), new SolidColorBrush(Windows.UI.Colors.White), new SolidColorBrush(Windows.UI.Colors.White), new SolidColorBrush(Windows.UI.Colors.White), new SolidColorBrush(Windows.UI.Colors.White) };


        //PathGeometry _GearPaths1;


        public UIGear()
        {
            Name = System.Guid.NewGuid().ToString();
        }

        static Random R = new Random();

        public void RandomInverted()
        {
            var holeRadius = 50;
           
            // inner has to be less than outer which is less than hole
            var innerRadius = R.Next() % 10 + 39;
            var outerRadius = innerRadius - (R.Next() % 10+10);

            var teeth = R.Next() % 30 + 10;

           var Left =  R.Next()%4 / 10.0;
           var Top =  Left +(R.Next() % 3 / 10.0);
           var Right = Top + (R.Next() % 3 / 10.0);

           SetGeometry(innerRadius, outerRadius, holeRadius, teeth, 0.01, Left, Top, Right);
        }

        public void RandomNormal()
        {
            var holeRadius = R.Next() % 20 + 10;

            // inner has to be less than outer which is less than hole
            var innerRadius =  R.Next() % 10 + 39;
            var outerRadius = 50;

            var teeth = R.Next() % 10 + 2;



            SetGeometry(innerRadius, outerRadius, holeRadius, teeth, 0.01, 0.1, 0.4, 0.5);
        }

        public void RandomSpike()
        {
            var holeRadius = R.Next() % 20 + 10;

            // inner has to be less than outer which is less than hole
            var innerRadius = R.Next() % 19 + 30;
            var outerRadius = 50;

            var teeth = R.Next() % 40 + 20;



            SetGeometry(innerRadius, outerRadius, holeRadius, teeth, 0.01, 0.25, 0.5, 0.5);
        }




        public void SetGeometry(double innerRadius, double outerRadius, double holeRadius, int teeth, double outlinePercent = 0, double gearLeftAngleStop = 0.1, double gearTopStop = 0.4, double gearRightAngleStop = 0.5)
        {
            if (innerRadius < 0 || innerRadius > 50)
            {
                throw new ArgumentOutOfRangeException("innerRadius", "innerRadius must be between 0.0 and 50.0");
            }

            if (outerRadius < 0 || outerRadius > 50)
            {
                throw new ArgumentOutOfRangeException("outerRadius", "outerRadius must be between 0.0 and 50.0");
            }

            if (holeRadius < 0 || holeRadius > 50)
            {
                throw new ArgumentOutOfRangeException("innerRadius", "innerRadius must be between 0.0 and 50.0");
            }

            if (teeth < 0 || teeth > 500)
            {
                throw new ArgumentOutOfRangeException("innerRadius", "innerRadius must be between 0.0 and 500.0");
            }


            _GearPaths[0] = MakeGear(innerRadius, outerRadius, holeRadius, teeth);

            // TODO: Add a full path figure here to create an outline

            for (int i  =0; i < 4; i++)
                _GearPaths[i] = MakeGear(innerRadius, outerRadius, holeRadius, teeth, gearLeftAngleStop, gearTopStop, gearRightAngleStop, i * (teeth >> 2), i < 3 ? (i+1) * (teeth >> 2) : teeth);


            if (outlinePercent > 0)
            {
                // Doesn't work if the hole is outside the outer radius curently (inverted gear)
                _GearPaths[4] = MakeGear(innerRadius * (1 + outlinePercent), outerRadius * (1 + outlinePercent), holeRadius * (1 - outlinePercent), teeth, gearLeftAngleStop, gearTopStop, gearRightAngleStop, 0, teeth);

                _GearPaths[5] = MakeGear(innerRadius * (1 + outlinePercent*2), outerRadius * (1 + outlinePercent*2), holeRadius * (1 - outlinePercent*2), teeth, gearLeftAngleStop, gearTopStop, gearRightAngleStop, 0, teeth);
            }
        }

        public int Width
        {
            get { return _Width; }
            set { _Width = value; }
        }

        public int Height
        {
            get { return _Height; }
            set { _Height = value; }
        }

        public string Name
        {
            get { return _Name; }
            set { _Name = value; }

        }


        public int Angle
        {
            get { return _Angle; }
            set { _Angle = value; }
        }

        public PathGeometry GearPaths
        {
            get { return _GearPaths[0]; }
            set { _GearPaths[0] = value; }
        }

        public PathGeometry GearPaths1
        {
            get { return _GearPaths[1]; }
            set { _GearPaths[1] = value; }
        }

        public PathGeometry GearPaths2
        {
            get { return _GearPaths[2]; }
            set { _GearPaths[2] = value; }
        }

        public PathGeometry GearPaths3
        {
            get { return _GearPaths[3]; }
            set { _GearPaths[3] = value; }
        }

        public PathGeometry GearPathsOutline
        {
            get { return _GearPaths[4]; }
            set { _GearPaths[4] = value; }
        }

        public PathGeometry GearPathsOutline2
        {
            get { return _GearPaths[5]; }
            set { _GearPaths[5] = value; }
        }

        public SolidColorBrush Fills
        {
            get { return _Fills[0]; }
            set { _Fills[0] = value; }
        }
        public SolidColorBrush Fills1
        {
            get { return _Fills[1]; }
            set { _Fills[1] = value; }
        }

        public SolidColorBrush Fills2
        {
            get { return _Fills[2]; }
            set { _Fills[2] = value; }
        }

        public SolidColorBrush Fills3
        {
            get { return _Fills[3]; }
            set { _Fills[3] = value; }
        }

        public SolidColorBrush FillsOutline
        {
            get { return _Fills[4]; }
            set { _Fills[4] = value; }
        }

        public SolidColorBrush FillsOutline2
        {
            get { return _Fills[5]; }
            set { _Fills[5] = value; }
        }


        public static PathGeometry MakeGear(double innerRadius, double outerRadius, double holeRadius, int teeth, double gearLeftAngleStop = 0.1, double gearTopStop = 0.4, double gearRightAngleStop = 0.5, int startTooth = 0, int endTooth = -1)
        {

            if (endTooth == -1)
                endTooth = teeth;

            var myPathGeo = new Windows.UI.Xaml.Media.PathGeometry();


            Func<double, double, Point> Translate = (x, y) =>
            {
                return new Point(x + 50, y + 50);
            };



            for (int i = startTooth; i < endTooth; i++)
            {
                var myPath = new PathFigure();


                var angle = (i * 360.0 / teeth) * Math.PI / 180.0; // starting angle
                var endAngle = ((i + 1) * 360.0 / teeth) * Math.PI / 180.0;
                var startPos = Translate(Math.Cos(angle) * innerRadius, Math.Sin(angle) * innerRadius);
                var endPos = Translate(Math.Cos(endAngle) * innerRadius, Math.Sin(endAngle) * innerRadius);


                myPath.StartPoint = Translate(Math.Cos(angle) * holeRadius, Math.Sin(angle) * holeRadius);

                var angleLengths = new { Q1 = (endAngle - angle) * gearLeftAngleStop, Q2 = (endAngle - angle) * gearTopStop, Q3 = (endAngle - angle) * gearRightAngleStop };


                var LineUp0 = new LineSegment();
                LineUp0.Point = Translate(Math.Cos(angle) * innerRadius, Math.Sin(angle) * innerRadius);
                myPath.Segments.Add(LineUp0);


                var LineUp = new LineSegment();
                LineUp.Point = Translate(Math.Cos(angle + angleLengths.Q1) * outerRadius, Math.Sin(angle + angleLengths.Q1) * outerRadius);
                myPath.Segments.Add(LineUp);

                var QuarterArc = new ArcSegment();
                QuarterArc.IsLargeArc = false;
                QuarterArc.Point = Translate(Math.Cos(angle + angleLengths.Q2) * outerRadius, Math.Sin(angle + angleLengths.Q2) * outerRadius); ;
                QuarterArc.SweepDirection = SweepDirection.Clockwise;
                QuarterArc.Size = new Size(outerRadius, outerRadius); // Circle arc segment
                myPath.Segments.Add(QuarterArc);

                var LineDown = new LineSegment();
                LineDown.Point = Translate(Math.Cos(angle + angleLengths.Q3) * innerRadius, Math.Sin(angle + angleLengths.Q3) * innerRadius);
                myPath.Segments.Add(LineDown);


                var HalfArc = new ArcSegment();
                HalfArc.IsLargeArc = false;
                HalfArc.Point = Translate(Math.Cos(endAngle) * innerRadius, Math.Sin(endAngle) * innerRadius); ;
                HalfArc.SweepDirection = SweepDirection.Clockwise;
                HalfArc.Size = new Size(innerRadius, innerRadius); // Circle arc segment
                myPath.Segments.Add(HalfArc);

                if (holeRadius > 0)
                {
                    var LineDown2 = new LineSegment();
                    LineDown2.Point = Translate(Math.Cos(endAngle) * holeRadius, Math.Sin(endAngle) * holeRadius);
                    myPath.Segments.Add(LineDown2);



                    var HalfArc2 = new ArcSegment();
                    HalfArc2.IsLargeArc = false;
                    HalfArc2.Point = Translate(Math.Cos(angle) * holeRadius, Math.Sin(angle) * holeRadius); ;
                    HalfArc2.SweepDirection = SweepDirection.Counterclockwise;

                    HalfArc2.Size = new Size(holeRadius, holeRadius); // Circle arc segment
                    myPath.Segments.Add(HalfArc2);
                }


                myPathGeo.Figures.Add(myPath);





            }
            return myPathGeo;
        }

    }
}
