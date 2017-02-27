/*------------------------------------------------------------------------
  Windows IoT Core demonstration app for the Adafruit Ultimate GPS Hat

  Requires the Adafruit Class Library for Windows IoT Core

  Written by Rick Lesniak for Adafruit Industries.

  Adafruit invests time and resources providing this open source code,
  please support Adafruit and open-source hardware by purchasing products
  from Adafruit!

  ------------------------------------------------------------------------
  Adafruit GPSHeaded is free software: you can redistribute it and/or
  modify it under the terms of the GNU Lesser General Public License
  as published by the Free Software Foundation, either version 3 of
  the License, or (at your option) any later version.

  Adafruit GPSHeaded is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU Lesser General Public License for more details.

  You should have received a copy of the GNU Lesser General Public
  License along with DotStar.  If not, see <http://www.gnu.org/licenses/>.
  ------------------------------------------------------------------------*/

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PejoTechIot.Gps
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        AdafruitClassLibrary.Gps Gps { get; set; }

        public MainPage()
        {
            this.InitializeComponent();
            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Gps = new AdafruitClassLibrary.Gps();

                Gps.RMCEvent += OnRmcEvent;
                Gps.GGAEvent += OnGgaEvent;

                StartGps();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Error starting app: {0}", ex.Message));
            }
        }

        private async void StartGps()
        {
            // see note below about changing baud rates.
            await Gps.ConnectToUARTAsync(9600);

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
            //if (Gps.Connected)
            //{
            //    await Gps.SetBaudRate(19200);
            //    Gps.DisconnectFromUART();
            //    await Gps.ConnectToUART(19200);
            //}

            if (Gps.Connected)
            {
                await Gps.SetSentencesReportingAsync(0, 1, 0, 1, 0, 0);
                await Gps.SetUpdateFrequencyAsync(1);  //1Hz.  Change to 5 for 5Hz. Change to 10 for 10Hz.  Change to 0.1 for 0.1Hz.
                Gps.StartReading();
            }
        }

        private void OnRmcEvent(object sender, AdafruitClassLibrary.Gps.GPSRMC rmc)
        {
            if (rmc.Valid)
            {
                LatTextBox.Text = rmc.Latitude.ToString();
                LatHemiTextBox.Text = rmc.LatHemisphere;
                LonTextBox.Text = rmc.Longitude.ToString();
                LonHemiTextBox.Text = rmc.LonHemisphere;
                TimeTextBox.Text = rmc.TimeStamp.ToString("HH:mm:ss.fff");
                DateTextBox.Text = rmc.TimeStamp.ToString("dd-MM-yyyy");
                SpeedTextBox.Text = rmc.Speed.ToString();
                CourseTextBox.Text = rmc.Course.ToString();
            }
            else
            {
                LatTextBox.Text = "";
                LatHemiTextBox.Text = "";
                LonTextBox.Text = "";
                LonHemiTextBox.Text = "";
                TimeTextBox.Text = "";
                DateTextBox.Text = "";
                SpeedTextBox.Text = "";
                CourseTextBox.Text = "";
            }
        }

        private void OnGgaEvent(object sender, AdafruitClassLibrary.Gps.GPSGGA gga)
        {
            if (gga.Quality != AdafruitClassLibrary.Gps.GPSGGA.FixQuality.noFix)
            {
                AltitudeTextBox.Text = gga.Altitude.ToString();
                SatellitesTextBox.Text = gga.Satellites.ToString();
                AltUnitsTextBox.Text = gga.AltUnits;
            }
            else
            {
                AltitudeTextBox.Text = "";
                SatellitesTextBox.Text = "";
                AltUnitsTextBox.Text = "";
            }

            txtMessages.Text = gga.Quality.ToString();
        }
    }
}