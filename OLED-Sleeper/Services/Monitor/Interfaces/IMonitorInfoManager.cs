using System.Collections.Generic;
using OLED_Sleeper.Models;

namespace OLED_Sleeper.Services.Monitor.Interfaces
{
    /// <summary>
    /// Defines the contract for managing and refreshing monitor information from the system.
    /// </summary>
    public interface IMonitorInfoManager
    {
        /// <summary>
        /// Gets the current list of monitors, from the cache if available.
        /// </summary>
        /// <returns>A list of <see cref="MonitorInfo"/> objects representing the current monitors.</returns>
        List<MonitorInfo> GetCurrentMonitors();

        /// <summary>
        /// Forces a refresh of the monitor list from the system.
        /// </summary>
        void RefreshMonitors();

        /// <summary>
        /// Gets the latest, up-to-date list of monitors from the system (basic info only, no enrichment).
        /// </summary>
        /// <returns>A list of <see cref="MonitorInfo"/> objects representing the latest monitors.</returns>
        List<MonitorInfo> GetLatestMonitorsBasicInfo();

        /// <summary>
        /// Enriches a list of MonitorInfo objects with DDC/CI support and hardware ID.
        /// </summary>
        /// <param name="monitors">The list of monitors to enrich.</param>
        void EnrichMonitorInfoList(List<MonitorInfo> monitors);
    }
}