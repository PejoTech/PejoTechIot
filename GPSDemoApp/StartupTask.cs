﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using System.Collections.ObjectModel;
using Windows.Devices.Enumeration;
using System.Threading.Tasks;
using System.Threading;
using AdafruitClassLibrary;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace GPSDemoApp
{
    public sealed class StartupTask : IBackgroundTask
    {
        Gps Gps { get; set; }

        BackgroundTaskDeferral deferral;
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();
            // 
            // TODO: Insert code to perform background work
            //
            // If you start any asynchronous methods here, prevent the task
            // from closing prematurely by using BackgroundTaskDeferral as
            // described in http://aka.ms/backgroundtaskdeferral
            //

            Gps = new Gps();

            await Gps.ConnectToUARTAsync(9600);

            if (Gps.Connected)
            {
                await Gps.SetSentencesReportingAsync(0, 1, 0, 0, 0, 0);
                CancellationTokenSource readCancellationTokenSource = new CancellationTokenSource();
                Gps.StartReading();
            }

            deferral.Complete();
        }
    }
}
