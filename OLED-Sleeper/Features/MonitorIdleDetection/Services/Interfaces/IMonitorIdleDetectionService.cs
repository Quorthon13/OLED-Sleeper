using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.UserSettings.Models;

namespace OLED_Sleeper.Features.MonitorIdleDetection.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for monitor idle detection services.
    /// Handles detection of idle/active state transitions and updates settings for managed monitors.
    /// </summary>
    public interface IMonitorIdleDetectionService
    {
        /// <summary>
        /// Starts the idle detection service and begins monitoring.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the idle detection service and monitoring.
        /// </summary>
        void Stop();

        /// <summary>
        /// Updates the settings for all managed monitors.
        /// </summary>
        /// <param name="monitorSettings">The list of monitor settings to manage.</param>
        /// <param name="monitors">
        /// The monitors the settings are joined against, supplying the bounds used for hit-testing. The caller
        /// passes the list it already holds so detection cannot be rebuilt against stale geometry.
        /// </param>
        /// <returns>A task that completes once the new settings are in effect.</returns>
        Task UpdateSettingsAsync(List<MonitorSettings> monitorSettings, IReadOnlyList<MonitorInfo> monitors);
    }
}