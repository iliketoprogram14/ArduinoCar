using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Kinect;
using Coding4Fun.Kinect.Wpf;

namespace ArduinoController
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Enumerates all 4 modes that the main window can be in: 1. Menu mode 2. Steering mode 3. Precision mode 4. Pod Racing mode
        /// </summary>
        private enum Mode { MENU, STEERING, PRECISION, POD };

        #region Lists of the coordinate labels and boxes
        /// <summary> List of the coordinate labels </summary>
        private List<Label> labels;

        /// <summary> List of the coordinate boxes </summary>
        private List<TextBox> textBoxes;
        #endregion

        #region Animation instance variables
        /// <summary> Brush Converter used for converting images into brushes for canvas backgrounds </summary>
        private BrushConverter bc;

        /// <summary> The default canvas background brush </summary>
        private Brush darkBlue;

        /// <summary> The default fade out animation duration </summary>
        TimeSpan fadeOut = new TimeSpan(0, 0, 0, 0, 500);

        /// <summary> The default fade in animation duration </summary>
        TimeSpan fadeIn = new TimeSpan(0, 0, 0, 1);

        /// <summary> A cached copy of a timespan of duration 0 seconds </summary>
        TimeSpan timeZero = new TimeSpan(0);
        #endregion

        #region Hand instance variables
        /// <summary> The currently selected button by the right hand. In other words, the button that the right hand is hovering over. </summary>
        private static Button _selectedButton = null;
        
        /// <summary> The currently selected button by the left hand. In other words, the button that the left hand is hovering over. </summary>
        private static Button _selectedLButton = null;

        /// <summary> Boolean indicating whether the right hand is bound to a driving control. True when a user is driving. </summary>
        private Boolean rhandBound;

        /// <summary> Boolean indicating whether the left hand is bound to a driving control. True when a user is driving. </summary>
        private Boolean lhandBound;
        #endregion

        #region X and Y coordinates of both the left and right hands for the driving modes (non-menu modes)
        /// <summary> X coordinate for the center of the right hand for the driving modes (non-menu modes). </summary>
        private static double _RhandX;

        /// <summary> Y coordinate for the center of the right hand for the driving modes (non-menu modes). </summary>
        private static double _RhandY;

        /// <summary> X coordinate for the center of the left hand for the driving modes (non-menu modes). </summary>
        private static double _LhandX;

        /// <summary> Y coordinate for the center of the left hand for the driving modes (non-menu modes). </summary>
        private static double _LhandY;
        #endregion

        /// <summary> Boolean that indicates whether the car is stopped or not </summary>
        private Boolean stopped = true;

        #region Values of forward and stop for the left and right servos
        /// <summary> The maximum valid value for a servos motor </summary>
        private float ArduinoMax = 180;

        /// <summary> The forward value for the left servos motor </summary>
        private float ArduinoLfwd = 180;

        /// <summary>The forward value for the right servos motor  </summary>
        private float ArduinoRfwd = 0;

        /// <summary> The value to stop a servos  </summary>
        private float ArduinoStop = 90;
        #endregion

        /// <summary> Initializes components and instance variables common to all modes </summary>
        private void initCommonComponents() {
            labels = new List<Label> { labelL, labelR, labelX, labelY };
            textBoxes = new List<TextBox> { BoxLX, BoxLY, BoxRX, BoxRY };
        }

        /// <summary>
        /// Delegates behavior based on what the current mode is
        /// </summary>
        private void compute() {
            switch (currentMode) {
                case Mode.MENU:
                    if (IsHandOverMenuButtons(kinectHand, menuButtons)) kinectHand.Hovering(); else kinectHand.Release();
                    break;
                case Mode.STEERING:
                    steeringBehavior();
                    break;
                case Mode.PRECISION:
                    precisionBehavior();
                    break;
                case Mode.POD:
                    podRacingBehavior();
                    break;
            }
        }

        /// <summary>
        /// Calls the turn off method for the current canvas
        /// </summary>
        /// <param name="s">The storyboard on which the animations for fading out/in will be played</param>
        private void turnOffCurrentCanvas(Storyboard s) {
            switch (currentMode) {
                case Mode.MENU:
                    turnOffMenu(s);
                    break;
                case Mode.STEERING:
                    turnOffSteering(s);
                    break;
                case Mode.POD:
                    turnOffPodRacing(s);
                    break;
                case Mode.PRECISION:
                    turnOffPrecision(s);
                    break;
            }
        }

        #region Driving Helpers

        /// <summary>
        /// Called when the BOE bot should start moving
        /// </summary>
        private void StartMoving() {
            stopped = false;
            vc.stopRecognizer(); // don't recognize voice commands while driving
        }

        /// <summary>
        /// Called when the BOE bot should stop moving
        /// </summary>
        private void StopMoving() {
            if (!stopped) vc.restartRecognizer(); // reset the voice recognition engine
            stopped = true;
            rhandBound = false;
            lhandBound = false;

            // stop the car and reset all sliders
            ArduinoSendLeftRight(ArduinoStop, ArduinoStop);
            leftEngine.Value = leftEngine.Maximum / 2;
            rightEngine.Value = rightEngine.Maximum / 2;
            LRslider.Value = LRslider.Maximum / 2;
            FRslider.Value = FRslider.Maximum / 2;
        }
        #endregion

        #region Hand Events and helpers

        /// <summary>
        /// Update the text boxes with the coordinates of the given joint
        /// </summary>
        /// <param name="joint">The joint whose coordinates will go into the coordinate boxes</param>
        /// <param name="jointID">The joint type/id of the given joint </param>
        private void UpdateBoxesWithCoords(Joint joint, JointType jointID) {
            Joint scaledJoint = joint.ScaleTo(ScreenMaxX, ScreenMaxY, 0.5F, 0.5F);

            if (jointID == JointType.HandRight) {
                BoxRX.Text = scaledJoint.Position.X.ToString();
                BoxRY.Text = scaledJoint.Position.Y.ToString();
            }

            if (jointID == JointType.HandLeft) {
                BoxLX.Text = scaledJoint.Position.X.ToString();
                BoxLY.Text = scaledJoint.Position.Y.ToString();
            }
        }

        /// <summary>
        /// Updates the hand positions, which are the center of the hands
        /// </summary>
        private void updateHandPositions() {
            var rHandTopLeft = new Point(Canvas.GetLeft(kinectHand), Canvas.GetTop(kinectHand));
            _RhandX = rHandTopLeft.X + (kinectHand.ActualWidth / 2);
            _RhandY = rHandTopLeft.Y + (kinectHand.ActualHeight / 2);

            var lHandTopLeft = new Point(Canvas.GetLeft(kinectHandL), Canvas.GetTop(kinectHandL));
            _LhandX = lHandTopLeft.X + (kinectHandL.ActualWidth / 2);
            _LhandY = lHandTopLeft.Y + (kinectHandL.ActualHeight / 2);
        }

        /// <summary>
        /// Event handler for when the right hand has "clicked" something
        /// </summary>
        /// <param name="sender">The object which sent this event</param>
        /// <param name="e">The "hand clicked something" event</param>
        private void kinectHand_Clicked(object sender, RoutedEventArgs e) {
            Console.Out.WriteLine("Hand is clicking");
            _selectedButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, _selectedButton));
        }

        /// <summary>
        /// Event handler for when the left hand has "clicked" something
        /// </summary>
        /// <param name="sender">The object which sent this event</param>
        /// <param name="e">The "hand clicked something" event</param>
        private void kinectHandL_Click(object sender, RoutedEventArgs e) {
            Console.Out.WriteLine("Left hand is clicking");
            _selectedLButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, _selectedLButton));

        }

        #endregion

        #region Button Helpers
        
        /// <summary>
        /// Is the left hand over the given button/target?
        /// </summary>
        /// <param name="hand">The left hand</param>
        /// <param name="target">The button in question</param>
        /// <returns>A boolean indicating whether or not the left hand is over the given button/target</returns>
        public static bool IsLHandOverObject(FrameworkElement hand, Button target) {
            if (_isClosing || !Window.GetWindow(hand).IsActive) return false;

            // get the location of the top left of the hand and then use it to find the middle of the hand
            var handTopLeft = new System.Windows.Point(Canvas.GetLeft(hand), Canvas.GetTop(hand));
            _LhandX = handTopLeft.X + (hand.ActualWidth / 2);
            _LhandY = handTopLeft.Y + (hand.ActualHeight / 2);

            //Point targetTopLeft = target.PointToScreen(new Point());
            return isHandInButton(_LhandX, _LhandY, target, false);
        }

        /// <summary>
        /// Is the right hand over the given button/target?
        /// </summary>
        /// <param name="hand">the right hand </param>
        /// <param name="target">The button in question</param>
        /// <returns>A boolean indicating whether or not the right hand is over the given button/target</returns>
        public static bool IsRHandOverObject(FrameworkElement hand, Button target) {
            if (_isClosing || !Window.GetWindow(hand).IsActive) return false;

            // get the location of the top left of the hand and then use it to find the middle of the hand
            var handTopLeft = new System.Windows.Point(Canvas.GetLeft(hand), Canvas.GetTop(hand));
            _RhandX = handTopLeft.X + (hand.ActualWidth / 2);
            _RhandY = handTopLeft.Y + (hand.ActualHeight / 2);

            //Point targetTopLeft = target.PointToScreen(new Point());
            return isHandInButton(_RhandX, _RhandY, target, true);
        }
        #endregion

        #region Animation Helpers
        /// <summary>
        /// Used for fade in/out animations for controls. This is done by manipulating the opacity property of the given control.
        /// </summary>
        /// <param name="control">The control on which the fade in/out will be performed</param>
        /// <param name="from">The initial opacity when the animation begins</param>
        /// <param name="to">The final opacity when the animation ends</param>
        /// <param name="duration">The length in time of the animation</param>
        /// <param name="offset">How long the animation should wait before it begins</param>
        /// <returns>A fade in or fade out animation over the given control</returns>
        private DoubleAnimation newAnimation(System.Windows.Controls.Control control, double from, double to,
            TimeSpan duration, TimeSpan offset) {
            DoubleAnimation animation = new DoubleAnimation { BeginTime = offset, From = from, To = to, Duration = new Duration(duration) };
            Storyboard.SetTargetName(animation, control.Name);
            Storyboard.SetTargetProperty(animation, new PropertyPath("Opacity", 0));
            return animation;
        }

        /// <summary>
        /// Used for fade in/out animations for images. This is done by manipulating the opacity property of the given image.
        /// </summary>
        /// <param name="img">The image on which the fade in/out will be performed</param>
        /// <param name="from">The initial opacity when the animation begins</param>
        /// <param name="to">The final opacity when the animation ends</param>
        /// <param name="duration">The length in time of the animation</param>
        /// <param name="offset">How long the animation should wait before it begins</param>
        /// <returns>A fade in or fade out animation over the given image</returns>
        private DoubleAnimation newImageAnimation(Image img, double from, double to,
            TimeSpan duration, TimeSpan offset) {
            DoubleAnimation animation = new DoubleAnimation { BeginTime = offset, From = from, To = to, Duration = new Duration(duration) };
            Storyboard.SetTargetName(animation, img.Name);
            Storyboard.SetTargetProperty(animation, new PropertyPath("Opacity", 0));
            return animation;
        }

        /// <summary>
        /// Used for fade in/out animations for canvases. This is done by manipulating the opacity property of the given canvas.
        /// </summary>
        /// <param name="canvas">The canvas on which the fade in/out will be performed</param>
        /// <param name="from">The initial opacity when the animation begins</param>
        /// <param name="to">The final opacity when the animation ends</param>
        /// <param name="duration">The length in time of the animation</param>
        /// <param name="offset">How long the animation should wait before it begins</param>
        /// <returns>A fade in or fade out animation over the given canvas</returns>
        private DoubleAnimation newBackgroundAnimation(Canvas canvas, double from, double to,
            TimeSpan duration, TimeSpan offset) {
            DoubleAnimation animation = new DoubleAnimation { BeginTime = offset, From = from, To = to, Duration = new Duration(duration) };
            Storyboard.SetTargetName(animation, canvas.Name);
            Storyboard.SetTargetProperty(animation, new PropertyPath("Opacity", 0));
            return animation;
        }

        /// <summary>
        /// Moves the viewer canvas (the video) to the specified x and y
        /// </summary>
        /// <param name="x">The x coordinate where the left of the viewer canvas should be</param>
        /// <param name="y">The y coordinate where the top of the viewer canvas should be</param>
        private void shiftViewerTo(int x, int y) {
            Canvas.SetLeft(ViewerCanvas, x);
            Canvas.SetTop(ViewerCanvas, y);
        }
        #endregion

    }
}
