using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using Microsoft.Kinect;
using System.Windows.Media.Animation;

namespace ArduinoController
{
    public partial class MainWindow : Window
    {
        #region Wheel Instance Variables
        /// <summary>Top of the wheel</summary>
        private double wheelTop;

        /// <summary>Left of the wheel</summary>
        private double wheelLeft;

        /// <summary>Width of the wheel</summary>
        private double wheelWidth;

        /// <summary>Height of the wheel</summary>
        private double wheelHeight;

        /// <summary>Center of the wheel</summary>
        private Point wheelCenter;
        #endregion

        #region Wheel Lock Instance Variables
        /// <summary>
        /// The wheel lock is the point at which the right hand is bound to the wheel.
        /// That point is the center of the first quadrant of the wheel.
        /// </summary>
        private Point wheelLock;

        /// <summary>
        /// The "slope" of the wheel lock is the slope from the wheel lock to the wheel center.
        /// </summary>
        private double wheelLockSlope;

        /// <summary>How far the wheel was rotated in the last frame</summary>
        private double lastRotation;
        #endregion

        /// <summary> True when the wheel has rotated more than 180 degrees; set to false when the wheel crosses back. </summary>
        private Boolean crossedWallFromRight;
        /// <summary> True when the wheel has rotated less than -180 degrees; set to false when wheel crosses back. </summary>
        private Boolean crossedWallFromLeft;

        /// <summary>
        /// Initializes components and instance variables for Steering Mode
        /// </summary>
        private void initSteeringComponents() {
            wheelTop = Canvas.GetTop(wheel);
            wheelLeft = Canvas.GetLeft(wheel);
            wheelWidth = wheel.Width;
            wheelHeight = wheel.Height;
            wheelCenter = new Point(wheelLeft + wheelWidth / 2, wheelTop + wheelHeight / 2);

            double wheelLockX = wheelCenter.X + wheelWidth / 4;
            double wheelLockY = wheelCenter.Y - wheelHeight / 4;
            wheelLock = new Point(wheelLockX, wheelLockY);
            wheelLockSlope = -1 * (wheelLock.Y - wheelCenter.Y) / (wheelLock.X - wheelCenter.X);
        }

        #region Main Behavior and Helpers
        /// <summary>
        /// The main Standard Mode function:
        ///  - Updates the internal representation of the hands
        ///  - Signals the Arduino when ready to drive
        ///  - Signals the Arduino when to stop
        ///  - Responsible for calculating the rotation of the wheel and updating the wheel in the GUI
        ///  - Transforms and sends the rotation data to the Arduino and the servos
        /// </summary>
        private void steeringBehavior() {

            updateHandPositions();

            if (stopped) {

                // if hands are in driving position and Arduino is stopped, then begin driving
                if (IsReadyToDrive()) StartMoving();
                // keep arduino stopped if it's stopped and we're not ready to drive
                else return;
            }
            // If Arduino is moving and hands are in stop area, stop driving
            else if (IsLHandOverObject(kinectHandL, stopButton) ||
                    IsRHandOverObject(kinectHand, stopButton)) {
                StopMoving();
                wheel.RenderTransform = new RotateTransform(0);
                crossedWallFromLeft = false;
                crossedWallFromRight = false;
                return;
            }

            // send driving data to Arduino and adjust wheel
            double nextRotation = getRotation();
            wheel.RenderTransform = new RotateTransform(nextRotation);
            TransformAndSendDataToArduino(nextRotation);
            lastRotation = nextRotation;
        }

        /// <summary>
        /// We are ready to drive if R hand in quadrant I and L hand in quadrant IV
        /// </summary>
        /// <returns>A boolean indicating whether we are ready to drive the car (right hand in QI of wheel and left hand in QIV of wheel)</returns>
        private Boolean IsReadyToDrive() {

            // is R hand in QI?
            if (!(_RhandX > wheelCenter.X &&
                _RhandX < wheelCenter.X + wheelWidth / 2 &&
                _RhandY < wheelCenter.Y &&
                _RhandY > wheelCenter.Y - wheelHeight / 2))
                return false;

            // is L hand in QIV
            if (!(_LhandX > wheelCenter.X - wheelWidth / 2 &&
                _LhandX < wheelCenter.X &&
                _LhandY < wheelCenter.Y &&
                _LhandY > wheelCenter.Y - wheelHeight / 2))
                return false;

            // The R hand is in QI and the L hand is in QIV, so we're ready to drive!!!
            return true;
        }

