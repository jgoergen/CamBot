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
        private int SUBTRACTION_HISTORY = 20; // the number of frames to use when normalizing out 'noise'
        private int SUBTRACTION_THRESHOLD = 20; // the amount of pixel difference to ignore when comparing sets of frames
        private int FRAME_BLUR_STRENGTH = 101; // this MUST be an odd number
        private int LARGEST_DETECTION_HEIGHT_SIZE_DIVISOR = 2;
        private int LARGEST_DETECTION_WIDTH_SIZE_DIVISOR = 2;
        private int SMALLEST_DETECTION_HEIGHT_SIZE_DIVISOR = 6;
        private int SMALLEST_DETECTION_WIDTH_SIZE_DIVISOR = 10;
        private long DEFAULT_MOVE_WAIT = 200; // how long to wait before looking moving
        private int MOVE_ADJUST = 30; // the lower this number the further the servos move towards percieved movement
        private long MS_WAIT_BEFORE_HOMING = 10 * 1000; // the amount of time the camera waits before returning to home postiion. movement resets wait.
        private int HOME_PAN = 40;
        private int HOME_TILT = 40;
        private int SERVO_MOVE_SPEED = 20;
        private int LED_CHANGE_SPEED = 2;
        private int MOVE_DIST_WAIT_MODIFIER = 40; // this is multiplied by the largest move distance and added to the natural wait time after a move.
        private int PAUSE_AFTER_HOMING = 400;
        // END SETTINGS ////////////////////////////////////////////

        private Robot robot;
        private CameraTracking camera;

        private Stopwatch sw = Stopwatch.StartNew();
        private long lastTimeMoved = 0;
        private long next_home_time = 0;
        private long moveWait = 0;

        public VideoSurveilance()
        {
            InitializeComponent();

            robot = 
                new Robot(
                    SERVO_MOVE_SPEED, 
                    LED_CHANGE_SPEED, 
                    HOME_PAN, 
                    HOME_TILT);

            camera =
                new CameraTracking(
                    SUBTRACTION_HISTORY,
                    SUBTRACTION_THRESHOLD,
                    FRAME_BLUR_STRENGTH,
                    LARGEST_DETECTION_HEIGHT_SIZE_DIVISOR,
                    LARGEST_DETECTION_WIDTH_SIZE_DIVISOR,
                    SMALLEST_DETECTION_HEIGHT_SIZE_DIVISOR,
                    SMALLEST_DETECTION_WIDTH_SIZE_DIVISOR);

            if (!camera.Ready)
            {
                Debug.WriteLine("Could not find Camera.");
                return;
            }

            if (!camera.Ready)
            {
                Debug.WriteLine("Could not find Robot.");
                return;
            }

            Application.Idle += ProcessFrame;
        }
        
        void ProcessFrame(object sender, EventArgs e)
        {
            var frameData = camera.Update();

            // display frames onscreen
            imageBox1.Image = frameData.Frame;
            imageBox2.Image = frameData.Mask;

            // if enough time has elapsed, deal with potential movement
            if (sw.ElapsedMilliseconds > lastTimeMoved + moveWait)
            {
                lastTimeMoved = sw.ElapsedMilliseconds;
                moveWait = DEFAULT_MOVE_WAIT;

                var subjectData =
                    camera.FindSubjects();

                if (!subjectData.FoundSubject)
                {
                    
                    if (sw.ElapsedMilliseconds > next_home_time)
                    {
                        robot.Home();
                        moveWait += PAUSE_AFTER_HOMING;
                        next_home_time = (lastTimeMoved + MS_WAIT_BEFORE_HOMING) * 10 + moveWait;                        
                    }
                    else
                    {
                        robot.SetLED(0, 0, 0);
                    }
                    
                    return;
                }

                robot.SetLED(0, 255, 0);
                next_home_time = lastTimeMoved + MS_WAIT_BEFORE_HOMING;

                int xDist = (int)Math.Floor(((camera.FrameWidth / 2) - subjectData.CenterX) / MOVE_ADJUST);
                int yDist = (int)Math.Floor(((camera.FrameHeight / 2) - subjectData.CenterY) / MOVE_ADJUST) * -1;
                robot.Move(xDist, yDist);

                // if we did actually move add alittle extra time onto the wait before movin again
                moveWait +=
                    (long)(Math.Abs(Math.Max(xDist, yDist) * MOVE_DIST_WAIT_MODIFIER));

                Debug.WriteLine("Waiting " + moveWait);
                Debug.WriteLine("");
            } else
            {
                Debug.Write(".");
            }
        }
    }
}