using OLED_Sleeper.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OLED_Sleeper.Services.Monitor.Info.Interfaces
{
    /// <summary>
    /// Defines the contract for managing and refreshing monitor information from the system.
    /// </summary>
    public interface IMonitorInfoManager
    {
        /// <summary>
        /// Begins asynchronous retrieval and enrichment of the monitor list.
        /// Subscribers will be notified via <see cref="MonitorListReady"/> when the list is available.
        /// If the cache is already populated, the event is raised immediately.
        /// </summary>
        void GetCurrentMonitorsAsync();

        /// <summary>
        /// Raised when the monitor list has been retrieved and enriched.
        /// </summary>
        event EventHandler<IReadOnlyList<MonitorInfo>> MonitorListReady;

        /// <summary>
        /// Forces a refresh of the monitor list from the system asynchronously.
        /// The refresh is performed on a background thread, and subscribers will be notified via <see cref="MonitorListReady"/> when the list is available.
        /// This method is event-driven and does not return a Task.
        /// </summary>
        void RefreshMonitorsAsync();

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