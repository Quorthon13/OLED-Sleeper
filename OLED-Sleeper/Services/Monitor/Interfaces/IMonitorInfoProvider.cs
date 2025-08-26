// File: Services/IMonitorInfoProvider.cs
using OLED_Sleeper.Models;
using System.Collections.Generic;

namespace OLED_Sleeper.Services.Monitor.Interfaces
{
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
        bool GetDdcCiSupport(MonitorInfo monitor);

        /// <summary>
        /// Returns the hardware ID for the given monitor.
        /// </summary>
        string GetHardwareId(MonitorInfo monitor);
    }
}