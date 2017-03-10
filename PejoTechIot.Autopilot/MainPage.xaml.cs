using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private readonly CancellationToken _servoTaskCancellationToken = CancellationToken.None;

        private ObservableCollection<string> _debugList;

        public static Hmc5883L Compass { get; set; }
        
        public double Course { get; set; }

        public double Speed { get; set; }

        public DateTime Time { get; set; }

        public int Sattelites { get; set; }

        public double TargetSpeed { get; set; }

        public double ToleranceSpeed { get; set; }

        public double ToleranceSeconds { get; set; }

        public int ServoPosition { get; set; }

        public MainPage()
        {
            this.InitializeComponent();

            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;

            DebugList.SelectionChanged += DebugList_Changed;
            DebugList.Loaded += DebugList_Loaded;

            _debugList = new ObservableCollection<string>();
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

                #region Servo

                _servo = new ServoController(5);
                await _servo.Connect();

                Test();
                _servo.SetPosition(ServoPosition).AllowTimeToMove(500).Go();
                await Task.Run(() => ServoControlTask(), _servoTaskCancellationToken);

                #endregion
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Error starting app: {0}", ex.Message));
            }
        }

        private void ServoControlTask()
        {
            while (true)
            {
                var diff = TargetSpeed - Speed;
                if (Math.Abs(diff) > ToleranceSpeed)
                {
                    if (diff < 0.0d)
                    {
                        ServoPosition += 1;
                    }
                    if (diff > 0.0d)
                    {
                        ServoPosition -= 1;
                    }

                    _servo.SetPosition(ServoPosition).AllowTimeToMove(10).Go();
                }

                Task.Delay((int) ToleranceSeconds).Wait();
            }
        }

        private void Test()
        {
            _servo.SetPosition(0).AllowTimeToMove(1000).Go();
            for (int i = 1; i < 180; i++)
            {
                Log(string.Format("Moving servo to {0}", i));
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

        #region Controls

        
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _servo.Dispose();
        }

        private void BtnTargetSpeedActivate_Click(object sender, RoutedEventArgs e)
        {
            Test();
            Log("Activate");
        }

        private void BtnTargetSpeedIncrease_Click(object sender, RoutedEventArgs e)
        {
            var targetSpeed = double.Parse(TxtTargetSpeed.Text);
            targetSpeed += 0.1;
            TargetSpeed = targetSpeed;

            Log("Target speed up 0.1 mph");

            UpdateUi();
        }

        private void BtnTargetSpeedDecrease_Click(object sender, RoutedEventArgs e)
        {
            var targetSpeed = double.Parse(TxtTargetSpeed.Text);
            targetSpeed -= 0.1;
            TargetSpeed = targetSpeed;

            Log("Target speed down 0.1 mph");

            UpdateUi();
        }

        private void BtnToleranceDecreaseKts_Click(object sender, RoutedEventArgs e)
        {
            var tolerance = double.Parse(TxtToleranceKts.Text);
            tolerance -= 0.1;
            ToleranceSpeed = tolerance;

            Log("ToleranceSpeed speed down 0.1 mph");

            UpdateUi();
        }

        private void BtnToleranceIncreaseKts_Click(object sender, RoutedEventArgs e)
        {
            var tolerance = double.Parse(TxtToleranceKts.Text);
            tolerance += 0.1;
            ToleranceSpeed = tolerance;

            Log("ToleranceSpeed speed up 0.1 mph");

            UpdateUi();
        }

        private void BtnToleranceDecreaseTime_Click(object sender, RoutedEventArgs e)
        {
            var tolerance = double.Parse(TxtToleranceSeconds.Text);
            tolerance -= 1;
            ToleranceSeconds = tolerance;

            Log("ToleranceSpeed seconds -1");

            UpdateUi();
        }

        private void BtnToleranceIncreaseTime_Click(object sender, RoutedEventArgs e)
        {
            var tolerance = double.Parse(TxtToleranceSeconds.Text);
            tolerance += 1;
            ToleranceSeconds = tolerance;

            Log("ToleranceSpeed seconds +1");

            UpdateUi();
        }

        private void DebugList_Changed(object sender, SelectionChangedEventArgs e)
        {
            DebugListScrollToBottom();
        }

        private void DebugList_Loaded(object sender, RoutedEventArgs e)
        {
            DebugListScrollToBottom();
        }

        private void DebugListScrollToBottom()
        {
            if (DebugList.Items == null) return;

            var selectedIndex = DebugList.Items.Count - 1;

            if (selectedIndex < 0) return;

            DebugList.SelectedIndex = selectedIndex;

            DebugList.UpdateLayout();
            DebugList.ScrollIntoView(DebugList.SelectedItem);
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
                await _gps.SetUpdateFrequencyAsync(10);  //1Hz.  Change to 5 for 5Hz. Change to 10 for 10Hz.  Change to 0.1 for 0.1Hz.
                _gps.StartReading();
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
            Speed = rmc.Speed.Value;
            Course = rmc.Course.Value;

            UpdateUi();
        }

        private void OnGgaEvent(object sender, Gps.GPSGGA gga)
        {
            if (gga.Quality == Gps.GPSGGA.FixQuality.noFix || gga.Satellites == null)
            {
                return;
            }

            Sattelites = gga.Satellites.Value;

            UpdateUi();
        }

        #endregion

        private void UpdateUi()
        {
            TxtCourse.Text = Course.ToString(CultureInfo.InvariantCulture);
            TxtSpeed.Text = Speed.ToString(CultureInfo.InvariantCulture); ;
            TxtTime.Text = Time.ToString("HH:mm:ss.fff");
            TxtSattelites.Text = Sattelites.ToString(CultureInfo.InvariantCulture); ;

            TxtTargetSpeed.Text = TargetSpeed.ToString(CultureInfo.InvariantCulture);
            TxtToleranceKts.Text = ToleranceSpeed.ToString(CultureInfo.InvariantCulture);
            TxtToleranceSeconds.Text = ToleranceSeconds.ToString(CultureInfo.InvariantCulture);
        }

        private void Log(string s)
        {
            _debugList.Add(s);
            DebugList.ItemsSource = _debugList.ToList();
        }
    }
}
