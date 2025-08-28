using OLED_Sleeper.Models;

namespace OLED_Sleeper.Services.Monitor.Info.Interfaces
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
        /// Returns whether the given monitor supports DDC/CI.
        /// </summary>
        /// <param name="monitor">The monitor to check.</param>
        /// <returns>True if DDC/CI is supported; otherwise, false.</returns>
        bool GetDdcCiSupport(MonitorInfo monitor);

        /// <summary>
        /// Returns the hardware ID for the given monitor.
        /// </summary>
        /// <param name="monitor">The monitor to get the hardware ID for.</param>
        /// <returns>The hardware ID string.</returns>
        string GetHardwareId(MonitorInfo monitor);
    }
}