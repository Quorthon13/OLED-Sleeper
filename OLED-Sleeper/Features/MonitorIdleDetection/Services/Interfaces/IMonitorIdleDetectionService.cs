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
        /// Starts the idle detection service and begins monitoring. Safe to call multiple times; only the first call starts the loop.
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
        void UpdateSettings(List<MonitorSettings> monitorSettings);

        /// <summary>
        /// Rebuilds managed monitors and resets per-monitor timers from persisted settings and an already-enriched live topology,
        /// without stopping the idle loop.
        /// </summary>
        /// <param name="monitorSettings">Full persisted settings list (managed flags are respected).</param>
        /// <param name="activeEnrichedMonitors">Current monitors with hardware IDs and DDC flags populated.</param>
        void ApplyTopologyAndSettings(List<MonitorSettings> monitorSettings, IReadOnlyList<MonitorInfo> activeEnrichedMonitors);
    }
}