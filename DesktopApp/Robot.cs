using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;

namespace VideoSurveilance
{
    public class Robot
    {
        public bool Ready = false;
        public int R = 0; // TODO: turn this into get / set instead
        public int G = 0; // TODO: turn this into get / set instead
        public int B = 0; // TODO: turn this into get / set instead
        public int LEDChangeSpeed = 0; // TODO: turn this into get / set instead
        public int Pan = 0; // TODO: turn this into get / set instead
        public int Tilt = 0; // TODO: turn this into get / set instead
        public int ServoMoveSpeed = 0; // TODO: turn this into get / set instead
        public int HomePanValue = 0;
        public int HomeTiltValue = 0;

        private string tString = string.Empty;
        private byte serialStringTerminator = 0x4;
        private SerialPort serialPort = new SerialPort();

        public Robot(int servoMoveSpeed, int ledChangeSpeed, int homePanValue, int homeTiltValue)
        {
            LEDChangeSpeed = ledChangeSpeed;
            ServoMoveSpeed = servoMoveSpeed;
            HomePanValue = homePanValue;
            HomeTiltValue = homeTiltValue;

            OpenSerialPort();
            if (Ready)
                Home();
        }

        public void Home()
        {
            SetLED(0, 0, 255);
            Set(HomePanValue, HomeTiltValue);
            Debug.WriteLine("Homing");
        }

        public void Move(int? xDist, int? yDist)
        {
            if (xDist == null)
            {
                xDist = 0;
            }
            else
            {
                Pan += xDist.Value;
            }

            if (yDist == null)
            {
                yDist = 0;
            }
            else
            {
                Tilt += yDist.Value;
            }

            if (xDist < 0)
            {
                Debug.WriteLine("Move left " + Math.Abs(xDist.Value));
                serialPort.WriteLine("<panleft:" + Math.Abs(xDist.Value) + ">");
                serialPort.WriteLine("");
            }
            else if (xDist > 0)
            {
                Debug.WriteLine("Move right " + xDist);
                serialPort.WriteLine("<panright:" + xDist + ">");
                serialPort.WriteLine("");
            }

            if (yDist < 0)
            {
                Debug.WriteLine("Move up " + Math.Abs(yDist.Value));
                serialPort.WriteLine("<tiltup:" + Math.Abs(yDist.Value) + ">");
                serialPort.WriteLine("");
            }
            else if (yDist > 0)
            {
                Debug.WriteLine("Move down " + yDist);
                serialPort.WriteLine("<tiltdown:" + yDist + ">");
                serialPort.WriteLine("");
            }
        }

        public void Set(int? pan, int? tilt)
        {
            if (pan == null)
                pan = Pan;
            else
                Pan = pan.Value;

            if (tilt == null)
                tilt = Tilt;
            else
                Tilt = tilt.Value;

            Debug.WriteLine("Setting pan " + pan + ", tilt " + tilt);
            serialPort.WriteLine("<panset:" + pan + ">");
            serialPort.WriteLine("<tiltset:" + tilt + ">");
        }

        public void SetLED(int? r, int? g, int? b)
        {
            // keel local values up to date and apply local value if param is null

            if (r == null)
                r = R;
            else
                R = r.Value;

            if (g == null)
                g = G;
            else
                G = g.Value;

            if (b == null)
                b = B;
            else
                B = b.Value;

            // send commands
            
            serialPort.WriteLine("<setr:" + r + ">");
            serialPort.WriteLine("<setg:" + g + ">");
            serialPort.WriteLine("<setb:" + b + ">");
        }

        public void SetServoMoveSpeed(int speed)
        {
            serialPort.WriteLine("<setservospeed:" + speed + "> ");
        }

        public void SetLEDChangeSpeed(int speed)
        {
            serialPort.WriteLine("<setledspeed:2>");
        }

        public void OpenSerialPort()
        {
            string portNum = "";
            serialPort.Close();

            foreach (string ports in SerialPort.GetPortNames())
            {
                portNum = ports.ToString();
                Debug.WriteLine("Trying port: " + portNum);

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
                    SetLEDChangeSpeed(LEDChangeSpeed);
                    SetServoMoveSpeed(ServoMoveSpeed);
                    serialPort.WriteLine("");
                    Ready = true;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Unable to use port.");
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
                Debug.WriteLine("Serial Data");
                Debug.WriteLine(workingString);
            }
        }
    }
}
