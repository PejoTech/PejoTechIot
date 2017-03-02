﻿using System.Threading.Tasks;
using Windows.Devices.I2c;

namespace Magellanic.I2C
{
    public interface I2cSlave
    {
        byte[] DeviceIdentifier { get; set; }

        I2cDevice Slave { get; set; }

        byte[] GetDeviceId();

        byte GetI2cAddress();

        Task Initialize();

        bool IsConnected();
    }
}