//----------------------------------------------------------------------------
//  Copyright (C) 2004-2017 by EMGU Corporation. All rights reserved.
//----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Emgu.CV;
using Emgu.CV.Cvb;
using Emgu.CV.UI;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.VideoSurveillance;
using System.IO.Ports;
using System.Diagnostics;

namespace VideoSurveilance
{
    public partial class VideoSurveilance : Form
    {
        // SETTINGS ////////////////////////////////////////////////
        private int SUBSTRACTION_HISTORY = 20; // the number of frames to use when normalizing out 'noise'
        private int SUBTRACTION_THRESHOLD = 30; // the amount of pixel difference to ignore when comparing sets of frames
        private int FRAME_BLUR_STRENGTH = 101; // this MUST be an odd number
        private int LARGEST_DETECTION_HEIGHT_SIZE_DIVISOR = 2;
        private int LARGEST_DETECTION_WIDTH_SIZE_DIVISOR = 2;
        private int SMALLEST_DETECTION_HEIGHT_SIZE_DIVISOR = 4;
        private int SMALLEST_DETECTION_WIDTH_SIZE_DIVISOR = 5;
        private long MS_MOVE_WAIT = 500; // how long to wait before looking moving
        private long MS_PAUSE_DETECT_AFTER_MOVE = 500; // how long to wait after a move for the detection to 'settle'
        private int MOVE_ADJUST = 10; // the lower this number the further the servos move towards percieved movement
        // END SETTINGS ////////////////////////////////////////////

        private static VideoCapture _cameraCapture;
        private Stopwatch sw = Stopwatch.StartNew();
        private long lastTimeMoved = 0;
        private static BackgroundSubtractor _fgDetector;
        private static Emgu.CV.Cvb.CvBlobDetector _blobDetector;
        private static Emgu.CV.Cvb.CvTracks _tracker;
        private SerialPort _serialPort = new SerialPort();
        private double centerX = 0;
        private double centerY = 0;
        private bool moving = false;
        private string tString = string.Empty;
        private byte _terminator = 0x4;

        public VideoSurveilance()
        {
            InitializeComponent();
            OpenSerialPort();
            Run();
        }

        public void OpenSerialPort()
        {
            string portNum = "";
            _serialPort.Close();

            foreach (string ports in SerialPort.GetPortNames())
            {
                portNum = ports.ToString();
                Debug.WriteLine("Trying port: " + portNum);

                try
                {
                    _serialPort.PortName = (portNum);
                    _serialPort.BaudRate = 9600;
                    _serialPort.DataBits = 8;
                    _serialPort.Parity = Parity.None;
                    _serialPort.StopBits = StopBits.One;
                    _serialPort.Handshake = Handshake.None;
                    _serialPort.Encoding = System.Text.Encoding.Default;
                    _serialPort.Open();
                    _serialPort.DataReceived += new SerialDataReceivedEventHandler(_serialPort_DataReceived);
                    _serialPort.WriteLine("<setledspeed:2>");
                    _serialPort.WriteLine("<setr:0>");
                    _serialPort.WriteLine("<setg:0>");
                    _serialPort.WriteLine("<setb:0>");
                    _serialPort.WriteLine("<panset:50>");
                    _serialPort.WriteLine("<tiltset:30>");
                    _serialPort.WriteLine("");
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Unable to use port.");
                    Debug.WriteLine(e.Message);
                }
            }

        }

        void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] buffer = new byte[_serialPort.ReadBufferSize];
            int bytesRead = _serialPort.Read(buffer, 0, buffer.Length);
            tString += Encoding.ASCII.GetString(buffer, 0, bytesRead);

