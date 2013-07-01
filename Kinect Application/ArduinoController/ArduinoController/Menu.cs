using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Coding4Fun.Kinect.Wpf;
using System.Windows.Media.Animation;

namespace ArduinoController
{

    public partial class MainWindow : Window
    {
        /// <summary> Menu's representation of the right hand's X coordinate</summary>
        private static double _handX;

        /// <summary> Menu's representation of the right hand's Y coordinate</summary>
        private static double _handY;

        /// <summary> List of the menu buttons</summary>
        private List<Button> menuButtons;

        #region Menu Events
        /// <summary>
        /// Event handler called whenever a user tries to navigate to Precision mode.
        /// </summary>
        /// <param name="sender">Where the event came from</param>
        /// <param name="e">The clicked button event</param>
        public void precisionButton_Clicked(object sender, RoutedEventArgs e) {
            Storyboard storyboard = new Storyboard();
            turnOffCurrentCanvas(storyboard);
            turnOnPrecision(storyboard);
            storyboard.Begin(mainCanvas);
        }

        /// <summary>
        /// Event handler called whenever a user tries to navigate to Steering mode.
        /// </summary>
        /// <param name="sender">Where the event came from</param>
        /// <param name="e">The clicked button event</param>
        public void steeringButton_Clicked(object sender, RoutedEventArgs e) {
            Storyboard storyboard = new Storyboard();
            turnOffCurrentCanvas(storyboard);
            turnOnSteering(storyboard);
            storyboard.Begin(mainCanvas);
        }

        /// <summary>
        /// Event handler called whenever a user tries to navigate to Pod Racing mode.
        /// </summary>
        /// <param name="sender">Where the event came from</param>
        /// <param name="e">The clicked button event</param>
        public void podRacingButton_Clicked(object sender, RoutedEventArgs e) {
            Storyboard storyboard = new Storyboard();
            turnOffCurrentCanvas(storyboard);
            turnOnPodRacing(storyboard);
            storyboard.Begin(mainCanvas);
        }

        /// <summary>
        /// Event handler called whenever a user tries to navigate to the Menu.
        /// </summary>
        /// <param name="sender">Where the event came from</param>
        /// <param name="e">The clicked button event</param>
        public void menuButton_Clicked(object sender, RoutedEventArgs e) {
            Storyboard storyboard = new Storyboard();
            turnOffCurrentCanvas(storyboard);
            turnOnMenu(storyboard);
            storyboard.Begin(mainCanvas);
        }
        #endregion

        #region Turn On/Off Menu
        /// <summary>
        /// Adds fade in animations for turning on the menu to the given storyboard.  Used when switching modes.
        /// </summary>
        /// <param name="storyboard">The storyboard on which the animations for fading out/in will be played</param>
        private void turnOnMenu(Storyboard storyboard) {
            foreach (Button b in menuButtons) {
                b.IsEnabled = true;
                storyboard.Children.Add(newAnimation(b, 0, 1, fadeIn, fadeOut));
            }
            currentMode = Mode.MENU;
            storyboard.Children.Add(newAnimation(menuTitle, 0, 1, fadeIn, fadeOut));

            storyboard.Children.Add(newBackgroundAnimation(ViewerCanvas, 0, 1, fadeIn, fadeOut));
            storyboard.Children.Add(newBackgroundAnimation(mainCanvas, 0.0, 1.0, fadeIn, fadeOut));
            ViewerCanvas.Opacity = 0.0;
            shiftViewerTo(470, 410);
        }

        /// <summary>
        /// Adds fade out animations for turning off the menu to the given storyboard.  Used when switching modes.
        /// </summary>
        /// <param name="storyboard">The storyboard on which the animations for fading out/in will be played</param>
        private void turnOffMenu(Storyboard storyboard) {
            foreach (Button b in menuButtons) {
                b.IsEnabled = false;
                storyboard.Children.Add(newAnimation(b, 1, 0, fadeOut, timeZero));
            }
            storyboard.Children.Add(newAnimation(menuTitle, 1, 0, fadeOut, timeZero));
            storyboard.Children.Add(newBackgroundAnimation(mainCanvas, 0, 0, fadeOut, timeZero));
        }
        #endregion
    }
}
