using OLED_Sleeper.Features.MonitorInformation.Models;

namespace OLED_Sleeper.Features.MonitorInformation.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for reading the attached monitors and probing them over DDC/CI.
    /// Reading and probing are separate calls: the first is cheap, the second is a bus round trip.
    /// </summary>
    public interface IMonitorInfoProvider
    {
        /// <summary>
        /// Enumerates the attached monitors, resolving names, geometry and hardware IDs in one pass.
        /// No DDC/CI probing is performed.
        /// </summary>
        /// <returns>One entry per attached monitor, with no capabilities. A monitor whose hardware ID
        /// could not be resolved is still returned, with <see cref="MonitorInfo.HardwareId"/> empty.</returns>
        List<MonitorInfo> GetAllMonitorsBasicInfo();

        /// <summary>
        /// Probes the given monitor over DDC/CI for its support and its brightness range.
        /// </summary>
        /// <param name="monitor">The monitor to probe.</param>
        /// <returns>What the probe reported. Both fields are unset when the monitor did not answer.</returns>
        DdcCiCapabilities GetDdcCiCapabilities(MonitorInfo monitor);
    }
}
