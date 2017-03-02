using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using AdafruitClassLibrary;
using PejoTechIot.Autopilot.Drivers;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PejoTechIot.Autopilot
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Gps _gps;
        private Task _chacheCompassTask;
        private static List<CompassRawDataModel> _compassData = new List<CompassRawDataModel>();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private CancellationToken _token = CancellationToken.None;

        public static Hmc5883L Compass { get; set; }

        public MainPage()
        {
            this.InitializeComponent();

            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _token = _cancellationTokenSource.Token;

            for (int i = 0; i < 10; i++)
            {
                Log("Lorem ipsum dolor sit amet \n\r");
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
                StartGps();

                StartCompass();

                if (Compass.IsConnected())
                {
                    _chacheCompassTask = Task.Run(() =>
                    {
                        while (true)
                        {
                            _compassData.Add(Compass.GetRawData());

                            if (_token.IsCancellationRequested)
                            {
                                _token.ThrowIfCancellationRequested();
                            }
                        }
                    }, _token);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Error starting app: {0}", ex.Message));
            }
        }

        private async void StartCompass()
        {

            string advancedQueryString = I2cDevice.GetDeviceSelector();
            var deviceInformations = await DeviceInformation.FindAllAsync(advancedQueryString);

            if (deviceInformations.Any())
            {
                Compass = new Hmc5883L();
                await Compass.Initialize();

                if (Compass.IsConnected())
                {
                    Compass.SetOperatingMode(Hmc5884LOperatingMode.ContinuousOperatingMode);
                }
            }
            else
            {
                Log("No I2C controllers are connected.");
            }

            Compass = new Hmc5883L();
            await Compass.Initialize();

            if (Compass.IsConnected())
            {
                Compass.SetOperatingMode(Hmc5884LOperatingMode.ContinuousOperatingMode);
            }

        }

        private static void CacheCompassData()
        {
            //await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
            //    CoreDispatcherPriority.Normal, () =>
            //    {
            //        TxtHeading.Text = ((180 * Math.Atan2(direction.Y, direction.X) / Math.PI) + 2.04).ToString(CultureInfo.InvariantCulture);
            //    });
        }

        private void Log(string s)
        {
            TxtDebug.Text += String.Format("{0}\r\n", s);
            svDebug.ScrollToVerticalOffset(0);
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
            tolerance -= 1;
            TxtToleranceSeconds.Text = tolerance.ToString(CultureInfo.InvariantCulture);

            Log("Tolerance seconds -1");
        }

        private void BtnToleranceIncreaseTime_Click(object sender, RoutedEventArgs e)
        {
            var tolerance = double.Parse(TxtToleranceSeconds.Text);
            tolerance += 1;
            TxtToleranceSeconds.Text = tolerance.ToString(CultureInfo.InvariantCulture);

            Log("Tolerance seconds +1");
        }

        #endregion

        #region Gps

        private async void StartGps()
        {
            _gps = new Gps();

            _gps.RMCEvent += OnRmcEvent;
            _gps.GGAEvent += OnGgaEvent;

            await _gps.ConnectToUARTAsync(9600);

            if (_gps.Connected)
            {
                await _gps.SetSentencesReportingAsync(0, 1, 0, 1, 0, 0);
                await _gps.SetUpdateFrequencyAsync(1);  //1Hz.  Change to 5 for 5Hz. Change to 10 for 10Hz.  Change to 0.1 for 0.1Hz.
                _gps.StartReading();
            }
        }

        private void OnRmcEvent(object sender, Gps.GPSRMC rmc)
        {
            if (!rmc.Valid) return;

            TxtTime.Text = rmc.TimeStamp.ToString("HH:mm:ss.fff");
            TxtSpeed.Text = rmc.Speed.ToString();
            TxtCourse.Text = rmc.Course.ToString();
        }

        private void OnGgaEvent(object sender, Gps.GPSGGA gga)
        {
            if (gga.Quality == Gps.GPSGGA.FixQuality.noFix) return;

            TxtSattelites.Text = gga.Satellites.ToString();
        }

        #endregion
    }
}
