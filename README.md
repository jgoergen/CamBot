# CamBot!
Arduino & C# Automated desktop webcam robot.

This is a project to make a (somewhat) expressive, desk toy webcam that looks for, follows, and recognizes faces. It's based on [Hackerbox kit #24](https://hackerboxes.com/collections/frontpage/products/hackerbox-0024-vision-quest) with a few added parts where needed for additional functionality.

## Video walkthrough of the hardware build
[![Cambot Video](https://img.youtube.com/vi/nbJNLVBo_g4/0.jpg)](https://www.youtube.com/watch?v=nbJNLVBo_g4)

### Current developement progress:

| Progress        | Description           
| ------------- |:-------------:
| Done! | Initial build. |
| Done! | Write Initial Arduino Serial interface API |
| Done! | Write Initial C# desktop app to prove out tracking and Serial interfacing. |
| Done! | Put together github for everything. |
| Done! | Put together tutorial video on initial build, initial code, key learnings and demonstration. |
| 50% | 2nd pass at Arduino code, clean things up, break them into seperate files, etc. |
| 40% | 2nd pass at C# desktop app, clean things up, break them into seperate classes, etc. |
| 0% | 3d print nicer cover for electronics and paint / finish stand. |

There are 2 sides to the software on this project.
- The Arduino code that creates a Serial Interface to control the robot and other hardware.
- The C# code to create a desktop app to read the webcam, look for interesting activity and control the robot ( via Arduino Serial Interface mentioned above. )

The hardware consists of:
( Amazon links added for beginners looking for easier options to purchase. )
1. An Arduino Nano V3 to control everything [Amazon Link](https://www.amazon.com/ATmega328P-Microcontroller-Board-Cable-Arduino/dp/B00NLAMS9C/ref=sr_1_6?ie=UTF8&qid=1528301653&sr=8-6&keywords=arduino+nano+v3)
2. A single Neopixel RGB LED [Amazon Link](https://www.amazon.com/Adafruit-Flora-RGB-Smart-NeoPixel/dp/B00KBXTJRQ/ref=sr_1_5?s=electronics&ie=UTF8&qid=1528301716&sr=1-5&keywords=neopixel+led)
3. 2 MG996R Servos [Amazon Link](https://www.amazon.com/Mallofusa-Mg995-Servos-Sensor-Arduino/dp/B00JXO9DBG/ref=sr_1_fkmr1_1?s=toys-and-games&ie=UTF8&qid=1528301810&sr=1-1-fkmr1&keywords=MG996R+Servo+Pan+%2F+Tilt+Assembly)
4. A Pan / Tilt Assembly [Amazon Link](https://www.amazon.com/Mallofusa-Mg995-Servos-Sensor-Arduino/dp/B00JXO9DBG/ref=sr_1_fkmr1_1?s=toys-and-games&ie=UTF8&qid=1528301810&sr=1-1-fkmr1&keywords=MG996R+Servo+Pan+%2F+Tilt+Assembly)
5. A laptop webcam assembly + USB adapter [Amazon Link](https://www.amazon.com/Logitech-960-000694-Widescreen-designed-Recording/dp/B004FHO5Y6/ref=sr_1_3?s=electronics&ie=UTF8&qid=1528301979&sr=1-3&keywords=usb+webcam)
6. A repurposed power adapter from an old cellphone [Amazon Link](https://www.amazon.com/Charger-Adapter-Charging-Paperwhite-Aaweal/dp/B075T9ZRY2/ref=sr_1_1_sspa?s=electronics&ie=UTF8&qid=1528302019&sr=1-1-spons&keywords=usb+2a+charger&psc=1)
7. 2 usb cables
8. Scrap floor samples as a weighted base + cardboard for 'feet'
9. A tiny breadboard + prototyping connectors to keep things open to experimentation. [Amazon Link](https://www.amazon.com/Electronics-Component-tie-points-Breadboard-Potentiometer/dp/B073ZC68QG/ref=sr_1_1_sspa?s=electronics&ie=UTF8&qid=1528302060&sr=1-1-spons&keywords=breadboard&psc=1)

The arduino code is pretty straightforward. There is a set of commands to control every piece of hardware. I generally write the Serial interpretter to take commands in the form of <<COMMAND:PARAMETER>>, that way if we end up with multiple commands in the buffer at a time they can easily be broken apart and dealt with individually. I also like to write this code so that you can define where you want the state of things to be, and how fast you want to move towards those states. That way you can just send one command that says "Hey, I want the light to fade to green slowly" with a few commands and not have to actually send all of the rgb values to animate that yourself.

The C# code is currently using a wrapper around the amazing [Open CV library](https://opencv.org/) for movement detection from the webcam feed. You can find this library [Here](http://www.emgu.com/wiki/index.php/Main_Page). At the moment I am just hacking up the movement detection example to suite my needs, but eventually I will start my own, more organized, app.

I would love for the CamBot to have some personality, it would look for faces and follow them. When it see's people it 'likes' it would light up with a happy color and beep in delight. Something that just sits on the desk and looks adorable. Maybe it would unlock your computer for you, or do other more useful things. Some kind of google home type integration?!

## Before you try to run the C# code!
You will have to be able to run .net 4 code, and you will also have to install the EMGU libs ( linked above ) or even better, use Nuget to pull down the latest EMGU.CV libs. **Warning. This is hacked together code at the moment, jammed sideways into an example from EMGU.**

## Some things to consider ##

Movement detection is VERY sensitive to alot of things. Take care to minimize the amount of noise in your video feed. For example; dark rooms will be a problem if your camera doesn't use infrared. Also cheap cameras have alot of noise in general. Your robot will need to be seated on a totally still surface. ANY unplanned movement will totally 'unsettle' your motion detection ( it will think EVERYTHING is moving until it has time to settle again. ) 

If you go into the Desktop app C# code ( VideoSurveilance.cs ) you will see some settings towards the top of the file that you can play with to fine tune things alittle. I will add more comments for this, but in the meantime I'll describe a few that might be usefull to combat false positives and bad motion detection in general.

MS_PAUSE_DETECT_AFTER_MOVE is the number of milliseconds the desktop software will wait before it moves towards ( what it thinks to be ) movement. The significance of this is the more frames of video the library ( Open CV ) 'stacks' for detecting movement the more the stationary objects will 'settle' out of the image. ( This is why the image will slowly go mostly black over time. Open CV is trying to remove things that are the same between frames leaving only the moving stuff. ) So if you turn this up, it will help remove stationary objects, but it will make your camera less responsive ( as it has to wait before it can move again. )

SUBSTRACTION_HISTORY works in tandem with the above setting. If you give the library more time to settle, you have to increase the number of frames it has in it's buffer to 'stack'. So if you increase MS_PAUSE_DETECT_AFTER_MOVE you should turn this up as well. 

SUBTRACTION_THRESHOLD is something you will just have to play with, but the idea is the higher this number the less the library will assume a difference between frames is actually movement. Turning this up would probably help remove some webcam noise but if it's too high it will make the motion detection alot less usefull.

FRAME_BLUR_STRENGTH would help with webcam noise alot like SUBTRACTION_THRESHOLD. ( the higher the number, the more it smooths or blurs frames. ) Just be careful, this MUST be an odd number and the higher it gets, the less accurate the motion detection will be.
