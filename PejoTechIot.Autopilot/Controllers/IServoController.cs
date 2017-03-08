using System;
using System.Threading.Tasks;

namespace PejoTechIot.Autopilot.Controllers
{
    public interface IServoController : IDisposable
    {
        int Frequency { get; set; }

        double MaximumDutyCycle { get; set; }

        double MinimumDutyCycle { get; set; }

        int ServoPin { get; set; }

        Task Connect();

        void Go();

        IServoController SetPosition(int degree);

        IServoController AllowTimeToMove(int pauseInMs);
    }
}