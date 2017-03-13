using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using PejoTechIot.Autopilot.Controllers;

namespace PejoTechIot.Autopilot
{
    public partial class ServoTest : Page
    {
        private ServoController _servo;

        public int ServoPosition { get; set; } = 0;

        public double ToleranceSpeed { get; set; } = 0.1d;

        public double Speed { get; set; } = 0;

        public double TargetSpeed { get; set; } = 2.5d;

        public bool Activated { get; set; }

        public ServoTest()
        {
            this.InitializeComponent();

            Loaded += Page_Loaded;
            Unloaded += Page_Unoaded;

            BtnActivate.Click += BtnActivate_Click;
            BtnTest.Click += BtnTest_Click;
            SpeedTestSlider.ValueChanged += SpeedTestSlider_Changed;
            ServoPositionSlider.ValueChanged += ServoPositionSlider_Changed;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateUi();

            BtnTargetSpeedIncrease.Click += BtnTargetSpeedIncrease_Click;
            BtnTargetSpeedDecrease.Click += BtnTargetSpeedDecrease_Click;

            BtnToleranceDecreaseKts.Click += BtnToleranceDecreaseKts_Click;
            BtnToleranceIncreaseKts.Click += BtnToleranceIncreaseKts_Click;

            _servo = new ServoController(5);
            await _servo.Connect();

            _servo.SetPosition(ServoPosition).AllowTimeToMove(2000).Go();
        }

        private void Page_Unoaded(object sender, RoutedEventArgs e)
        {
            _servo.Dispose();
        }

        private void ServoPositionSlider_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            _servo.SetPosition((int)e.NewValue).AllowTimeToMove(100).Go();
            ServoPosition = (int)e.NewValue;
            TxtServoPosition.Text = ServoPosition.ToString(CultureInfo.InvariantCulture);
        }

        private void SpeedTestSlider_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            Speed = e.NewValue;
            TxtTestSpeed.Text = Speed.ToString(CultureInfo.InvariantCulture);
        }

        private void BtnBack_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }

        private void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            _servo.SetPosition(0).AllowTimeToMove(3000).Go();
            for (int i = 1; i < 180; i++)
            {
                _servo.SetPosition(i).AllowTimeToMove(100).Go();
            }
            _servo.SetPosition(0).AllowTimeToMove(3000).Go();
        }

        private async void BtnActivate_Click(object sender, RoutedEventArgs e)
        {
            if (Activated)
            {
                Activated = false;
            }
            else
            {
                Activated = true;
                await Task.Run(() => ServoControlTask());
            }
        }

        private void BtnTargetSpeedIncrease_Click(object sender, RoutedEventArgs e)
        {
            var targetSpeed = double.Parse(TxtTargetSpeed.Text);
            targetSpeed += 0.1;
            TargetSpeed = targetSpeed;

            UpdateUi();
        }

        private void BtnTargetSpeedDecrease_Click(object sender, RoutedEventArgs e)
        {
            var targetSpeed = double.Parse(TxtTargetSpeed.Text);
            targetSpeed -= 0.1;
            TargetSpeed = targetSpeed;

            UpdateUi();
        }

        private void BtnToleranceDecreaseKts_Click(object sender, RoutedEventArgs e)
        {
            var tolerance = double.Parse(TxtToleranceSpeed.Text);
            tolerance -= 0.1;
            ToleranceSpeed = tolerance;

            UpdateUi();
        }

        private void BtnToleranceIncreaseKts_Click(object sender, RoutedEventArgs e)
        {
            var tolerance = double.Parse(TxtToleranceSpeed.Text);
            tolerance += 0.1;
            ToleranceSpeed = tolerance;

            UpdateUi();
        }

        private void ServoControlTask()
        {
            while (Activated)
            {
                var diff = TargetSpeed - Speed;

                if (Math.Abs(diff) > ToleranceSpeed && Speed > 0)
                {
                    if (diff > 0.0d)
                    {
                        ServoPosition += 1;
                    }
                    if (diff < 0.0d && ServoPosition >= 0)
                    {
                        ServoPosition -= 1;
                    }

                    _servo.SetPosition(ServoPosition).AllowTimeToMove(100).Go();
                }

                var wait = 1000 - (Math.Abs(diff) * 100 * 2);
                Task.Delay((int)(wait > 0 ? wait : 0)).Wait();
            }
        }

        private void UpdateUi()
        {
            TxtTargetSpeed.Text = TargetSpeed.ToString(CultureInfo.InvariantCulture);
            TxtToleranceSpeed.Text = ToleranceSpeed.ToString(CultureInfo.InvariantCulture);
        }
    }
}