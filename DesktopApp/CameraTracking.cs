using Emgu.CV;
using Emgu.CV.Cvb;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.VideoSurveillance;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace VideoSurveilance
{
    public class CameraTracking
    {
        public bool Ready = false;
        public int FrameBlurStrength = 0;
        public int FrameWidth = 0;
        public int FrameHeight = 0;
        public int LargestDetectionHeightSizeDivisor = 0;
        public int LargestDetectionWidthSizeDivisor = 0;
        public int SmallestDetectionHeightSizeDivisor = 0;
        public int SmallestDetectionWidthSizeDivisor = 0;
        public Emgu.CV.Mat LastFrame;
        public Emgu.CV.Mat LastMask;

        private static BackgroundSubtractor _fgDetector;
        private static Emgu.CV.Cvb.CvBlobDetector _blobDetector;
        private static Emgu.CV.Cvb.CvTracks _tracker;
        private static VideoCapture _cameraCapture;

        public CameraTracking(int subtractionHistory, int subtractionThreshold, int frameBlurStrength, int largestDetectionHeightSizeDivisor, int largestDetectionWidthSizeDivisor, int smallestDetectionHeightSizeDivisor, int smallestDetectionWidthSizeDivisor)
        {
            FrameBlurStrength = frameBlurStrength;
            LargestDetectionHeightSizeDivisor = largestDetectionHeightSizeDivisor;
            LargestDetectionWidthSizeDivisor = largestDetectionWidthSizeDivisor;
            SmallestDetectionHeightSizeDivisor = smallestDetectionHeightSizeDivisor;
            SmallestDetectionWidthSizeDivisor = smallestDetectionWidthSizeDivisor;

            try
            {
                _cameraCapture = new VideoCapture();

                // I had to set this by hand to match our camera as opencv doesn't always pull these properties correctly and sometimes shows funky frames or nothing at all
                // _cameraCapture.SetCaptureProperty(CapProp.FrameWidth, 1600);
                // _cameraCapture.SetCaptureProperty(CapProp.FrameHeight, 1200);
                // _cameraCapture.SetCaptureProperty(CapProp.FourCC, Emgu.CV.VideoWriter.Fourcc('Y', 'U', 'Y', '2'));

                _fgDetector = new Emgu.CV.VideoSurveillance.BackgroundSubtractorMOG2(subtractionHistory, subtractionThreshold);
                _blobDetector = new CvBlobDetector();
                _tracker = new CvTracks();
                Ready = true;
            }
            catch (Exception e)
            {
                Ready = false;
            }
        }
        
        public CameraTrackingUpdateReturnModel Update()
        {
            // capture frame
            Mat frame = _cameraCapture.QueryFrame();
            Mat smoothedFrame = new Mat();
            CvInvoke.GaussianBlur(frame, smoothedFrame, new Size(FrameBlurStrength, FrameBlurStrength), 1); //filter out noises

            // get mask for preview
            Mat forgroundMask = new Mat();
            _fgDetector.Apply(smoothedFrame, forgroundMask);
            
            LastFrame = frame;
            LastMask = forgroundMask;

            return new CameraTrackingUpdateReturnModel()
            {
                Frame = frame,
                Mask = forgroundMask
            };
        }

        public CameraTrackingFindSubjectsReturnModel FindSubjects()
        {
            double largestW = 0;
            double largestH = 0;
            double centerX = 0;
            double centerY = 0;
            bool foundSubject = false;
            Rectangle subject = new Rectangle();

            // get detection 'blobs' or regions
            CvBlobs blobs = new CvBlobs();
            _blobDetector.Detect(LastMask.ToImage<Gray, byte>(), blobs);
            blobs.FilterByArea(100, int.MaxValue);

            float scale = (LastFrame.Width + LastFrame.Width) / 2.0f;
            _tracker.Update(blobs, 0.01 * scale, 5, 5);

            FrameWidth = LastFrame.Width;
            FrameHeight = LastFrame.Height;

            foreach (var pair in _tracker)
            {
                CvTrack b = pair.Value;

                // limit the largest and smallest size boxes we care about.
                if (b.BoundingBox.Width < (LastFrame.Width / SmallestDetectionWidthSizeDivisor) ||
                    b.BoundingBox.Height < (LastFrame.Height / SmallestDetectionHeightSizeDivisor) ||
                    (b.BoundingBox.Width > (LastFrame.Width / LargestDetectionWidthSizeDivisor) &&
                    b.BoundingBox.Height > (LastFrame.Height / LargestDetectionHeightSizeDivisor)))
                    continue;

                // keep track of the largest regions as we only care to track the largest
                if (b.BoundingBox.Width > largestW)
                {
                    subject = b.BoundingBox;
                    largestW = b.BoundingBox.Width;
                    largestH = b.BoundingBox.Height;
                    centerX = b.Centroid.X;
                    centerY = b.Centroid.Y;
                    CvInvoke.Rectangle(LastFrame, b.BoundingBox, new MCvScalar(255.0, 255.0, 255.0), 20);
                    CvInvoke.PutText(LastFrame, b.Id.ToString(), new Point((int)Math.Round(b.Centroid.X), (int)Math.Round(b.Centroid.Y)), FontFace.HersheyPlain, 1.0, new MCvScalar(255.0, 255.0, 255.0));
                    foundSubject = true;
                }
                else
                {
                    CvInvoke.Rectangle(LastFrame, b.BoundingBox, new MCvScalar(255.0, 255.0, 255.0), 1);
                    CvInvoke.PutText(LastFrame, b.Id.ToString(), new Point((int)Math.Round(b.Centroid.X), (int)Math.Round(b.Centroid.Y)), FontFace.HersheyPlain, 1.0, new MCvScalar(255.0, 255.0, 255.0));
                }
            }

            return new CameraTrackingFindSubjectsReturnModel()
            {
                CenterX = centerX,
                CenterY = centerY,
                BoundingBox = subject,
                FoundSubject = foundSubject
            };
        }
    }
}
