using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PejoTechIot.Servo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        ServoController _servo;

        public MainPage()
        {
            this.InitializeComponent();

            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
            BtnMove.Click += BtnMove_Click;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _servo = new ServoController(5);
            await _servo.Connect();

            Move(_servo);
        }

        private static void Move(ServoController servo)
        {
            servo.SetPosition(0).AllowTimeToMove(1000).Go();
            for (int i = 1; i < 180; i++)
            {
                servo.SetPosition(i).AllowTimeToMove(100).Go();
            }
        }

        private void BtnMove_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            Move(_servo);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _servo.Dispose();
        }
    }
}
