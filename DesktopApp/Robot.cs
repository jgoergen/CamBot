using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;

namespace VideoSurveilance
{
    public class Robot
    {
        public bool Ready = false;

        private int r = 0;
        private int g = 0;
        private int b = 0;
        private int pan = 0;
        private int tilt = 0;
        private int ledChangeSpeed = 0;
        private int servoMoveSpeed = 0;
        private int homePanValue = 0;
        private int homeTiltValue = 0;
        private string tString = string.Empty;
        private byte serialStringTerminator = 0x4;
        private SerialPort serialPort = new SerialPort();

        // set this to true to disable serial communications for testing locally

        private bool disable = false;

        public Robot(int servoMoveSpeed, int ledChangeSpeed, int homePanValue, int homeTiltValue)
        {
            Debug.WriteLine("Robot:: Initializing");

            this.ledChangeSpeed = ledChangeSpeed;
            this.servoMoveSpeed = servoMoveSpeed;
            this.homePanValue = homePanValue;
            this.homeTiltValue = homeTiltValue;

            OpenSerialPort();
            if (this.Ready)
                Home();
            else
                throw new Exception("Unable to connect to the robot via Serial!");
        }

        public void Home()
        {
            SetLED(0, 0, 255);
            Set(this.homePanValue, this.homeTiltValue);
            Debug.WriteLine("Robot:: Homing");
        }

        public void Move(int? xDist, int? yDist)
        {
            if (this.disable)
                return;

            if (xDist == null)
            {
                xDist = 0;
            }
            else
            {
                this.pan += xDist.Value;
            }

            if (yDist == null)
            {
                yDist = 0;
            }
            else
            {
                this.tilt += yDist.Value;
            }

            if (xDist < 0)
            {
                Debug.WriteLine("Robot:: Move left " + Math.Abs(xDist.Value));
                serialPort.WriteLine("<panleft:" + Math.Abs(xDist.Value) + ">");
                serialPort.WriteLine("");
            }
            else if (xDist > 0)
            {
                Debug.WriteLine("Robot:: Move right " + xDist);
                serialPort.WriteLine("<panright:" + xDist + ">");
                serialPort.WriteLine("");
            }

            if (yDist < 0)
            {
                Debug.WriteLine("Robot:: Move up " + Math.Abs(yDist.Value));
                serialPort.WriteLine("<tiltup:" + Math.Abs(yDist.Value) + ">");
                serialPort.WriteLine("");
            }
            else if (yDist > 0)
            {
                Debug.WriteLine("Robot:: Move down " + yDist);
                serialPort.WriteLine("<tiltdown:" + yDist + ">");
                serialPort.WriteLine("");
            }
        }

        public void Set(int? pan, int? tilt)
        {
            if (disable)
                return;

            if (pan == null)
                pan = this.pan;
            else
                this.pan = pan.Value;

            if (tilt == null)
                tilt = this.tilt;
            else
                this.tilt = tilt.Value;

            Debug.WriteLine("Robot:: Setting pan " + pan + ", tilt " + tilt);
            serialPort.WriteLine("<panset:" + pan + ">");
            serialPort.WriteLine("<tiltset:" + tilt + ">");
        }

        public void SetLED(int? r, int? g, int? b)
        {
            if (disable)
                return;

            // keel local values up to date and apply local value if param is null

            if (r == null)
                r = this.r;
            else
                this.r = r.Value;

            if (g == null)
                g = this.g;
            else
                this.g = g.Value;

            if (b == null)
                b = this.b;
            else
                this.b = b.Value;

            Debug.WriteLine("Robot:: Setting R,G,B to " + this.r + "," + this.g + "," + this.b);
            serialPort.WriteLine("<setr:" + r + ">");
            serialPort.WriteLine("<setg:" + g + ">");
            serialPort.WriteLine("<setb:" + b + ">");
        }

        public void SetServoMoveSpeed(int speed)
        {
            if (disable)
                return;

            this.servoMoveSpeed = speed;
            Debug.WriteLine("Robot:: Setting Servo Movement Speed to " + speed);
            serialPort.WriteLine("<setservospeed:" + speed + "> ");
        }

        public void SetLEDChangeSpeed(int speed)
        {
            if (disable)
                return;

            this.ledChangeSpeed = speed;
            Debug.WriteLine("Robot:: Setting LED Change Speed to " + speed);
            serialPort.WriteLine("<setledspeed:2>");
        }

        public void OpenSerialPort()
        {
            if (disable)
            {
                this.Ready = true;
                return;
            }

            string portNum = "";
            serialPort.Close();

            foreach (string ports in SerialPort.GetPortNames())
            {
                portNum = ports.ToString();
                Debug.WriteLine("Robot:: Trying port: " + portNum);

                try
                {
                    serialPort.Close();
                    serialPort.PortName = (portNum);
                    serialPort.BaudRate = 9600;
                    serialPort.DataBits = 8;
                    serialPort.Parity = Parity.None;
                    serialPort.StopBits = StopBits.One;
                    serialPort.Handshake = Handshake.None;
                    serialPort.Encoding = System.Text.Encoding.Default;
                    serialPort.Open();
                    serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);
                    SetLEDChangeSpeed(this.ledChangeSpeed);
                    SetServoMoveSpeed(this.servoMoveSpeed);
                    serialPort.WriteLine("");
                    Ready = true;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Robot:: Unable to use port.");
                    Debug.WriteLine(e.Message);
                }
            }
        }

        void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] buffer = new byte[serialPort.ReadBufferSize];
            int bytesRead = serialPort.Read(buffer, 0, buffer.Length);
            tString += Encoding.ASCII.GetString(buffer, 0, bytesRead);

            if (tString.IndexOf((char)serialStringTerminator) > -1)
            {
                string workingString = tString.Substring(0, tString.IndexOf((char)serialStringTerminator));
                tString = tString.Substring(tString.IndexOf((char)serialStringTerminator));
                Debug.WriteLine("Robot:; Serial Data");
                Debug.WriteLine("    '" + workingString + "'");
            }
        }
    }
}
