using OLED_Sleeper.Features.MonitorInformation.Models;

namespace OLED_Sleeper.Features.MonitorInformation.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for managing and refreshing monitor information from the system.
    /// </summary>
    public interface IMonitorInfoManager
    {
        /// <summary>
        /// Gets the enriched monitor list, scanning the system on first use and serving a cached
        /// result thereafter. Concurrent callers share a single scan.
        /// </summary>
        /// <returns>The enriched list of <see cref="MonitorInfo"/> objects.</returns>
        Task<IReadOnlyList<MonitorInfo>> GetCurrentMonitorsAsync();

        /// <summary>
        /// Forces a re-scan of the monitor list from the system and replaces the cache with the result.
        /// </summary>
        /// <returns>The freshly enriched list of <see cref="MonitorInfo"/> objects.</returns>
        Task<IReadOnlyList<MonitorInfo>> RefreshMonitorsAsync();

        /// <summary>
        /// Gets the latest, up-to-date list of monitors from the system (basic info only, no DDC/CI probing).
        /// </summary>
        /// <returns>A list of <see cref="MonitorInfo"/> objects representing the latest monitors, including
        /// any whose hardware ID did not resolve.</returns>
        List<MonitorInfo> GetLatestMonitorsBasicInfo();

        /// <summary>
        /// Enriches a list of MonitorInfo objects with their DDC/CI capabilities.
        /// </summary>
        /// <param name="monitors">The list of monitors to enrich.</param>
        void EnrichMonitorInfoList(List<MonitorInfo> monitors);
    }
}
