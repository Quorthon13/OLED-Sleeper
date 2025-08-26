using OLED_Sleeper.Models;
using System.Collections.Generic;

namespace OLED_Sleeper.Services.Monitor.Interfaces
{
    public interface IMonitorInfoManager
    {
        /// <summary>
        /// Gets the current list of monitors, from the cache if available.
        /// </summary>
        List<MonitorInfo> GetCurrentMonitors();

        /// <summary>
        /// Forces a refresh of the monitor list from the system.
        /// </summary>
        void RefreshMonitors();

        /// <summary>
        /// Gets the latest, up-to-date list of monitors from the system (basic info only, no enrichment).
        /// </summary>
        List<MonitorInfo> GetLatestMonitorsBasicInfo();
    }
}