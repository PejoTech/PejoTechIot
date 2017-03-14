using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using AdafruitClassLibrary;
using PejoTechIot.Autopilot.Controllers;

namespace PejoTechIot.Autopilot
{
    public sealed partial class MainPage : Page
    {
        public Gps Gps { get; set; }

        public ServoController ServoController { get; set; }

        public static Hmc5883L Compass { get; set; }

        public double Course { get; set; }

        public double Speed { get; set; }

        public List<double> SpeedList { get; set; }

        public DateTime Time { get; set; }

        public int Sattelites { get; set; }

        public double TargetSpeed { get; set; }

        public double ToleranceSpeed { get; set; }

        public double ToleranceSeconds { get; set; }

        public int ServoPosition { get; set; }

        public bool Activated { get; set; }

        public bool UpdatingUi { get; set; } = true;

        public bool SpeedAverage { get; set; }

        public MainPage()
        {
            this.InitializeComponent();

            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;

            SpeedList = new List<double>();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ServoPosition = 0;

            TargetSpeed = 2.5d;
            ToleranceSpeed = 0.1d;
            ToleranceSeconds = 1.0d;

            BtnTargetSpeedActivate.Click += BtnTargetSpeedActivate_Click;
            BtnTargetSpeedIncrease.Click += BtnTargetSpeedIncrease_Click;
            BtnTargetSpeedDecrease.Click += BtnTargetSpeedDecrease_Click;

            BtnToleranceDecreaseKmh.Click += BtnToleranceDecreaseKmh_Click;
            BtnToleranceIncreaseKmh.Click += BtnToleranceIncreaseKmh_Click;

            BtnTimeFactorDecrease.Click += BtnTimeFactorDecrease_Click;
            BtnTimeFactorIncrease.Click += BtnTimeFactorIncrease_Click;

            BtnTest.Click += BtnTest_Click;

            TxtDebug.TextChanged += TxtDebug_TextChanged;

            ChbSpeedAverage.Checked += ChbSpeedAverage_Changed;
            ChbSpeedAverage.Unchecked += ChbSpeedAverage_Changed;

            // Initialize Gps
            try
            {
                StartGps();

                #region Compass Initialize

                // TODO: Check I2C connection of RPi
                //StartCompass();

                //if (Compass.IsConnected())
                //{
                //    _chacheCompassTask = Task.Run(() =>
                //    {
                //        while (true)
                //        {
                //            CompassData.Add(Compass.GetRawData());

                //            if (_compassTaskToken.IsCancellationRequested)
                //            {
                //                _compassTaskToken.ThrowIfCancellationRequested();
                //            }
                //        }
                //    }, _compassTaskToken);
                //}

                #endregion

                #region ServoController

                ServoController = new ServoController(5);
                await ServoController.Connect();
                ServoController.SetPosition(ServoPosition).AllowTimeToMove(2000).Go();

                await Task.Run(async () =>
                {
                    while (UpdatingUi)
                    {
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                           CoreDispatcherPriority.Normal, () =>
                           {
                               UpdateUi();
                           });

                        Task.Delay(2000).Wait();
                    }
                });

                #endregion
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Error starting app: {0}", ex.Message));
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            ServoController.Dispose();

            Activated = false;
            UpdatingUi = false;
        }

        #region ServoController

        private void ServoControlTask()
        {
            while (Activated)
            {
                var speed = SpeedAverage && SpeedList.Any() ? SpeedList.Average() : Speed;
                SpeedList.Clear();

                var diff = TargetSpeed - speed;

                if (Math.Abs(diff) > ToleranceSpeed && speed > 0)
                {
                    if (diff < 0.0d)
                    {
                        ServoPosition -= 1;
                        LogAsync(string.Format("{0} km/h off; decreasing by 1 degree to {1}", diff, ServoPosition));
                    }
                    if (diff > 0.0d && ServoPosition >= 0)
                    {
                        ServoPosition += 1;
                        LogAsync(string.Format("{0} km/h off; increasing by 1 degree to {1}", diff, ServoPosition));
                    }

                    ServoController.SetPosition(ServoPosition).AllowTimeToMove(100).Go();
                }
                else
                {
                    LogAsync(string.Format("Speed of {0} withing tolerance or 0; doing nothing", speed));
                }

                Task.Delay((int)(ToleranceSeconds * 1000)).Wait();
            }

            LogAsync("Speed control loop ended. Setting servo back to 0 degree");

            ServoPosition = 0;
            ServoController.SetPosition(0).AllowTimeToMove(2000).Go();
        }

        #endregion

