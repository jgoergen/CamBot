using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace VideoSurveilance
{
    public class CameraTrackingFindSubjectsReturnModel
    {
        public double CenterX = 0;
        public double CenterY = 0;
        public Rectangle BoundingBox;
        public bool FoundSubject;
    }
}
