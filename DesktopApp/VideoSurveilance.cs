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

        private static VideoCapture _cameraCapture;
        private Stopwatch sw = Stopwatch.StartNew();
        long millisecondsWaitBeforeMove = 200;
        long lastTimeMoved = 0;
        private static BackgroundSubtractor _fgDetector;
        private static Emgu.CV.Cvb.CvBlobDetector _blobDetector;
        private static Emgu.CV.Cvb.CvTracks _tracker;
        private SerialPort _serialPort = new SerialPort();
        private double centerX = 0;
        private double centerY = 0;
        private bool moving = false;

        /// <summary>
        /// Holds data received until we get a terminator.
        /// </summary>
        private string tString = string.Empty;
        /// <summary>
        /// End of transmition byte in this case EOT (ASCII 4).
        /// </summary>
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
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return;
            }

            _fgDetector = new Emgu.CV.VideoSurveillance.BackgroundSubtractorMOG2();
            _blobDetector = new CvBlobDetector();
            _tracker = new CvTracks();

            Application.Idle += ProcessFrame;
        }

        void ProcessFrame(object sender, EventArgs e)
        {
            Mat frame = _cameraCapture.QueryFrame();
            Mat smoothedFrame = new Mat();
            CvInvoke.GaussianBlur(frame, smoothedFrame, new Size(31, 31), 1); //filter out noises
                                                                              //frame._SmoothGaussian(3);

            #region use the BG/FG detector to find the forground mask
            Mat forgroundMask = new Mat();
            _fgDetector.Apply(smoothedFrame, forgroundMask);
            #endregion

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

                if (b.BoundingBox.Width < (frame.Width / 10) || b.BoundingBox.Height < (frame.Height / 10) || (b.BoundingBox.Width > (frame.Width / 2) && b.BoundingBox.Height > (frame.Height / 2)))
                    continue;

                if (b.BoundingBox.Width > largestW)
                {

                    largestW = b.BoundingBox.Width;
                    largestH = b.BoundingBox.Height;

                    centerX = b.Centroid.X;
                    centerY = b.Centroid.Y;
                    CvInvoke.Rectangle(frame, b.BoundingBox, new MCvScalar(255.0, 255.0, 255.0), 10);
                    CvInvoke.PutText(frame, b.Id.ToString(), new Point((int)Math.Round(b.Centroid.X), (int)Math.Round(b.Centroid.Y)), FontFace.HersheyPlain, 1.0, new MCvScalar(255.0, 255.0, 255.0));

                }
                else
                {
                    //CvInvoke.Rectangle(frame, b.BoundingBox, new MCvScalar(255.0, 255.0, 255.0), 2);
                    //CvInvoke.PutText(frame, b.Id.ToString(), new Point((int)Math.Round(b.Centroid.X), (int)Math.Round(b.Centroid.Y)), FontFace.HersheyPlain, 1.0, new MCvScalar(255.0, 255.0, 255.0));

                }
            }
            imageBox1.Image = frame;
            imageBox2.Image = forgroundMask;

            if (sw.ElapsedMilliseconds > lastTimeMoved + millisecondsWaitBeforeMove)
            {
                Debug.WriteLine(frame.Width + " " + frame.Height + " " + centerX + " " + centerY);
                lastTimeMoved = sw.ElapsedMilliseconds;

                if (moving)
                {
                    moving = false;
                    centerX = 0;
                    centerY = 0;
                }

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

                if ((frame.Width / 2) < centerX)
                {
                    Debug.WriteLine("Move left 2");
                    _serialPort.WriteLine("<panleft:4>");
                    _serialPort.WriteLine("");
                    moving = true;

                }
                else if ((frame.Width / 2) > centerX)
                {
                    Debug.WriteLine("Move right 2");
                    _serialPort.WriteLine("<panright:4>");
                    _serialPort.WriteLine("");
                    moving = true;
                }

                if ((frame.Height / 2) > centerY)
                {
                    Debug.WriteLine("Move up 2");
                    _serialPort.WriteLine("<tiltup:4>");
                    _serialPort.WriteLine("");
                    moving = true;

                }
                else if ((frame.Height / 2) < centerY)
                {
                    Debug.WriteLine("Move down 2");
                    _serialPort.WriteLine("<tiltdown:4>");
                    _serialPort.WriteLine("");
                    moving = true;
                }

                centerX = 0;
                centerY = 0;
            }
        }
    }
}