        /// <summary>
        /// This method calculates the rotation of the wheel.
        /// It is calculated by finding the dot product between two vectors: 
        /// a) the vector from the wheel center to the wheel lock and
        /// b) the vector from the wheel center to the position of the right hand.
        /// Once we have the dot product, we can easily find the angle between the two vectors, which can be regarded as the wheel rotation.
        /// </summary>
        /// <returns>The rotation of the wheel from the original position; negative if the rotation is to the left, positive if to the right</returns>
        private double getRotation() {

            // use the dot product method to find the rotation of the wheel
            double distX = _RhandX - wheelCenter.X;
            double distY = _RhandY - wheelCenter.Y;
            double lockdistX = wheelLock.X - wheelCenter.X;
            double lockdistY = wheelLock.Y - wheelCenter.Y;
            double dotproduct = distX * lockdistX + distY * lockdistY;

            double dist = Math.Sqrt(distX * distX + distY * distY);
            double lockdist = Math.Sqrt(lockdistX * lockdistX + lockdistY * lockdistY);
            double angle = Math.Acos(dotproduct / (dist * lockdist)) * 180 / Math.PI;

            // is the angle to the left or to the right? Negative angles are to the left, and positive angles are to the right
            double y = wheelCenter.Y - _RhandY;
            double x = _RhandX - wheelCenter.X;
            angle = (y <= x) ? angle : (-1 * angle);

            // did the user's hand go more than 180 degrees/did it turn really far right?
            if (lastRotation > 150 && angle < 0) {
                crossedWallFromRight = true;
                angle = 180;
            }
            // did the user's hand previously go more than 180 degrees, and did it come back to be less than 180 degrees?
            else if (crossedWallFromRight && lastRotation == 180 && angle > 150) {
                crossedWallFromRight = false;
            }
            // did the user's hand go less than -180 degrees/did it turn really far left?
            else if (lastRotation < -150 && angle > 0) {
                crossedWallFromLeft = true;
                angle = -180;
            }
            // did the user's hand previously go less than -180 degrees, and did it come back to be more than -180 degrees?
            else if (crossedWallFromLeft && lastRotation == -180 && angle < -150) {
                crossedWallFromLeft = false;
            }

            return angle;
        }

        /// <summary>
        /// Transforms hand positions into servos data and sends that to the Arduino
        /// </summary>
        /// <param name="rotation"></param>
        private void TransformAndSendDataToArduino(double rotation) {
            // if rotation is negative, move left
            if (rotation < 0)
                ArduinoSendLeftRight((float)(180 - (-1 * rotation)), ArduinoRfwd);

            // if rotation is positive, move right
            else if (rotation > 0)
                ArduinoSendLeftRight(ArduinoLfwd, (float)rotation);

            // otherwise, move forward
            else
                ArduinoSendLeftRight(ArduinoLfwd, ArduinoRfwd);
        }
        #endregion

        #region Turn On/Off Steering Mode
        /// <summary>
        /// Adds fade in animations (that turn on Standard Mode) to the given storyboard.
        /// </summary>
        /// <param name="storyboard">The storyboard on which the animations for fading out/in will be played</param>
        private void turnOnSteering(Storyboard storyboard) {
            stopped = true;
            crossedWallFromLeft = false;
            crossedWallFromRight = false;

            kinectHandL.Visibility = System.Windows.Visibility.Visible;
            kinectHandL.IsEnabled = true;

            stopButton.Opacity = 0.0;
            stopButton.Visibility = System.Windows.Visibility.Visible;
            stopButton.IsEnabled = true;
            storyboard.Children.Add(newAnimation(stopButton, 0, 1.0, fadeIn, fadeOut));

            wheel.Opacity = 0.0;
            wheel.Visibility = System.Windows.Visibility.Visible;
            wheel.IsEnabled = true;
            storyboard.Children.Add(newImageAnimation(wheel, 0, 1.0, fadeIn, fadeOut));

            foreach (TextBox box in textBoxes) {
                box.Visibility = System.Windows.Visibility.Visible;
                storyboard.Children.Add(newAnimation(box, 0, 1.0, fadeIn, fadeOut));
            }

            foreach (Label label in labels) {
                label.Visibility = System.Windows.Visibility.Visible;
                storyboard.Children.Add(newAnimation(label, 0, 1.0, fadeIn, fadeOut));
            }

            storyboard.Children.Add(newBackgroundAnimation(ViewerCanvas, 0, 1, fadeIn, fadeOut));
            shiftViewerTo(60, 200);
            storyboard.Children.Add(newBackgroundAnimation(mainCanvas, 0.0, 1.0, fadeIn, fadeOut));

            currentMode = Mode.STEERING;
        }

        /// <summary>
        /// Adds fade out animations (that turn off Standard Mode) to the given storyboard.
        /// </summary>
        /// <param name="storyboard">The storyboard on which the animations for fading out/in will be played</param>
        private void turnOffSteering(Storyboard storyboard) {
            StopMoving();
            wheel.RenderTransform = new RotateTransform(0);
            crossedWallFromLeft = false;
            crossedWallFromRight = false;

            kinectHandL.Visibility = System.Windows.Visibility.Hidden;
            kinectHandL.IsEnabled = false;

            stopButton.IsEnabled = false;
            storyboard.Children.Add(newAnimation(stopButton, 1.0, 0, fadeOut, timeZero));
            
            wheel.IsEnabled = false;
            storyboard.Children.Add(newImageAnimation(wheel, 1.0, 0, fadeOut, timeZero));

            foreach (TextBox box in textBoxes) {
                storyboard.Children.Add(newAnimation(box, 1.0, 0.0, fadeOut, timeZero));
            }

            foreach (Label label in labels) {
                storyboard.Children.Add(newAnimation(label, 1.0, 0.0, fadeOut, timeZero));
            }

            storyboard.Children.Add(newBackgroundAnimation(mainCanvas, 0, 0, fadeOut, timeZero));
            storyboard.Children.Add(newBackgroundAnimation(ViewerCanvas, 0, 0, fadeOut, timeZero));
        }
        #endregion
    }
}
