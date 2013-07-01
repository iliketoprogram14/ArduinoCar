using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Kinect;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ArduinoController
{
    public partial class MainWindow : Window
    {
        #region Left engine X and Y coordinates
        /// <summary> Y coordinate of the top of the left engine in Pod Racing mode </summary>
        double leftEngineTopY;

        /// <summary> Y coordinate of the center of the left engine in Pod Racing mode</summary>
        double leftEngineCenterY;

        /// <summary> Y coordinate of the bottom of the left engine in Pod Racing mode</summary>
        double leftEngineBottomY;

        /// <summary> X coordinate of the left engine in Pod Racing mode</summary>
        double leftEngineX;
        #endregion

        #region Right engine X and Y coordinates
        /// <summary> Y coordinate of the top of the right engine in Pod Racing mode </summary>
        double rightEngineTopY;

        /// <summary> Y coordinate of the center of the right engine in Pod Racing mode </summary>
        double rightEngineCenterY;

        /// <summary> Y coordinate of the bottom of the right engine in Pod Racing mode </summary>
        double rightEngineBottomY;

        /// <summary> X coordinate of the right engine in Pod Racing mode </summary>
        double rightEngineX;
        #endregion

        /// <summary> List of pod racing sliders </summary>
        private List<Slider> podRacingSliders;
        /// <summary> The pod racing mode background (an image) </summary>
        private ImageBrush podRacingBG;

        /// <summary>
        /// Initializes components and instance variables for Pod Racing Mode
        /// </summary>
        private void initPodRacingComponents() {
            bc = new BrushConverter();
            darkBlue = (Brush)bc.ConvertFrom("#FF001932");

            leftEngineTopY = Canvas.GetTop(leftEngine);
            leftEngineCenterY = leftEngineTopY + leftEngine.Height / 2;
            leftEngineBottomY = leftEngineTopY + leftEngine.Height;
            leftEngineX = Canvas.GetLeft(leftEngine) + leftEngine.Width;

            rightEngineTopY = Canvas.GetTop(rightEngine);
            rightEngineCenterY = rightEngineTopY + rightEngine.Height / 2;
            rightEngineBottomY = rightEngineTopY + rightEngine.Height;
            rightEngineX = Canvas.GetLeft(rightEngine) + rightEngine.Width;

            podRacingSliders = new List<Slider> { leftEngine, rightEngine };
            podRacingBG = new ImageBrush(new BitmapImage(new Uri(BaseUriHelper.GetBaseUri(this), "Resources/StarTours2.jpg")));
        }

        #region Main Behavior and Helpers
        /// <summary>
        /// The main Pod Racing Mode function:
        ///  - Updates the internal representation of the hands
        ///  - Tries to "lock in" or bind the left and right hands to the sliders in order to be ready to drive
        ///  - Signals the Arduino when the hands are bound and the Arduino is ready to be driven
        ///  - Signals the Arduino when to stop
        ///  - Responsible for updating the slider values
        ///  - 
        /// </summary>
        private void podRacingBehavior() {

            updateHandPositions();

            if (stopped) {
                
                // if R hand over right slider, try to bind the right hand to that slider
                if (!rhandBound && isHandInButton(_RhandX, _RhandY, rightEngineButton, true))
                    kinectHand.Hovering();
                else kinectHand.Release();

                // if L hand over left slider, try to bind the left hand to that slider
                if (!lhandBound && isHandInButton(_LhandX, _LhandY, leftEngineButton, false))
                    kinectHandL.Hovering();
                else kinectHandL.Release();

                // if both hands are bound, then start driving
                if (rhandBound && lhandBound)
                    StartMoving();
                else return;
            }
            // If Arduino is moving and one of the hands is in the stop area, stop driving
            else if (IsLHandOverObject(kinectHandL, stopButton) ||
                    IsRHandOverObject(kinectHand, stopButton)) {
                StopMoving();
                return;
            }

            // update the engine values and send those values to the Arduino
            updateLeftEngine();
            updateRightEngine();
            SendEngineValuesToArduino();
        }

        /// <summary>
        /// Sets the value of the left engine slider according to the position of the left hand.
        /// If the hand is below the slider, then its value is 0.
        /// If the hand is above the slider, then its value is the max.
        /// If the hand is in the same Y range of the slider, then its value is the hand's relative positions
        /// </summary>
        private void updateLeftEngine() {
            if (_LhandY > leftEngineBottomY) leftEngine.Value = 0;
            else if (_LhandY < leftEngineTopY) leftEngine.Value = leftEngine.Maximum;
            else {
                double dist = (leftEngine.Height - (_LhandY - leftEngineTopY));
                leftEngine.Value = dist / leftEngine.Height * leftEngine.Maximum;
            }
        }

        /// <summary>
        /// Sets the value of the right engine slider according to the position of the right hand.
        /// If the hand is below the slider, then its value is 0.
        /// If the hand is above the slider, then its value is the max.
        /// If the hand is in the same Y range of the slider, then its value is the hand's relative positions
        /// </summary>
        private void updateRightEngine() {
            if (_RhandY > rightEngineBottomY) rightEngine.Value = 0;
            else if (_RhandY < rightEngineTopY) rightEngine.Value = rightEngine.Maximum;
            else {
                double dist = (rightEngine.Height - (_RhandY - rightEngineTopY));
                rightEngine.Value = dist / rightEngine.Height * rightEngine.Maximum;
            }
        }

        /// <summary>
        /// Scale the engine values and send them to the Arduino
        /// </summary>
        private void SendEngineValuesToArduino() {
            double leftSpeed = leftEngine.Value / leftEngine.Maximum * ArduinoMax;
            double rightSpeed = ArduinoMax - rightEngine.Value / rightEngine.Maximum * ArduinoMax;

            ArduinoSendLeftRight((float)leftSpeed, (float)rightSpeed);
        }
        #endregion

        #region Pod Racing Slider Helpers
        /// <summary>
        /// Event handler called when the left engine has been clicked
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The "clicked button" event</param>
        private void leftEngineButton_Clicked(object sender, RoutedEventArgs e) {
            lhandBound = true;
        }

        /// <summary>
        /// Event handler called when the right engine has been clicked
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The "clicked button" event</param>
        private void rightEngineButton_Clicked(object sender, RoutedEventArgs e) {
            rhandBound = true;
        }
        #endregion

        #region Turn On/Off Pod Racing Mode

        /// <summary>
        /// Adds fade in animations (that turn on Pod Racing Mode) to the given storyboard.
        /// </summary>
        /// <param name="storyboard">The storyboard on which the animations for fading out/in will be played</param>
        private void turnOnPodRacing(Storyboard storyboard) {
            stopped = true;
            rhandBound = false;
            lhandBound = false;

            kinectHandL.Visibility = System.Windows.Visibility.Visible;
            kinectHandL.IsEnabled = true;

            enginesImg.Visibility = System.Windows.Visibility.Visible;
            storyboard.Children.Add(newImageAnimation(enginesImg, 0.0, 1.0, fadeIn, fadeOut));

            stopButton.Opacity = 0.5;
            stopButton.Visibility = System.Windows.Visibility.Visible;
            stopButton.IsEnabled = true;
            storyboard.Children.Add(newAnimation(stopButton, 0, 0.5, fadeIn, fadeOut));

            foreach (Slider s in podRacingSliders) {
                s.Visibility = System.Windows.Visibility.Visible;
                s.IsEnabled = true;
                storyboard.Children.Add(newAnimation(s, 0, 0.5, fadeIn, fadeOut));
            }

            foreach (TextBox box in textBoxes) {
                box.Visibility = System.Windows.Visibility.Visible;
                storyboard.Children.Add(newAnimation(box, 0, 1.0, fadeIn, fadeOut));
            }

            foreach (Label label in labels) {
                label.Foreground = Brushes.Black;
                label.FontWeight = FontWeights.Bold;
                label.Visibility = System.Windows.Visibility.Visible;
                storyboard.Children.Add(newAnimation(label, 0, 1.0, fadeIn, fadeOut));
            }

            storyboard.Children.Add(newBackgroundAnimation(ViewerCanvas, 0, 1, fadeIn, fadeOut));
            shiftViewerTo(880, 200);

            mainCanvas.Opacity = 0.0;
            mainCanvas.Background = podRacingBG;
            storyboard.Children.Add(newBackgroundAnimation(mainCanvas, 0.0, 1.0, fadeIn, fadeOut));
            currentMode = Mode.POD;
        }

        /// <summary>
        /// Adds fade out animations (that turn off Pod Racing Mode) to the given storyboard.
        /// </summary>
        /// <param name="storyboard">The storyboard on which the animations for fading out/in will be played</param>
        private void turnOffPodRacing(Storyboard storyboard) {
            StopMoving();

            kinectHandL.Visibility = System.Windows.Visibility.Hidden;
            kinectHandL.IsEnabled = false;

            stopButton.IsEnabled = false;
            storyboard.Children.Add(newAnimation(stopButton, 0.5, 0, fadeOut, timeZero));

            storyboard.Children.Add(newImageAnimation(enginesImg, 1.0, 0.0, fadeOut, timeZero));

            foreach (Slider s in podRacingSliders) {
                s.IsEnabled = false;
                storyboard.Children.Add(newAnimation(s, 0.5, 0, fadeOut, timeZero));
            }

            foreach (TextBox box in textBoxes) {
                storyboard.Children.Add(newAnimation(box, 1.0, 0.0, fadeOut, timeZero));
            }

            foreach (Label label in labels) {
                label.Foreground = Brushes.White;
                label.FontWeight = FontWeights.Normal;
                storyboard.Children.Add(newAnimation(label, 1.0, 0.0, fadeOut, timeZero));
            }

            storyboard.Children.Add(newBackgroundAnimation(ViewerCanvas, 0, 0, fadeOut, timeZero));

            mainCanvas.Background = darkBlue;
            storyboard.Children.Add(newBackgroundAnimation(mainCanvas, 0.0, 1.0, fadeOut, timeZero));

        }
        #endregion
    }
}
