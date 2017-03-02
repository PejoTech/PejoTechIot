using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;
using Windows.Media.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Magellanic.I2C;
using Magellanic.I2C.Exceptions;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PejoTechIot.Compass
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private bool _loop = false;
        private const int ButtonPinNr = 21;

        public GpioPin GpioButtonPin { get; set; }

        public static Hmc5883L Compass { get; set; }

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
        }

        private void ButtonPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (_loop)
            {
                if (e.Edge == GpioPinEdge.FallingEdge)
                {
                    RefreshDirection();
                }
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            while (_loop)
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
                    txtX.Text = direction.X.ToString(CultureInfo.InvariantCulture);
                    txtY.Text = direction.Y.ToString(CultureInfo.InvariantCulture);
                    txtZ.Text = direction.Z.ToString(CultureInfo.InvariantCulture);

                    txtHeading.Text = ((180 * Math.Atan2(direction.Y, direction.X) / Math.PI) + 2.04).ToString(CultureInfo.InvariantCulture);
                });
        }
    }
}
