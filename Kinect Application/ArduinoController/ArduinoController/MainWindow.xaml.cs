using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using System.Threading;

using Microsoft.Kinect;
using System.IO;
using System.IO.Ports;
using Coding4Fun.Kinect.Wpf;
using Microsoft.Expression.Drawing;
using System.ComponentModel;
using System.Windows.Threading;
using System.Windows.Media.Animation;

namespace ArduinoController
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Arduino instance vars
        /// <summary> Arduino's serial port </summary>
        private SerialPort serialPort;

        /// <summary> Boolean indicating whether there is an Arduino/serial device plugged in to write to </summary>
        private Boolean noArduino = false;

        /// <summary> Arduino's serial port number </summary>
        private String ComPortNum = "7";
        #endregion

        #region Window instance vars
        /// <summary> Width of the main window </summary>
        private int ScreenMaxX;

        /// <summary> Height of the main window </summary>
        private int ScreenMaxY;

        /// <summary> Tracks what mode the main window is in </summary>
        private Mode currentMode;

        /// <summary> Boolean indicating whether the main window is closing </summary>
        private static bool _isClosing = false;
        #endregion

        #region Kinect instance vars
        /// <summary> Array of all skeletons detected by the Kinect </summary>
        private Skeleton[] allSkeletons = new Skeleton[6];

        /// <summary> The voice recognizer object </summary>
        private VoiceCommands vc;
        #endregion

        /// <summary>
        /// The main window where all the action happens
        /// </summary>
        public MainWindow() {
            // init GUI
            InitializeComponent();

            // init Menu components
            menuButtons = new List<Button> { steeringButton, precisionButton, podButton };
            currentMode = Mode.MENU;
            ScreenMaxX = (int)this.Width;
            ScreenMaxY = (int)this.Height;

            // init other components
            initCommonComponents();
            initSteeringComponents();
            initPrecisionComponents();
            initPodRacingComponents();
        }

        #region Window Events
        /// <summary>
        /// Event handler called when the window has loaded.
        /// Adds the kinect detection event handler and opens the serial port
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The "window loaded" event</param>
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            // start up the kinect if it has been detected
            kinectSensorChooser1.KinectSensorChanged += new DependencyPropertyChangedEventHandler(kinectSensorChooser1_KinectSensorChanged);

            // open up the serial port for communication if an arduino is connected
            if (!noArduino) {
                ArduinoSetSerial();
                ArduinoOpenSerial();
                ArduinoSendLeftRight(ArduinoStop, ArduinoStop);
            }
        }

        /// <summary>
        /// Event handler called when the window is closing.
        /// Stops the kinect.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The "window closed" event</param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            StopKinect(kinectSensorChooser1.Kinect);
        }
        #endregion

        #region Kinect Events
        /// <summary>
        /// Event handler called when a new Kinect has been connected.
        /// Stops old Kinect, starts new Kinect, and starts voice recognition.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void kinectSensorChooser1_KinectSensorChanged(object sender, DependencyPropertyChangedEventArgs e) {
            // kill the old kinect
            KinectSensor oldSensor = (KinectSensor)e.OldValue;
            StopKinect(oldSensor);

            // get the new kinect
            KinectSensor newSensor = (KinectSensor)e.NewValue;
            if (newSensor == null) return;

            // smooth out movements
            var parameters = new TransformSmoothParameters {
                Smoothing = 0.3f,
                Correction = 0.0f,
                Prediction = 0.0f,
                JitterRadius = 1.0f,
                MaxDeviationRadius = 0.5f
            };

            newSensor.ColorStream.Enable(); // IS THIS LINE NEEDED???
            newSensor.DepthStream.Enable(); // IS THIS LINE NEEDED???

            // enable all streams and add the event handler called on each frame
            newSensor.SkeletonStream.Enable(parameters);
            newSensor.SkeletonStream.Enable();
            newSensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);

            // start the sensor and the voice recognizer
            try {
                newSensor.Start();
            }
            catch (System.IO.IOException) {
                kinectSensorChooser1.AppConflictOccurred();
            }
            vc = new VoiceCommands(newSensor, this);
            vc.startRecognizer();
        }

        /// <summary>
        /// Event handler called on every frame.
        /// Updates the hands on the GUI, does some computation depending on the current mode, 
        /// and updates coordinate text boxes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e) {
            // grab the skeleton and set the positions of the left and right hands
            Skeleton first = GetFirstSkeleton(e);
            if (first != null) {
                ScaleAndSetPosition(kinectHand, first.Joints[JointType.HandRight]);
                if (currentMode != Mode.MENU)
                    ScaleAndSetPosition(kinectHandL, first.Joints[JointType.HandLeft]);

                // update the GUI and compute data to send to the Arduino, depending on the current mode
                compute();

                // update the coordinates in the text boxes
                UpdateBoxesWithCoords(first.Joints[JointType.HandLeft], JointType.HandLeft);
                UpdateBoxesWithCoords(first.Joints[JointType.HandRight], JointType.HandRight);
            }
        }

        /// <summary>
        /// Stops the kinect and voice recognition
        /// </summary>
        /// <param name="s">The Kinect to be stopped</param>
        private void StopKinect(KinectSensor s) {
            if (s != null) {
                if (s.IsRunning) {
                    s.Stop();
                    if (s.AudioSource != null) {
                        vc.stopRecognizer();
                        s.AudioSource.Stop();
                    }
                }
            }
        }
        #endregion

        #region Kinect Helpers
        /// <summary>
        /// Helper to get the first skeleton that the Kinect senses
        /// </summary>
        /// <param name="e">The current frame</param>
        /// <returns>The first skeleton sensed by the Kinect</returns>
        private Skeleton GetFirstSkeleton(AllFramesReadyEventArgs e) {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame()) {
                if (skeletonFrameData == null) return null;

                skeletonFrameData.CopySkeletonDataTo(allSkeletons);

                //get the first tracked skeleton
                Skeleton first = (from s in allSkeletons
                                  where s.TrackingState == SkeletonTrackingState.Tracked
                                  select s).FirstOrDefault();

                return first;
            }
        }

        /// <summary>
        /// Updates the GUI with the position of the hands, scaled to fit the window
        /// </summary>
        /// <param name="element">The left or right hand element in the GUI</param>
        /// <param name="joint">This parameter is used to compute the scaled position for the element</param>
        private void ScaleAndSetPosition(FrameworkElement element, Joint joint) {
            //convert & scale (.5 = means 1/2 of joint distance)
            Joint scaledJoint = joint.ScaleTo(ScreenMaxX, ScreenMaxY, .5f, .5f);

            // set the position of the element in the canvas
            Canvas.SetLeft(element, scaledJoint.Position.X);
            Canvas.SetTop(element, scaledJoint.Position.Y);
        }

        /// <summary>
        /// Is the right hand in one of the menu buttons?
        /// </summary>
        /// <param name="hand">The right hand</param>
        /// <param name="buttons">The menu buttons</param>
        /// <returns></returns>
        /// <seealso cref="IsRHandOverObject">For the more general method, check out IsRHandOverObject</seealso>
        /// <seealso cref="IsLHandOverObject">For the more general method for the left hand, check out IsLHandOverObject</seealso>
        public static bool IsHandOverMenuButtons(FrameworkElement hand, List<Button> buttons) {
            if (_isClosing || !Window.GetWindow(hand).IsActive) return false;

            // get the location of the top left of the hand and then use it to find the middle of the hand
            var handTopLeft = new Point(Canvas.GetLeft(hand), Canvas.GetTop(hand));
            _handX = handTopLeft.X + (hand.ActualWidth / 2);
            _handY = handTopLeft.Y + (hand.ActualHeight / 2);

            // is the hand "in" or over a button/target?
            foreach (Button target in buttons) {
                if (isHandInButton(_handX, _handY, target, true)) return true;
            }
            return false;
        }

        /// <summary>
        /// A method that checks if a hand's coordinates are within the coordinate rectangle of the given button (target).
        /// </summary>
        /// <param name="handx">X coordinate of the hand</param>
        /// <param name="handy">Y coordinate of the hand</param>
        /// <param name="target">The button that the hand may be over</param>
        /// <param name="isRightHand">True if the hand is the right hand, false if it's the left</param>
        /// <returns></returns>
        private static bool isHandInButton(double handx, double handy, Button target, bool isRightHand) {
            System.Windows.Point targetTopLeft = new System.Windows.Point(Canvas.GetLeft(target), Canvas.GetTop(target));
            if (handx > targetTopLeft.X
                && handx < targetTopLeft.X + target.Width
                && handy > targetTopLeft.Y
                && handy < targetTopLeft.Y + target.Height) {
                if (isRightHand) _selectedButton = target;
                else _selectedLButton = target;
                return true;
            }
            return false;
        }

        #endregion

        #region Serial Methods
        /// <summary>
        /// Sets up the serial port for the Arduino
        /// </summary>
        private void ArduinoSetSerial() {
            String ArduinoCom = ComPortNum;
            serialPort = new SerialPort();
            serialPort.PortName = "COM" + ComPortNum;
            serialPort.BaudRate = 9600;
            serialPort.DataBits = 8;
            serialPort.Handshake = 0;
            serialPort.ReadTimeout = 500;
            serialPort.WriteTimeout = 500;
        }

        /// <summary>
        /// Opens the serial port for the Arduino
        /// </summary>
        private void ArduinoOpenSerial() {
            if (!serialPort.IsOpen) serialPort.Open();
            else System.Console.WriteLine("Serial port cannot be opened");
        }

        /// <summary>
        /// Closes the serial port for the Arduino
        /// </summary>
        private void ArduinoCloseSerial() {
            if (serialPort.IsOpen) serialPort.Close();
        }
        #endregion

        #region Arduino Methods
        /// <summary>
        /// Sends the left and right servos speeds to the Arduino
        /// </summary>
        /// <param name="lspeed">The speed of the left servo</param>
        /// <param name="rspeed">The speed of the right servo</param>
        private void ArduinoSendLeftRight(float lspeed, float rspeed) {
            Byte ls, rs;

            ls = (byte)lspeed;
            rs = (byte)rspeed;

            byte[] ArduinoBuffer = { ls, rs };
            if (!noArduino && serialPort.IsOpen)
                serialPort.Write(ArduinoBuffer, 0, ArduinoBuffer.Length);
        }
        #endregion


    }
}
