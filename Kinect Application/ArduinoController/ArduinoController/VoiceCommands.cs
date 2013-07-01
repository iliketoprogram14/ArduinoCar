using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using System.Windows;

namespace ArduinoController
{
    /// <summary>
    /// From Kinect Speech Demo.
    /// A class that parses voice commands and calls the appropriate 
    /// function upon recognizing some command.
    /// </summary>
    class VoiceCommands
    {
        /// <summary> Kinect's audio source </summary>
        private KinectAudioSource audioSrc;
        /// <summary> Information about the recognizer. In this case, that information is about the US version of the English language. </summary>
        private RecognizerInfo ri;
        /// <summary> The speech recognition engine that does the actual speech recognition</summary>
        private SpeechRecognitionEngine recognizer;
        /// <summary> A reference to the main window </summary>
        private MainWindow window;

        public VoiceCommands(KinectSensor sensor, MainWindow w) {
            window = w;
            audioSrc = sensor.AudioSource;
            audioSrc.EchoCancellationMode = EchoCancellationMode.None;
            audioSrc.AutomaticGainControlEnabled = false;

            ri = GetKinectRecognizer();

            if (ri == null) {
                Console.Out.WriteLine("Could not find Kinect speech recognizer.");
                return;
            }

            // Need to wait > 4 seconds for device to be ready right after initialization
            int wait = 5;
            while (wait > 0) {
                Console.Out.WriteLine("Kinect will be ready for speech recognition in {0} second(s).", wait--);
                Thread.Sleep(1000);
            }

            loadGrammarAndWords();
        }

        #region Voice Recognizer Constructor Helpers
        /// <summary>
        /// Gets the English-US recognizer info
        /// </summary>
        /// <returns>The English-US recognizer info</returns>
        private RecognizerInfo GetKinectRecognizer() {
            Func<RecognizerInfo, bool> matchingFunc = r => {
                string value;
                r.AdditionalInfo.TryGetValue("Kinect", out value);
                return "True".Equals(value, StringComparison.InvariantCultureIgnoreCase) && "en-US".Equals(r.Culture.Name, StringComparison.InvariantCultureIgnoreCase);
            };
            return SpeechRecognitionEngine.InstalledRecognizers().Where(matchingFunc).FirstOrDefault();
        }

        /// <summary>
        /// Loads the recognized commands into the speech recognizer
        /// </summary>
        private void loadGrammarAndWords() {
            recognizer = new SpeechRecognitionEngine(ri.Id);
            Choices commands = new Choices();
            commands.Add("Kinect menu");
            commands.Add("Kinect steering");
            commands.Add("Kinect pod racing");
            commands.Add("Kinect precision");

            var gb = new GrammarBuilder { Culture = ri.Culture };

            // Specify the culture to match the recognizer in case we are running in a different culture.                                 
            gb.Append(commands);

            // Create the actual Grammar instance, and then load it into the speech recognizer.
            var g = new Grammar(gb);
            recognizer.LoadGrammar(g);
            recognizer.SpeechRecognized += SpeechRecognized;
        }
        #endregion

        #region Interface to MainWindow
        /// <summary>
        /// Starts listening to the Kinect's audio stream and starts the recognizer asynchronously
        /// </summary>
        public void startRecognizer() {
            // start listening to the user and recognize events asynchronously
            Stream s = audioSrc.Start();
            recognizer.SetInputToAudioStream(s, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
            recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }

        /// <summary>
        /// (Re)starts the recognizer asynchronously
        /// </summary>
        public void restartRecognizer() {
            recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }

        /// <summary>
        /// Cancels the recognizer asynchronously (as soon as possible)
        /// </summary>
        public void stopRecognizer() {
            recognizer.RecognizeAsyncCancel();
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Event handler called when some speech is recognized.
        /// Delegates behavior to the appropriate function/event handler depending on the recognized speech.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The "clicked button" event</param>
        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e) {
            if (e.Result.Confidence >= 0.7) {
                Console.Out.WriteLine("Speech Recognized: \t{0}\tConfidence:\t{1}", e.Result.Text, e.Result.Confidence);
                RoutedEventArgs re = new RoutedEventArgs();
                switch (e.Result.Text) {
                    case "Kinect menu":
                        window.menuButton_Clicked((object)this, re);
                        break;
                    case "Kinect steering":
                        window.steeringButton_Clicked((object)this, re);
                        break;
                    case "Kinect precision":
                        window.precisionButton_Clicked((object)this, re);
                        break;
                    case "Kinect pod racing":
                        window.podRacingButton_Clicked((object)this, re);
                        break;
                }
            }
            else {
                Console.Out.WriteLine("Speech Recognized but confidence was too low: \t{0}", e.Result.Confidence);
            }
        }
        #endregion

    }
}
