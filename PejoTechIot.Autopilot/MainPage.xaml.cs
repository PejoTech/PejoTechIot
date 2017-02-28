using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using AdafruitClassLibrary;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PejoTechIot.Autopilot
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Gps _gps;

        public MainPage()
        {
            this.InitializeComponent();

            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 10; i++)
            {
                TxtDebug.Text += "Lorem ipsum dolor sit amet \n\r";
            }

            TxtTargetSpeed.Text = "2.5";
            TxtToleranceKts.Text = "0.1";
            TxtToleranceSeconds.Text = "5";

            BtnTargetSpeedActivate.Click += BtnTargetSpeedActivate_Click;
            BtnTargetSpeedIncrease.Click += BtnTargetSpeedIncrease_Click;
            BtnTargetSpeedDecrease.Click += BtnTargetSpeedDecrease_Click;

            BtnToleranceDecreaseKts.Click += BtnToleranceDecreaseKts_Click;
            BtnToleranceIncreaseKts.Click += BtnToleranceIncreaseKts_Click;

            BtnToleranceDecreaseTime.Click += BtnToleranceDecreaseTime_Click;
            BtnToleranceIncreaseTime.Click += BtnToleranceIncreaseTime_Click;

            // Initialize Gps
            try
            {
                _gps = new Gps();

                _gps.RMCEvent += OnRmcEvent;
                _gps.GGAEvent += OnGgaEvent;

                StartGps();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Error starting app: {0}", ex.Message));
            }
        }

        private void Log(string s)
        {
            TxtDebug.Text += String.Format("{0}\r\n", s);
        }

        #region Buttons



        private void BtnTargetSpeedActivate_Click(object sender, RoutedEventArgs e)
        {
            Log("Activate");
        }

        private void BtnTargetSpeedIncrease_Click(object sender, RoutedEventArgs e)
        {
            var targetSpeed = double.Parse(TxtTargetSpeed.Text);
            targetSpeed += 0.1;
            TxtTargetSpeed.Text = targetSpeed.ToString(CultureInfo.InvariantCulture);

            Log("Target speed up 0.1 mph");
        }

        private void BtnTargetSpeedDecrease_Click(object sender, RoutedEventArgs e)
        {
            var targetSpeed = double.Parse(TxtTargetSpeed.Text);
            targetSpeed -= 0.1;
            TxtTargetSpeed.Text = targetSpeed.ToString(CultureInfo.InvariantCulture);

            Log("Target speed down 0.1 mph");
        }

        private void BtnToleranceDecreaseKts_Click(object sender, RoutedEventArgs e)
        {
            var tolerance = double.Parse(TxtToleranceKts.Text);
            tolerance -= 0.1;
            TxtToleranceKts.Text = tolerance.ToString(CultureInfo.InvariantCulture);

            Log("Tolerance speed down 0.1 mph");
        }

        private void BtnToleranceIncreaseKts_Click(object sender, RoutedEventArgs e)
        {
            var tolerance = double.Parse(TxtToleranceKts.Text);
            tolerance += 0.1;
            TxtToleranceKts.Text = tolerance.ToString(CultureInfo.InvariantCulture);

            Log("Tolerance speed up 0.1 mph");
        }

        private void BtnToleranceDecreaseTime_Click(object sender, RoutedEventArgs e)
        {
            var tolerance = double.Parse(TxtToleranceSeconds.Text);
            tolerance -= 0.1;
            TxtToleranceSeconds.Text = tolerance.ToString(CultureInfo.InvariantCulture);

            Log("Tolerance seconds -1");
        }

        private void BtnToleranceIncreaseTime_Click(object sender, RoutedEventArgs e)
        {
            var tolerance = double.Parse(TxtToleranceSeconds.Text);
            tolerance += 0.1;
            TxtToleranceSeconds.Text = tolerance.ToString(CultureInfo.InvariantCulture);

            Log("Tolerance seconds +1");
        }

        #endregion

        #region Gps

        private async void StartGps()
        {
            // see note below about changing baud rates.
            await _gps.ConnectToUARTAsync(9600);

            // To change the baud rate on the GPS:
            // 
            // First, connect at the currently-set baud rate (as above)
            // Then execute this section of code, substituting the desired baudrate
            // in the SetBaudrate and ConnectToUART commands.
            //  You can actually leave this code in place.  Once the baud rate is changed,
            //  the first ConnectToUART and the SetBaudRate commands will have no effect.
            //  The second ConnectToUART will become the operative function.
            // Leaving it in here if you change the default baud rate will allow your 
            //  program to recover in case power is lost to the GPS and it resets to factory defaults.
            //
            //if (gps.Connected)
            //{
            //    await gps.SetBaudRate(19200);
            //    gps.DisconnectFromUART();
            //    await gps.ConnectToUART(19200);
            //}

            if (_gps.Connected)
            {
                await _gps.SetSentencesReportingAsync(0, 1, 0, 1, 0, 0);
                await _gps.SetUpdateFrequencyAsync(1);  //1Hz.  Change to 5 for 5Hz. Change to 10 for 10Hz.  Change to 0.1 for 0.1Hz.
                _gps.StartReading();
            }
        }

        private void OnRmcEvent(object sender, Gps.GPSRMC rmc)
        {
            if (rmc.Valid)
            {
                TxtTime.Text = rmc.TimeStamp.ToString("HH:mm:ss.fff");
                TxtSpeed.Text = rmc.Speed.ToString();
                TxtCourse.Text = rmc.Course.ToString();
            }
            else
            {
                TxtTime.Text = "";
                TxtSpeed.Text = "";
                TxtCourse.Text = "";
            }
        }

        private void OnGgaEvent(object sender, Gps.GPSGGA gga)
        {
            TxtSattelites.Text = gga.Quality != Gps.GPSGGA.FixQuality.noFix ? gga.Satellites.ToString() : "";
        }

        #endregion
    }
}
