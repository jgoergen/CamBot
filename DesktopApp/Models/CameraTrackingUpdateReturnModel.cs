using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace VideoSurveilance
{
    public class CameraTrackingUpdateReturnModel
    {
        public Emgu.CV.Mat Frame;
        public Emgu.CV.Mat Mask;
    }
}
