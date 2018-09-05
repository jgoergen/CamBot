//----------------------------------------------------------------------------
//  Copyright (C) 2004-2017 by EMGU Corporation. All rights reserved.
//----------------------------------------------------------------------------

using System;
using System.Windows.Forms;
using System.Diagnostics;

namespace VideoSurveilance
{
    public partial class VideoSurveilance : Form
    {
        // SETTINGS ////////////////////////////////////////////////
        
        // the number of frames to use when normalizing out 'noise'. the lower this is, the noisier your images will be
        // which will reduce the quality of fine grain tracking, but if you make it too big it slows down your responsiveness

        private const int SUBTRACTION_HISTORY = 20; 

        // the amount of pixel difference to ignore when comparing sets of frames

        private const int SUBTRACTION_THRESHOLD = 20;

        // lowering this will speed things up alittle and improve very fine grain tracking, but if your feed is noisy you will get 
        // false positives from all the noise.
        // NOTE: this MUST be an odd number!!!

        private const int FRAME_BLUR_STRENGTH = 101;

        // how long to wait before moving towards movement again
        // reducing this will speed up tracking and response, but if you don't wait long enough the previous camera
        // movement will be counted when looking for movement ( which will throw everything off. )

        private const long DEFAULT_MOVE_WAIT = 200;

        // this 'dampens' the response to movement. this will have to be adjusted to taste and differe between servos
        // the lower this number the further the servos move towards percieved movement, too low ( moving too fast ) will overshoot targets

        private const int MOVE_ADJUST = 30;

        // the amount of time the camera waits before returning to home postiion. movement will reset this wait time.

        private const long MS_WAIT_BEFORE_HOMING = 10 * 1000;

        // this is multiplied by the largest move distance ( to compensate for longer moves ) and added to the wait time after a move

        private const int MOVE_DIST_WAIT_MODIFIER = 40;

        // the default horizontal servo position when 'homed'

        private const int HOME_PAN = 40;

        // the default vertical servo position when 'homed'

        private const int HOME_TILT = 40;

        // the speed at which the servos move to new locations
        // raising this too high will result in jerky movements which may interfere with camera tracking if the chassis is wobbly
        // lowering too much will require raising the MOVE_DIST_WAIT_MODIFIER above

        private const int SERVO_MOVE_SPEED = 20;

        // this is the speed that the 'neopixel' fades from previous color to newly assigned color

        private const int LED_CHANGE_SPEED = 2;

        // the amount of time to wait after homing, before moving towards movement again

        private const int PAUSE_AFTER_HOMING = 400;

        // this is a terribly named variable. it defines the largest object we care to track relative to the entire video height.
        // so a value of 2 means that the largest object we care to track is VIDEO_HEIGHT / 2 or one half the total height
        // making this value too low ( 1 being the lowest value ) will result in potentially tracking camera shakes & jerks
        // making this value too high will result in the tracking ignoring most or all objects

        private const int LARGEST_DETECTION_HEIGHT_SIZE_DIVISOR = 2;

        // this is a terribly named variable. it defines the largest object we care to track relative to the entire video width.
        // so a value of 2 means that the largest object we care to track is VIDEO_WIDTH / 2 or one half the total width
        // making this value too low ( 1 being the lowest value ) will result in potentially tracking camera shakes & jerks
        // making this value too high will result in the tracking ignoring most or all objects

        private const int LARGEST_DETECTION_WIDTH_SIZE_DIVISOR = 2;

        // this is a terribly named variable. it defines the smallest object we care to track relative to the entire video height.
        // so a value of 2 means that the largest object we care to track is VIDEO_HEIGHT / 6 or one sixth the total height
        // making this value too high will result in potentially tracking camera shakes & jerks
        // making this value too low ( 1 being the lowest value ) will result in the tracking ignoring most or all objects

        private const int SMALLEST_DETECTION_HEIGHT_SIZE_DIVISOR = 6;

        // this is a terribly named variable. it defines the smallest object we care to track relative to the entire video width.
        // so a value of 2 means that the largest object we care to track is VIDEO_WIDTH / 10 or one tenth the total width
        // making this value too high will result in potentially tracking camera shakes & jerks
        // making this value too low ( 1 being the lowest value ) will result in the tracking ignoring most or all objects

        private const int SMALLEST_DETECTION_WIDTH_SIZE_DIVISOR = 10;
        // END SETTINGS ////////////////////////////////////////////

        private Robot robot;
        private CameraTracking camera;
        private Stopwatch sw = Stopwatch.StartNew();
        private long lastTimeMoved = 0;
        private long nextHomeTime = 0;
        private long moveWait = 0;

        public VideoSurveilance()
        {
            Debug.WriteLine("Initializing");

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
                    
                    if (sw.ElapsedMilliseconds > nextHomeTime)
                    {
                        robot.Home();
                        moveWait += PAUSE_AFTER_HOMING;
                        nextHomeTime = (lastTimeMoved + MS_WAIT_BEFORE_HOMING) * 10 + moveWait;                        
                    }
                    else
                    {
                        robot.SetLED(0, 0, 0);
                    }
                    
                    return;
                }

                robot.SetLED(0, 255, 0);
                nextHomeTime = lastTimeMoved + MS_WAIT_BEFORE_HOMING;

                int xDist = (int)Math.Floor(((camera.FrameWidth / 2) - subjectData.CenterX) / MOVE_ADJUST);
                int yDist = (int)Math.Floor(((camera.FrameHeight / 2) - subjectData.CenterY) / MOVE_ADJUST) * -1;
                robot.Move(xDist, yDist);

                // if we did actually move add alittle extra time onto the wait before movin again

                moveWait +=
                    (long)(Math.Abs(Math.Max(xDist, yDist) * MOVE_DIST_WAIT_MODIFIER));

                Debug.WriteLine("Waiting " + moveWait);
                Debug.WriteLine("");
            }
            else
            {
                Debug.Write(".");
            }
        }
    }
}