            if (tString.IndexOf((char)_terminator) > -1)
            {
                string workingString = tString.Substring(0, tString.IndexOf((char)_terminator));
                tString = tString.Substring(tString.IndexOf((char)_terminator));
                Debug.WriteLine("Serial Data");
                Debug.WriteLine(workingString);
            }
        }

        void Run()
        {
            try
            {
                _cameraCapture = new VideoCapture();

                // I had to set this by hand to match our camera as opencv doesn't always pull these properties correctly and sometimes shows funky frames or nothing at all
                _cameraCapture.SetCaptureProperty(CapProp.FrameWidth, 1600);
                _cameraCapture.SetCaptureProperty(CapProp.FrameHeight, 1200);
                _cameraCapture.SetCaptureProperty(CapProp.FourCC, Emgu.CV.VideoWriter.Fourcc('Y', 'U', 'Y', '2'));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return;
            }

            _fgDetector = new Emgu.CV.VideoSurveillance.BackgroundSubtractorMOG2(SUBSTRACTION_HISTORY, SUBTRACTION_THRESHOLD);
            _blobDetector = new CvBlobDetector();
            _tracker = new CvTracks();
            Application.Idle += ProcessFrame;
        }

        void ProcessFrame(object sender, EventArgs e)
        {
            // capture frame
            Mat frame = _cameraCapture.QuerySmallFrame();
            Mat smoothedFrame = new Mat();
            CvInvoke.GaussianBlur(frame, smoothedFrame, new Size(FRAME_BLUR_STRENGTH, FRAME_BLUR_STRENGTH), 1); //filter out noises

            // get mask for preview
            Mat forgroundMask = new Mat();
            _fgDetector.Apply(smoothedFrame, forgroundMask);

            // get detection 'blobs' or regions
            CvBlobs blobs = new CvBlobs();
            _blobDetector.Detect(forgroundMask.ToImage<Gray, byte>(), blobs);
            blobs.FilterByArea(100, int.MaxValue);

            float scale = (frame.Width + frame.Width) / 2.0f;
            _tracker.Update(blobs, 0.01 * scale, 5, 5);

            double largestW = 0;
            double largestH = 0;

            foreach (var pair in _tracker)
            {
                CvTrack b = pair.Value;

                // limit the largest and smallest size boxes we care about.
                if (b.BoundingBox.Width < (frame.Width / SMALLEST_DETECTION_WIDTH_SIZE_DIVISOR) ||
                    b.BoundingBox.Height < (frame.Height / SMALLEST_DETECTION_HEIGHT_SIZE_DIVISOR) ||
                    (b.BoundingBox.Width > (frame.Width / LARGEST_DETECTION_WIDTH_SIZE_DIVISOR) &&
                    b.BoundingBox.Height > (frame.Height / LARGEST_DETECTION_HEIGHT_SIZE_DIVISOR)))
                    continue;

                // keep track of the largest regions as we only care to track the largest
                if (b.BoundingBox.Width > largestW)
                {
                    largestW = b.BoundingBox.Width;
                    largestH = b.BoundingBox.Height;
                    centerX = b.Centroid.X;
                    centerY = b.Centroid.Y;
                    CvInvoke.Rectangle(frame, b.BoundingBox, new MCvScalar(255.0, 255.0, 255.0), 20);
                    CvInvoke.PutText(frame, b.Id.ToString(), new Point((int)Math.Round(b.Centroid.X), (int)Math.Round(b.Centroid.Y)), FontFace.HersheyPlain, 1.0, new MCvScalar(255.0, 255.0, 255.0));
                }
                else
                {
                    CvInvoke.Rectangle(frame, b.BoundingBox, new MCvScalar(255.0, 255.0, 255.0), 1);
                    CvInvoke.PutText(frame, b.Id.ToString(), new Point((int)Math.Round(b.Centroid.X), (int)Math.Round(b.Centroid.Y)), FontFace.HersheyPlain, 1.0, new MCvScalar(255.0, 255.0, 255.0));
                }
            }

            // display frames onscreen
            imageBox1.Image = frame;
            imageBox2.Image = forgroundMask;

            // if enough time has elapsed, deal with potential movement
            if (sw.ElapsedMilliseconds > lastTimeMoved + MS_MOVE_WAIT)
            {
                Debug.WriteLine(frame.Width + " " + frame.Height + " " + centerX + " " + centerY);
                lastTimeMoved = sw.ElapsedMilliseconds;

                // did we just move? if so, skip movement this time around.
                if (moving)
                {
                    moving = false;
                    // reset wait time before next move
                    MS_MOVE_WAIT -= MS_PAUSE_DETECT_AFTER_MOVE;
                    centerX = 0;
                    centerY = 0;
                }

                // if there is no movement, fade back to 0,0,0
                if (centerX == 0 && centerY == 0)
                {
                    _serialPort.WriteLine("<setr:0>");
                    _serialPort.WriteLine("<setg:0>");
                    _serialPort.WriteLine("<setb:0>");
                    return;
                }
                else
                {
                    _serialPort.WriteLine("<setg:255>");
                }

                var xDist = (((frame.Width / 2) - centerX) / MOVE_ADJUST);
                var yDist = (((frame.Height / 2) - centerY) / MOVE_ADJUST) * -1;

                if (xDist < 0)
                {
                    Debug.WriteLine("Move left " + Math.Abs(xDist));
                    _serialPort.WriteLine("<panleft:" + Math.Abs(xDist) + ">");
                    _serialPort.WriteLine("");
                    moving = true;
                }
                else if (xDist > 0)
                {
                    Debug.WriteLine("Move right " + xDist);
                    _serialPort.WriteLine("<panright:" + xDist + ">");
                    _serialPort.WriteLine("");
                    moving = true;
                }

                if (yDist < 0)
                {
                    Debug.WriteLine("Move up " + Math.Abs(yDist));
                    _serialPort.WriteLine("<tiltup:" + Math.Abs(yDist)  + ">");
                    _serialPort.WriteLine("");
                    moving = true;
                }
                else if (yDist > 0)
                {
                    Debug.WriteLine("Move down " + yDist);
                    _serialPort.WriteLine("<tiltdown:" + yDist  + ">");
                    _serialPort.WriteLine("");
                    moving = true;
                }

                // clear out values now that we've moved.
                centerX = 0;
                centerY = 0;

                // if we did actually move add alittle extra time onto the wait before movin again
                if (moving)
                    MS_MOVE_WAIT += MS_PAUSE_DETECT_AFTER_MOVE;
            }
        }
    }
}