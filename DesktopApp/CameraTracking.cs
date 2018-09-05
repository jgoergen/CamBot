using Emgu.CV;
using Emgu.CV.Cvb;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Diagnostics;
using System.Drawing;

namespace VideoSurveilance
{
    public class CameraTracking
    {
        public bool Ready = false;
        public int FrameWidth = 0;
        public int FrameHeight = 0;

        private static BackgroundSubtractorMOG2 _fgDetector;
        private static Emgu.CV.Cvb.CvBlobDetector _blobDetector;
        private static Emgu.CV.Cvb.CvTracks _tracker;
        private static VideoCapture _cameraCapture;

        private int frameBlurStrength = 0;
        private int largestDetectionHeightSizeDivisor = 0;
        private int largestDetectionWidthSizeDivisor = 0;
        private int smallestDetectionHeightSizeDivisor = 0;
        private int smallestDetectionWidthSizeDivisor = 0;
        private Emgu.CV.Mat lastFrame;
        private Emgu.CV.Mat lastMask;

        
        public CameraTracking(int subtractionHistory, int subtractionThreshold, int frameBlurStrength, int largestDetectionHeightSizeDivisor, int largestDetectionWidthSizeDivisor, int smallestDetectionHeightSizeDivisor, int smallestDetectionWidthSizeDivisor)
        {
            Debug.WriteLine("CameraTracking:: Initializing");

            if (largestDetectionHeightSizeDivisor > smallestDetectionHeightSizeDivisor ||
                largestDetectionWidthSizeDivisor > smallestDetectionWidthSizeDivisor)
                throw new Exception("The large detection divisors should be smaller then the smallest detection divisors!");

            this.frameBlurStrength = frameBlurStrength;
            this.largestDetectionHeightSizeDivisor = largestDetectionHeightSizeDivisor;
            this.largestDetectionWidthSizeDivisor = largestDetectionWidthSizeDivisor;
            this.smallestDetectionHeightSizeDivisor = smallestDetectionHeightSizeDivisor;
            this.smallestDetectionWidthSizeDivisor = smallestDetectionWidthSizeDivisor;

            try
            {
                CameraTracking._cameraCapture = new VideoCapture();

                // I had to set this by hand to match our camera as opencv doesn't always pull these properties correctly 
                // and sometimes shows funky frames or nothing at all
                // CameraTracking._cameraCapture.SetCaptureProperty(CapProp.FrameWidth, 1600);
                // CameraTracking._cameraCapture.SetCaptureProperty(CapProp.FrameHeight, 1200);
                // CameraTracking._cameraCapture.SetCaptureProperty(CapProp.FourCC, Emgu.CV.VideoWriter.Fourcc('Y', 'U', 'Y', '2'));

                CameraTracking._fgDetector = new Emgu.CV.BackgroundSubtractorMOG2(subtractionHistory, subtractionThreshold);
                CameraTracking._blobDetector = new CvBlobDetector();
                CameraTracking._tracker = new CvTracks();
                this.Ready = true;
                Debug.WriteLine("CameraTracking:: Camera Initialized");
            }
            catch (Exception e)
            {
                throw new Exception("Unable to initialize the webcam!");
            }
        }
        
        public CameraTrackingUpdateReturnModel Update()
        {
            // capture frame

            Mat frame = _cameraCapture.QueryFrame();

            //filter out noises

            Mat smoothedFrame = new Mat();

            CvInvoke.GaussianBlur(
                frame, 
                smoothedFrame, 
                new Size(this.frameBlurStrength, this.frameBlurStrength), 
                1); 

            // get mask for preview

            Mat forgroundMask = new Mat();
            _fgDetector.Apply(smoothedFrame, forgroundMask);

            this.lastFrame = frame;
            this.lastMask = forgroundMask;

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
            _blobDetector.Detect(this.lastMask.ToImage<Gray, byte>(), blobs);
            blobs.FilterByArea(100, int.MaxValue);

            float scale = (this.lastFrame.Width + this.lastFrame.Width) / 2.0f;
            _tracker.Update(blobs, 0.01 * scale, 5, 5);

            FrameWidth = this.lastFrame.Width;
            FrameHeight = this.lastFrame.Height;

            foreach (var pair in _tracker)
            {
                CvTrack b = pair.Value;

                // limit the largest and smallest size boxes we care about.

                if (b.BoundingBox.Width < (this.lastFrame.Width / this.smallestDetectionWidthSizeDivisor) ||
                    b.BoundingBox.Height < (this.lastFrame.Height / this.smallestDetectionHeightSizeDivisor) ||
                    (b.BoundingBox.Width > (this.lastFrame.Width / this.largestDetectionWidthSizeDivisor) &&
                    b.BoundingBox.Height > (this.lastFrame.Height / this.largestDetectionHeightSizeDivisor)))
                    continue;

                // keep track of the largest regions as we only care to track the largest

                if (b.BoundingBox.Width > largestW)
                {
                    subject = b.BoundingBox;
                    largestW = b.BoundingBox.Width;
                    largestH = b.BoundingBox.Height;
                    centerX = b.Centroid.X;
                    centerY = b.Centroid.Y;

                    CvInvoke.Rectangle(
                        this.lastFrame, 
                        b.BoundingBox, 
                        new MCvScalar(
                            255.0, 
                            255.0, 
                            255.0), 
                        20);

                    CvInvoke.PutText(
                        this.lastFrame, 
                        b.Id.ToString(), 
                        new Point(
                            (int)Math.Round(b.Centroid.X), 
                            (int)Math.Round(b.Centroid.Y)), 
                        FontFace.HersheyPlain, 
                        1.0, 
                        new MCvScalar(255.0, 255.0, 255.0));

                    foundSubject = true;
                }
                else
                {
                    CvInvoke.Rectangle(
                        this.lastFrame, 
                        b.BoundingBox, 
                        new MCvScalar(
                            255.0, 
                            255.0, 
                            255.0), 
                        1);

                    CvInvoke.PutText(
                        this.lastFrame, 
                        b.Id.ToString(), 
                        new Point(
                            (int)Math.Round(b.Centroid.X), 
                            (int)Math.Round(b.Centroid.Y)), 
                        FontFace.HersheyPlain, 
                        1.0, 
                        new MCvScalar(
                            255.0, 
                            255.0, 
                            255.0));
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
