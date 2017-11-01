# CamBot!
Arduino & C# Automated desktop webcam robot.

This is a project to make a (somewhat) expressive, desk toy webcam that looks for, follows, and recognizes faces. It's based on Hackerbox kit #24 ( https://hackerboxes.com/collections/frontpage/products/hackerbox-0024-vision-quest ) with a few added parts where needed for additional functionality.

### Current developement progress:

| Progress        | Description           
| ------------- |:-------------:
| Done! | Initial build. |
| Done! | Write Initial Arduino Serial interface API |
| 50% | Write Initial C# desktop app to prove out tracking and Serial interfacing. |
| 20% | Put together github for everything. |
| 0% | Put together tutorial video on initial build, initial code, key learnings and demonstration. |
| 0% | 2nd pass at Arduino code, clean things up, break them into seperate files, etc. |
| 0% | 2nd pass at C# desktop app, clean things up, break them into seperate classes, etc. |
| 0% | 3d print nicer cover for electronics and paint / finish stand. |

There are 2 sides to the software on this project.
- The Arduino code that creates a Serial Interface to control the robot and other hardware.
- The C# code to create a desktop app to read the webcam, look for interesting activity and control the robot ( via Arduino Serial Interface mentioned above. )

The arduino code is pretty strait forward. There is a set of commands to control every piece of hardware. I generally write the Serial interpretter to take commands in the form of <COMMAND:PARAMETER>, that way if we end up with multiple commands in the buffer at a time they can easily be broken apart and dealt with individually. I also like to write this code so that you can define where you want the state of things to be, and how fast you want to move towards those states. That way you can just send one command that says "Hey, I want the light to fade to green slowly" with a few commands and not have to actually send all of the rgb values to animate that yourself.

The C# code is currently using a wrapper around the amazing [Open CV library](https://opencv.org/) for feature detection from the webcam feed. You can find this wrapper [Here](http://www.emgu.com/wiki/index.php/Main_Page). At the moment I am just hacking up the movement detection example to suite my needs, but eventually I will start my own, more organized, app.

Eventually I would love for the app to have some personality, it would look for faces and follow them. When it see's people it 'likes' it would light up with a happy color and beep in delight. Something that just sits on the desk and looks adorable. Maybe it would unlock your computer for you, or do other more useful things. Some kind of google home type integration?!