        #region Compass

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
                LogAsync("No I2C controllers are connected.");
            }
        }

        #endregion

        #region Controls

        private async void BtnTargetSpeedActivate_Click(object sender, RoutedEventArgs e)
        {
            if (Activated)
            {
                Activated = false;
                LogAsync("Deactivated");
            }
            else
            {
                Activated = true;
                LogAsync("Activated");

                await Task.Run(() => ServoControlTask());
            }
        }

        private void BtnTargetSpeedIncrease_Click(object sender, RoutedEventArgs e)
        {
            var targetSpeed = double.Parse(TxtTargetSpeed.Text);
            targetSpeed += 0.1;
            TargetSpeed = targetSpeed;
            LogAsync("Target speed up 0.1 mph");

            UpdateUi();
        }

        private void BtnTargetSpeedDecrease_Click(object sender, RoutedEventArgs e)
        {
            var targetSpeed = double.Parse(TxtTargetSpeed.Text);
            targetSpeed -= 0.1;
            TargetSpeed = targetSpeed;
            LogAsync("Target speed down 0.1 mph");

            UpdateUi();
        }

        private void BtnToleranceDecreaseKmh_Click(object sender, RoutedEventArgs e)
        {
            var tolerance = double.Parse(TxtToleranceKmh.Text);
            tolerance -= 0.1;
            ToleranceSpeed = tolerance;
            LogAsync("ToleranceSpeed speed down 0.1 mph");

            UpdateUi();
        }

        private void BtnToleranceIncreaseKmh_Click(object sender, RoutedEventArgs e)
        {
            var tolerance = double.Parse(TxtToleranceKmh.Text);
            tolerance += 0.1;
            ToleranceSpeed = tolerance;
            LogAsync("ToleranceSpeed speed up 0.1 mph");

            UpdateUi();
        }

        private void BtnTimeFactorDecrease_Click(object sender, RoutedEventArgs e)
        {
            var tolerance = double.Parse(TxtTimeFactor.Text);
            tolerance -= 1;
            ToleranceSeconds = tolerance;
            LogAsync("ToleranceSpeed seconds -1");

            UpdateUi();
        }

        private void BtnTimeFactorIncrease_Click(object sender, RoutedEventArgs e)
        {
            var tolerance = double.Parse(TxtTimeFactor.Text);
            tolerance += 1;
            ToleranceSeconds = tolerance;
            LogAsync("ToleranceSpeed seconds +1");

            UpdateUi();
        }

        private void TxtDebug_TextChanged(object sender, TextChangedEventArgs e)
        {
            var grid = (Grid)VisualTreeHelper.GetChild(TxtDebug, 0);
            for (var i = 0; i <= VisualTreeHelper.GetChildrenCount(grid) - 1; i++)
            {
                object obj = VisualTreeHelper.GetChild(grid, i);
                if (!(obj is ScrollViewer)) continue;
                ((ScrollViewer)obj).ChangeView(0.0f, ((ScrollViewer)obj).ExtentHeight, 1.0f);
                break;
            }
        }

        private void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ServoTest));
        }

        private void ChbSpeedAverage_Changed(object sender, RoutedEventArgs e)
        {
            SpeedAverage = ChbSpeedAverage.IsChecked ?? false;
            LogAsync(SpeedAverage ? "Measuring average speed" : "Measuring speed of moment");
        }

        #endregion

        #region Gps

        private async void StartGps()
        {
            Gps = new Gps();

            Gps.RMCEvent += OnRmcEvent;
            Gps.GGAEvent += OnGgaEvent;

            await Gps.ConnectToUARTAsync(9600);

            if (Gps.Connected)
            {
                await Gps.SetSentencesReportingAsync(0, 1, 0, 1, 0, 0);
                await Gps.SetUpdateFrequencyAsync(10); //1Hz.  Change to 5 for 5Hz. Change to 10 for 10Hz.  Change to 0.1 for 0.1Hz.
                Gps.StartReading();
            }
        }

        private void OnRmcEvent(object sender, Gps.GPSRMC rmc)
        {
            if (!rmc.Valid || rmc.Speed == null || rmc.Course == null)
            {
                return;
            }

            //TODO: Maybe too much ui updates
            Time = rmc.TimeStamp;
            Speed = rmc.Speed.Value * 1.852d;
            SpeedList.Add(rmc.Speed.Value);
            Course = rmc.Course.Value;
        }

        private void OnGgaEvent(object sender, Gps.GPSGGA gga)
        {
            if (gga.Quality == Gps.GPSGGA.FixQuality.noFix || gga.Satellites == null)
            {
                return;
            }

            Sattelites = gga.Satellites.Value;
        }

        #endregion

        private void UpdateUi()
        {
            TxtCourse.Text = Course.ToString(CultureInfo.InvariantCulture);
            TxtSpeed.Text = SpeedAverage && SpeedList.Any()
                ? SpeedList.Average().ToString(CultureInfo.InvariantCulture)
                : Speed.ToString(CultureInfo.InvariantCulture);
            TxtTime.Text = Time.ToString("HH:mm:ss");
            TxtSattelites.Text = Sattelites.ToString(CultureInfo.InvariantCulture);
            TxtTargetSpeed.Text = TargetSpeed.ToString(CultureInfo.InvariantCulture);
            TxtToleranceKmh.Text = ToleranceSpeed.ToString(CultureInfo.InvariantCulture);
            TxtTimeFactor.Text = ToleranceSeconds.ToString(CultureInfo.InvariantCulture);
        }

        private async void LogAsync(string s)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () =>
                {
                    TxtDebug.Text += string.Format("{0}\r\n", s);
                });
        }
    }
}