using OLED_Sleeper.Models;
using System.Collections.Generic;

namespace OLED_Sleeper.Services
{
    public interface IMonitorManagerService
    {
        /// <summary>
        /// Gets the current list of monitors, from the cache if available.
        /// </summary>
        List<MonitorInfo> GetCurrentMonitors();

        /// <summary>
        /// Forces a refresh of the monitor list from the system.
        /// </summary>
        void RefreshMonitors();
    }
}