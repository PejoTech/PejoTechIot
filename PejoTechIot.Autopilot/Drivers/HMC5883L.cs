using System;
using Magellanic.I2C;

namespace PejoTechIot.Autopilot.Drivers
{
    public class Hmc5883L : AbstractI2CDevice
    {
        private const byte I2CAddress = 0x1E;

        private byte _operatingModeRegister = 0x02;
        
        private readonly byte[] _firstDataRegister = new byte[] { 0x03 };

        private readonly byte[] _identificationRegisterA = new byte[] { 0x0A };

        private readonly byte[] _identificationRegisterB = new byte[] { 0x0B };

        private readonly byte[] _identificationRegisterC = new byte[] { 0x0C };


        public Hmc5883L()
        {
            this.DeviceIdentifier = new byte[3] { 0x48, 0x34, 0x33 };
        }

        public CompassRawDataModel GetRawData()
        {
            var compassData = new byte[6];

            this.Slave.WriteRead(_firstDataRegister, compassData);

            var xReading = (short)((compassData[0] << 8) | compassData[1]);
            var zReading = (short)((compassData[2] << 8) | compassData[3]);
            var yReading = (short)((compassData[4] << 8) | compassData[5]);

            return new CompassRawDataModel
            {
                Date = DateTime.Now,
                X = xReading,
                Y = yReading,
                Z = zReading
            };
        }
        
        public override byte[] GetDeviceId()
        {
            var identificationBufferA = new byte[1];
            var identificationBufferB = new byte[1];
            var identificationBufferC = new byte[1];

            this.Slave.WriteRead(_identificationRegisterA, identificationBufferA);
            this.Slave.WriteRead(_identificationRegisterB, identificationBufferB);
            this.Slave.WriteRead(_identificationRegisterC, identificationBufferC);

            return new byte[3] { identificationBufferA[0], identificationBufferB[0], identificationBufferC[0] };
        }

        public void SetOperatingMode(Hmc5884LOperatingMode operatingMode)
        {
            // convention is to specify the register first, and then the value to write to it
            var writeBuffer = new byte[2] { _operatingModeRegister, (byte)operatingMode };

            this.Slave.Write(writeBuffer);
        }

        public override byte GetI2cAddress()
        {
            return I2CAddress;
        }
    }

    public enum Hmc5884LOperatingMode
    {
        ContinuousOperatingMode = 0x00,
        SingleOperatingMode = 0x01,
        IdleOperatingMode = 0x10
    }

    public class CompassRawDataModel
    {
        public DateTime Date { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }
}
