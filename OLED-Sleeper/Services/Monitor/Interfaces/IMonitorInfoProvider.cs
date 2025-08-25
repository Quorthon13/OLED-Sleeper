// File: Services/IMonitorInfoProvider.cs
using OLED_Sleeper.Models;
using System.Collections.Generic;

namespace OLED_Sleeper.Services.Monitor.Interfaces
{
    public interface IMonitorInfoProvider
    {
        /// <summary>
        /// Enumerates all monitors connected to the system and returns their information.
        /// </summary>
        /// <returns>A list of <see cref="MonitorInfo"/> objects representing each monitor.</returns>
        List<MonitorInfo> GetMonitors();
    }
}