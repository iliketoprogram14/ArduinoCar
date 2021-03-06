# Kinect Arduino Car #

## Summary ##

For the ES50 course in college, I created a Kinect application that accepts natural user input (ie, motion and voice input), parses the input into commands, and forwards the commands to a Arduino-powered car wirelessly.

## Description ##

I took an introductory electrical engineering class called [ES50](https://www.facebook.com/EngSci50) in the spring of my junior year.  I really enjoyed it, and it got me interested in using [Arduinos](http://arduino.cc) to automate things in real life, not just in the digital world.  In fact, the end result of the class can be seen in this video:

[![Kinect Arduino Car](http://img.youtube.com/vi/4v98L51F9Vw/0.jpg)](http://youtube.com/watch?v=4v98L51F9Vw "Kinect-controlled Arduino Car")

My final project for the class ended up being a lot heavier on the software side of things though.  I ended up designing and creating a [Kinect](http://www.xbox.com/en-US/kinect) application that took in motion input and/or voice input, parsed the input into commands, and sent those commands over a wireless [Xbee](http://digi.com/xbee) connection to an Arduino-powered robotic car. 

The application has 3 "modes" which are basically 3 different ways to give motion input to the car. The application supports voice input for both controlling the car and navigating the application.

You can read the full specification for the project [here](http://iliketoprogram.com/assets/pdf/KinectSpec.pdf).  Enjoy!

## Screenshots ##

#### The full setup ####
![Full setup](assets/fullsetup.png "The full setup")

#### Main menu ####
![Menu](assets/menu.png "Main menu")

#### Pod racing mode ####
![Pod racing mode](assets/podracing.png "Pod racing mode")

#### Precision steering mode ####
![Precision lock](assets/precisionlock.png "Precision steering mode")

#### Normal steering mode ####
![Steering](assets/steering.png "Normal steering mode")

#### The Xbee car ####
![Xbee car](assets/xbeecar.jpg "The Xbee car")
