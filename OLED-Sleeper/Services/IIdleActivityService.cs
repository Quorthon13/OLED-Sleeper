using OLED_Sleeper.Models;

namespace OLED_Sleeper.Services
{
    public interface IIdleActivityService
    {
        void Start();

        void Stop();

        void UpdateSettings(List<MonitorSettings> monitorSettings);
    }
}