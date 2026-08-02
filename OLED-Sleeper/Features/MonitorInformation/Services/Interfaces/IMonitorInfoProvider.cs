using OLED_Sleeper.Features.MonitorInformation.Models;

namespace OLED_Sleeper.Features.MonitorInformation.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for providing basic monitor information and DDC/CI support.
    /// </summary>
    public interface IMonitorInfoProvider
    {
        /// <summary>
        /// Enumerates all monitors connected to the system and returns their basic information (no enrichment).
        /// </summary>
        /// <returns>A list of <see cref="MonitorInfo"/> objects representing each monitor (basic info only).</returns>
        List<MonitorInfo> GetAllMonitorsBasicInfo();

        /// <summary>
        /// Probes the given monitor over DDC/CI for its support and its brightness range.
        /// </summary>
        /// <param name="monitor">The monitor to probe.</param>
        /// <returns>What the probe reported. Both fields are unset when the monitor did not answer.</returns>
        DdcCiCapabilities GetDdcCiCapabilities(MonitorInfo monitor);

        /// <summary>
        /// Returns the hardware ID for the given monitor.
        /// </summary>
        /// <param name="monitor">The monitor to get the hardware ID for.</param>
        /// <returns>The hardware ID string.</returns>
        string GetHardwareId(MonitorInfo monitor);
    }
}