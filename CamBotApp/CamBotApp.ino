#include <Servo.h>
#include <Adafruit_NeoPixel.h>

#define TILT_SERVO_PIN    5
#define PAN_SERVO_PIN     3
#define NEOPIXEL_PIN      4
#define NEOPIXEL_COUNT    1

#define PAN_LOWER_BOUNDS  10
#define PAN_UPPER_BOUNDS  140
#define TILT_LOWER_BOUNDS 10
#define TILT_UPPER_BOUNDS 140

Servo servoTilt, servoPan;
Adafruit_NeoPixel pixels = Adafruit_NeoPixel(NEOPIXEL_COUNT, NEOPIXEL_PIN, NEO_GRB + NEO_KHZ800);

const char COMMAND_BEGIN_CHAR = '<';
const char COMMAND_END_CHAR = '>';
const byte COMMAND_LENGTH = 32;

char receivedChars[COMMAND_LENGTH];
boolean newData = false;
char receivedNewData[COMMAND_LENGTH];
int panAmount = 20;
int tiltAmount = 20;
int actualPanAmount = 40;
int actualTiltAmount = 40;
float r = 0.0f;
float g = 0.0f;
float b = 0.0f;
float newR = 255.0f;
float newG = 255.0f;
float newB = 255.0f;
float changeSpeed = 1.0f;
int servoStepWait = 20;

void setup()
{
  servoTilt.attach(TILT_SERVO_PIN);  //The Tilt servo is attached to pin 2
  servoPan.attach(PAN_SERVO_PIN);   //The Pan servo is attached to pin 3
  
  servoTilt.write(tiltAmount);  //Initially position both servos to 25
  servoPan.write(panAmount);      
  
  Serial.begin(9600);
  
  pixels.begin();  
}

void loop()
{
  readSerial();
  
  if (receivedNewData[0] != 0 && receivedNewData[1] != 0 && receivedNewData[2] != 0) {
  
    // seperate the command from the parameter 
    char* command = strtok(receivedNewData, ":");
    char* param = strtok(NULL, ":");

    processCommand(command, param);
  }

  updateServos();
  updateLED();
}

void updateServos() {

  if (panAmount != actualPanAmount) {

    if (panAmount > actualPanAmount)
      actualPanAmount ++;
    else
      actualPanAmount --;
      
    servoPan.write(actualPanAmount);
  }

  if (tiltAmount != actualTiltAmount) {

    if (tiltAmount > actualTiltAmount)
      actualTiltAmount ++;
    else
      actualTiltAmount --;
      
    servoTilt.write(actualTiltAmount);
  }

  delay(servoStepWait);
}

void updateLED() {

  if (r > newR)
    r -= changeSpeed;

  if (r < newR)
    r += changeSpeed;

  if (g > newG)
    g -= changeSpeed;

  if (g < newG)
    g += changeSpeed;

  if (b > newB)
    b -= changeSpeed;

  if (b < newB)
    b += changeSpeed;
    
  pixels.setPixelColor(0, pixels.Color(int(r), int(g), int(b))); 
  pixels.show(); 
}

void processCommand(char* command, char* param) {

  if (strcmp(command, "panset") == 0) {

    Serial.print("Pan Set: ");
    panAmount = actualPanAmount = atoi(param);
    servoPan.write(panAmount);
    Serial.println(panAmount);
    
  } else if (strcmp(command, "tiltset") == 0) {
    
    Serial.print("Tilt Set: ");
    tiltAmount = actualTiltAmount = atoi(param);
    servoTilt.write(tiltAmount);
    Serial.println(tiltAmount);
    
  } else if (strcmp(command, "panleft") == 0) {
    
    Serial.print("Pan Left: ");
    panAmount -= atoi(param);

    if (panAmount < PAN_LOWER_BOUNDS)
      panAmount = PAN_LOWER_BOUNDS;
    
    Serial.println(panAmount);
    
  } else if (strcmp(command, "panright") == 0) {
    
    Serial.print("Pan Right: ");
    panAmount += atoi(param);

    if (panAmount > PAN_UPPER_BOUNDS)
      panAmount = PAN_UPPER_BOUNDS;
    
    Serial.println(panAmount);
    
  } else if (strcmp(command, "tiltup") == 0) {
    
    Serial.print("Tilt Up: ");
    tiltAmount -= atoi(param);

    if (tiltAmount < TILT_LOWER_BOUNDS)
      tiltAmount = TILT_LOWER_BOUNDS;
    
    Serial.println(tiltAmount);
    
  } else if (strcmp(command, "tiltdown") == 0) {

    Serial.print("Tilt Down: ");
    tiltAmount += atoi(param);

    if (tiltAmount > TILT_UPPER_BOUNDS)
      tiltAmount = TILT_UPPER_BOUNDS;
    
    Serial.println(tiltAmount);
    
  } else if (strcmp(command, "setr") == 0) {

    Serial.print("Set LED R: ");
    newR = atoi(param);

    if (newR > 254)
      newR = 254;

    if (newR < 0)
      newR = 0;
    
    Serial.println(newR);
    
  } else if (strcmp(command, "setg") == 0) {

    Serial.print("Set LED G: ");
    newG = atoi(param);

    if (newG > 254)
      newG = 254;

    if (newG < 0)
      newG = 0;
    
    Serial.println(newG);
    
  } else if (strcmp(command, "setb") == 0) {

    Serial.print("Set LED B: ");
    newB = atoi(param);

    if (newB > 254)
      newB = 254;

    if (newB < 0)
      newB = 0;
    
    Serial.println(newB);
    
  } else if (strcmp(command, "setledspeed") == 0) {

    Serial.print("Set LED Change Speed: ");
    changeSpeed = atoi(param);

    if (changeSpeed > 10)
      changeSpeed = 10;

    if (changeSpeed < 0)
      changeSpeed = 0;
    
    Serial.println(changeSpeed);
  } else if (strcmp(command, "setservospeed") == 0) {

    Serial.print("Set Servo Move Speed: ");
    servoStepWait = atoi(param);

    if (servoStepWait > 500)
      servoStepWait = 5000;

    if (servoStepWait < 1)
      servoStepWait = 1;
    
    Serial.println(servoStepWait);
  }
}

// commands should be in the format "<COMMAND:PARAMER>"

void readSerial() {
  
  static boolean recvInProgress = false;
  static byte ndx = 0;
  char rc;
  
  while (Serial.available() > 0 && newData == false) {
    
    rc = Serial.read();

    if (recvInProgress == true) {
      
        if (rc != COMMAND_END_CHAR) {
          
            receivedChars[ndx] = rc;
            ndx++;
            
            if (ndx >= COMMAND_LENGTH) {
              
                ndx = COMMAND_LENGTH - 1;
            }
        } else {
          
            receivedChars[ndx] = '\0'; // terminate the string
            recvInProgress = false;
            ndx = 0;
            newData = true;
        }
    }

    else if (rc == COMMAND_BEGIN_CHAR) {
      
        recvInProgress = true;
    }
  }

  if (newData == true) {

    newData = false;
    memcpy(
      receivedNewData, 
      receivedChars, 
      COMMAND_LENGTH * sizeof(char));
  } else {

    receivedNewData[0] = 0;
    receivedNewData[1] = 0;
    receivedNewData[2] = 0;
  }
}
