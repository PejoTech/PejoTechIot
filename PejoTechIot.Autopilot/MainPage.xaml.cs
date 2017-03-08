using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using AdafruitClassLibrary;
using PejoTechIot.Autopilot.Controllers;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PejoTechIot.Autopilot
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Gps _gps;

        static ServoController _servo;

        private Task _chacheCompassTask;

        private static readonly List<CompassRawDataModel> CompassData = new List<CompassRawDataModel>();
        private readonly CancellationTokenSource _compassTaskCancelToken = new CancellationTokenSource();
        private CancellationToken _compassTaskToken = CancellationToken.None;

        private readonly CancellationTokenSource _servoTaskCancelToken = new CancellationTokenSource();
        private CancellationToken _servoTaskToken = CancellationToken.None;

        public static Hmc5883L Compass { get; set; }

        public MainPage()
        {
            this.InitializeComponent();

            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _compassTaskToken = _compassTaskCancelToken.Token;

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

                _servo = new ServoController(5);
                await _servo.Connect();

                Test();

                //await ServoControlTask(_servo);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Error starting app: {0}", ex.Message));
            }
        }

        //private static async Task ServoControlTask(ServoController servo)
        //{

        //}

        private static void Test()
        {
            _servo.SetPosition(0).AllowTimeToMove(1000).Go();
            for (int i = 1; i < 180; i++)
            {
                //Log(string.Format("Moving servo to {0}", i));
                _servo.SetPosition(i).AllowTimeToMove(10).Go();
            }
        }

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
                Log("No I2C controllers are connected.");
            }

        }

        #endregion

        #region Buttons



        private void BtnTargetSpeedActivate_Click(object sender, RoutedEventArgs e)
        {
            Test();
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

        private void Log(string s)
        {
            TxtDebug.Text += String.Format("{0}\r\n", s);
            svDebug.ScrollToVerticalOffset(0);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _servo.Dispose();
        }
    }
}
