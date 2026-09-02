using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;

namespace GearMaker
{

    class RotateTuple
    {
        public double FinalValue;
        public RotateTransform Transform;

        public RotateTuple(double FinalVal, RotateTransform TransformRef)
        {
            FinalValue = FinalVal;
            Transform = TransformRef;
        }
    }
}
