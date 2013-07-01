using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.Kinect;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ArduinoController
{
    public partial class MainWindow : Window
    {
        #region X and Y coordinates for FRSlider (forward/reverse slider; the vertical slider)
        /// <summary> Y coordinate for the top of FRSlider (forward/reverse slider; the vertical slider in Precision mode)</summary>
        private double FRSliderTopY;
        /// <summary> Y coordinate for the center of FRSlider (forward/reverse slider; the vertical slider in Precision mode)</summary>
        private double FRSliderCenterY;
        /// <summary> Y coordinate for the bottom of FRSlider (forward/reverse slider; the vertical slider in Precision mode)</summary>
        private double FRSliderBottomY;
        /// <summary> X coordinate for FRSlider (forward/reverse slider; the vertical slider in Precision mode)</summary>
        private double FRSliderX;
        #endregion

        #region X and Y coordinates for LRSlider (left/right slider; the horizontal slider)
        /// <summary> X coordinate for the left of LRSlider (left/right slider; the horizontal slider in Precision mode)</summary>
        private double LRSliderLeftX;
        /// <summary> X coordinate for the center of LRSlider (left/right slider; the horizontal slider in Precision mode)</summary>
        private double LRSliderCenterX;
        /// <summary> X coordinate for the right of LRSlider (left/right slider; the horizontal slider in Precision mode)</summary>
        private double LRSliderRightX;
        /// <summary> Y coordinate for LRSlider (left/right slider; the horizontal slider in Precision mode)</summary>
        private double LRSliderY;
        #endregion

        #region Lists of labels, images, and sliders for Pod Racing Mode
        /// <summary> List of labels used in Precision Mode </summary>
        private List<Label> precisionLabels;
        /// <summary> List of images used in Precision Mode </summary>
        private List<Image> precisionImages;
        /// <summary> List of sliders used in Precision Mode </summary>
        private List<Slider> precisionSliders;
        #endregion

        /// <summary>
        /// Initializes components and instance variables for Precision Mode
        /// </summary>
        private void initPrecisionComponents() {
            FRSliderTopY = Canvas.GetTop(FRslider);
            FRSliderCenterY = FRSliderTopY + FRslider.Height / 2;
            FRSliderBottomY = FRSliderTopY + FRslider.Height;
            FRSliderX = Canvas.GetLeft(FRslider) + FRslider.Width;

            LRSliderLeftX = Canvas.GetLeft(LRslider);
            LRSliderCenterX = LRSliderLeftX + LRslider.Width / 2;
            LRSliderRightX = LRSliderLeftX + LRslider.Width;
            LRSliderY = Canvas.GetTop(LRslider) + LRslider.Height / 2;

            precisionLabels = new List<Label> { FLabel, RLabel };
            precisionImages = new List<Image> { lImg, rImg };
            precisionSliders = new List<Slider> { FRslider, LRslider };
        }

        #region Main Behavior and Helpers

        /// <summary>
        /// The main Precision Mode function:
        ///  - Updates the internal representation of the hands
        ///  - Tries to "lock in" or bind the left and right hands to the sliders in order to be ready to drive
        ///  - Signals the Arduino when the hands are bound and the Arduino is ready to be driven
        ///  - Signals the Arduino when to stop
        ///  - Responsible for updating the slider values
        ///  - Transforms the speed and velocity data into servos data that is subsequently sent to the Arduino
        /// </summary>
        private void precisionBehavior() {

            updateHandPositions();

            if (stopped) {

                // if R hand over horizontal slider, try to bind the right hand to that slider
                if (!rhandBound && isHandInButton(_RhandX, _RhandY, LRsliderButton, true))
                    kinectHand.Hovering();
                else kinectHand.Release();

                // if L hand over vertical slider, try to bind the left hand to that slider
                if (!lhandBound && isHandInButton(_LhandX, _LhandY, FRsliderButton, false))
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

            // update the sliders and send the speed and direction to the Arduino
            updateFRslider();
            updateLRslider();
            double speed = FRslider.Value;
            double direction = LRslider.Value;
            TransformAndSendVelocity(speed, direction);
        }

        /// <summary>
        /// Sets the value of the forward/reverse slider according to the position of the left hand.
        /// If the hand is below the slider, then its value is 0.
        /// If the hand is above the slider, then its value is the max.
        /// If the hand is in the same Y range of the slider, then its value is the hand's relative positions
        /// </summary>
        private void updateFRslider() {
            if (_LhandY > FRSliderBottomY) FRslider.Value = 0;
            else if (_LhandY < FRSliderTopY) FRslider.Value = FRslider.Maximum;
            else {
                double dist = (FRslider.Height - (_LhandY - FRSliderTopY));
                FRslider.Value = dist / FRslider.Height * FRslider.Maximum;
            }
        }

        /// <summary>
        /// Sets the value of the left/right slider according to the position of the right hand.
        /// If the hand is to the left of the slider, then its value is 0.
        /// If the hand is to the right of the slider, then its value is the max.
        /// If the hand is in the same X range of the slider, then its value is the hand's relative positions
        /// </summary>
        private void updateLRslider() {
            if (_RhandX > LRSliderRightX) LRslider.Value = LRslider.Maximum;
            else if (_RhandX < LRSliderLeftX) LRslider.Value = 0;
            else {
                double dist = (_RhandX - LRSliderLeftX);
                LRslider.Value = dist / LRslider.Width * LRslider.Maximum;
            }
        }

        /// <summary>
        /// Transforms the speed and direction values into left wheel and right wheel values,
        /// which are then sent to the Arduino.
        /// </summary>
        /// <param name="speed">The speed of the car</param>
        /// <param name="dir">The direction of the car</param>
        private void TransformAndSendVelocity(double speed, double dir) {
            double rwheelvalue = ArduinoRfwd;
            double lwheelvalue = ArduinoLfwd;
            double horizMid = LRslider.Maximum / 2;
            double vertMid = FRslider.Maximum / 2;

            // turn right if the horizontal slider's value is on the right; do this by slowing down the right engine
            if (dir > horizMid) {
                rwheelvalue = (dir - horizMid) / horizMid * 180;
                lwheelvalue = 180;
            }
            // turn left if the horizontal slider's value is on the left; do this by slowing down the left engine
            else if (dir < horizMid) {
                lwheelvalue = dir / horizMid * 180;
                rwheelvalue = 0;
            }
            // default is to go straight

            /*
             *  How much the velocity should be scaled according to the vertical slider's value?
             *  Essentially, the scale factor is the absolute value of the difference between the 
             *  current value and half the max value of the slider, divided by half the max value of
             *  the slider. Why half the max? Because the halves correspond to different directions,
             *  so scaling speed should be considered in the context of only 1 half.
             */
            double scale = 0;

            // reverse direction relatively if the vertical slider's value is in the bottom half
            if (speed < vertMid) {
                lwheelvalue = 180 - lwheelvalue;
                rwheelvalue = 180 - rwheelvalue;
                scale = (vertMid - speed) / vertMid;
            }
            // forward direction relatively if the vertical slider's value is in the top half
            else if (speed > vertMid)
                scale = (speed - vertMid) / vertMid;
            // do not move if the vertical slider is right in the middle
            else {
                rwheelvalue = 90;
                lwheelvalue = 90;
            }

            // scale and send the servos data
            rwheelvalue = scaleWheelVal(rwheelvalue, scale);
            lwheelvalue = scaleWheelVal(lwheelvalue, scale);
            ArduinoSendLeftRight((float)lwheelvalue, (float)rwheelvalue);
        }

        /// <summary>
        /// Scales the speed of the wheel according to the scale value calculated by another method <see cref="TransformAndSendVelocity"/>.
        /// </summary>
        /// <param name="wheelVal"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        private double scaleWheelVal(double wheelVal, double scale) {
            if (wheelVal > ArduinoStop)
                wheelVal = ArduinoStop + scale * (wheelVal - ArduinoStop);
            else if (wheelVal < ArduinoStop)
                wheelVal = ArduinoStop - scale * (ArduinoStop - wheelVal);
            return wheelVal;
        }
        #endregion

        #region Precision Slider Helpers

        /// <summary>
        /// Event handler called when the LRslider has been clicked
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The "clicked button" event</param>
        private void LRsliderButton_Clicked(object sender, RoutedEventArgs e) {
            rhandBound = true;
            Console.Out.WriteLine("lrSliderButton clicked");
        }

        /// <summary>
        /// Event handler called when the FRslider has been clicked
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The "clicked button" event</param>
        private void FRsliderButton_Clicked(object sender, RoutedEventArgs e) {
            lhandBound = true;
            Console.Out.WriteLine("frSliderButton clicked");
        }
        #endregion

        #region Turn On/Off Precision Mode
        /// <summary>
        /// Adds fade in animations (that turn on Precision Mode) to the given storyboard.
        /// </summary>
        /// <param name="storyboard">The storyboard on which the animations for fading out/in will be played</param>
        private void turnOnPrecision(Storyboard storyboard) {
            stopped = true;
            rhandBound = false;
            lhandBound = false;

            stopButton.Opacity = 0.0;
            stopButton.Visibility = System.Windows.Visibility.Visible;
            stopButton.IsEnabled = true;
            storyboard.Children.Add(newAnimation(stopButton, 0, 1.0, fadeIn, fadeOut));

            kinectHandL.Visibility = System.Windows.Visibility.Visible;
            kinectHandL.IsEnabled = true;

            foreach (Label label in precisionLabels) {
                FLabel.Visibility = System.Windows.Visibility.Visible;
                storyboard.Children.Add(newAnimation(label, 0.0, 1.0, fadeIn, fadeOut));
            }

            foreach (Image img in precisionImages) {
                img.Visibility = System.Windows.Visibility.Visible;
                storyboard.Children.Add(newImageAnimation(img, 0, 1.0, fadeIn, fadeOut));
            }

            foreach (Slider s in precisionSliders) {
                s.Visibility = System.Windows.Visibility.Visible;
                storyboard.Children.Add(newAnimation(s, 0, 1.0, fadeIn, fadeOut));
            }

            foreach (TextBox box in textBoxes) {
                box.Visibility = System.Windows.Visibility.Visible;
                storyboard.Children.Add(newAnimation(box, 0, 1.0, fadeIn, fadeOut));
            }

            foreach (Label label in labels) {
                label.Visibility = System.Windows.Visibility.Visible;
                storyboard.Children.Add(newAnimation(label, 0, 1.0, fadeIn, fadeOut));
            }

            storyboard.Children.Add(newBackgroundAnimation(ViewerCanvas, 0, 1, fadeIn, fadeOut));
            shiftViewerTo(880, 200);
            storyboard.Children.Add(newBackgroundAnimation(mainCanvas, 0.0, 1.0, fadeIn, fadeOut));
            currentMode = Mode.PRECISION;
        }

        /// <summary>
        /// Adds fade out animations (that turn off Precision Mode) to the given storyboard.
        /// </summary>
        /// <param name="storyboard">The storyboard on which the animations for fading out/in will be played</param>
        private void turnOffPrecision(Storyboard storyboard) {
            StopMoving();

            stopButton.IsEnabled = false;
            storyboard.Children.Add(newAnimation(stopButton, 1.0, 0, fadeOut, timeZero));

            kinectHandL.Visibility = System.Windows.Visibility.Hidden;
            kinectHandL.IsEnabled = false;

            foreach (Label label in precisionLabels)
                storyboard.Children.Add(newAnimation(label, 1.0, 0.0, fadeOut, timeZero));

            foreach (Image img in precisionImages)
                storyboard.Children.Add(newImageAnimation(img, 1.0, 0.0, fadeOut, timeZero));

            foreach (Slider s in precisionSliders)
                storyboard.Children.Add(newAnimation(s, 1.0, 0.0, fadeOut, timeZero));

            foreach (TextBox box in textBoxes)
                storyboard.Children.Add(newAnimation(box, 1.0, 0.0, fadeOut, timeZero));

            foreach (Label label in labels)
                storyboard.Children.Add(newAnimation(label, 1.0, 0.0, fadeOut, timeZero));

            storyboard.Children.Add(newBackgroundAnimation(ViewerCanvas, 0, 0, fadeOut, timeZero));
            storyboard.Children.Add(newBackgroundAnimation(mainCanvas, 0, 0, fadeOut, timeZero));
        }
        #endregion

    }
}
