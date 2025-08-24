using OLED_Sleeper.Models;

namespace OLED_Sleeper.Services
{
    public interface IMonitorService
    {
        List<MonitorInfo> GetMonitors();
    }
}