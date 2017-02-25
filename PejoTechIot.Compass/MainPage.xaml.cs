using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Media.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PejoTechIot.Compass
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const int ButtonPinNr = 21;

        public GpioPin GpioButtonPin { get; set; }
        
        public Hmc5883L Compass { get; set; }

        public MainPage()
        {
            this.InitializeComponent();

            this.Loaded += Page_Loaded;
            this.Unloaded += Page_Unloaded;
        }

        private void Page_Unloaded(object sender, object args)
        {
            GpioButtonPin.Dispose();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await SetupButton();
            await SetupCompass();

            RefreshDirection();
        }

        private async Task SetupButton()
        {
            var gpio = await GpioController.GetDefaultAsync();

            if (gpio == null)
            {
                this.txtMessages.Text = "No GPIO controller found!";
                return;
            }

            GpioButtonPin = gpio.OpenPin(ButtonPinNr);
            GpioButtonPin.SetDriveMode(GpioButtonPin.IsDriveModeSupported(GpioPinDriveMode.InputPullUp)
                ? GpioPinDriveMode.InputPullUp
                : GpioPinDriveMode.Input);

            GpioButtonPin.DebounceTimeout = TimeSpan.FromMilliseconds(50);
            GpioButtonPin.ValueChanged += ButtonPin_ValueChanged;

            var timer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(500)};
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            RefreshDirection();
        }

        private async Task SetupCompass()
        {
            Compass = new Hmc5883L();

            await Compass.Initialize();

            if (Compass.IsConnected())
            {
                Compass.SetOperatingMode(Hmc5884LOperatingMode.ContinuousOperatingMode);
            }
        }

        private void ButtonPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (e.Edge == GpioPinEdge.FallingEdge)
            {
                RefreshDirection();
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            while (true)
            {
                RefreshDirection();
            }
        }

        private async void RefreshDirection()
        {
            var direction = Compass.GetRawData();

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () =>
                {
                    txtX.Text = direction.X.ToString();
                    txtY.Text = direction.Y.ToString();
                    txtZ.Text = direction.Z.ToString();

                    txtHeading.Text = (180 * Math.Atan2(direction.Y, direction.X) / Math.PI).ToString(CultureInfo.InvariantCulture);

                    ////Direction(y > 0) = 90 - [arcTAN(x / y)] * 180 /¹
                    //if (direction.Y > 0)
                    //{
                    //    txtHeading.Text =
                    //        (90 - Math.Atan2(direction.Y, direction.X) / Math.PI * 180).ToString(
                    //            CultureInfo.InvariantCulture);
                    //}
                    ////Direction(y < 0) = 270 - [arcTAN(x / y)] * 180 /¹
                    //else if (direction.Y < 0)
                    //{
                    //    txtHeading.Text =
                    //        (270 - Math.Atan2(direction.Y, direction.X) / Math.PI * 180 ).ToString(
                    //            CultureInfo.InvariantCulture);
                    //}
                    ////Direction(y = 0, x < 0) = 180.0
                    //else if (direction.Y == 0 && direction.X < 0)
                    //{
                    //    txtHeading.Text = "180.0";
                    //}
                    ////Direction(y = 0, x > 0) = 0.0
                    //else
                    //{
                    //    txtHeading.Text = "0.0";
                    //}
                });
        }
    }
}
