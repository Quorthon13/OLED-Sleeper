using OLED_Sleeper.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OLED_Sleeper.Helpers
{
    /// <summary>
    /// Provides static helper methods for monitor-related operations such as list comparison and display number parsing.
    /// </summary>
    public static class MonitorHelper
    {
        /// <summary>
        /// Returns a list of monitors that were disconnected.
        /// </summary>
        /// <param name="oldMonitors">The previous list of monitors.</param>
        /// <param name="newMonitors">The new list of monitors.</param>
        /// <returns>List of disconnected monitors.</returns>
        public static List<MonitorInfo> GetDisconnectedMonitors(IReadOnlyList<MonitorInfo> oldMonitors, IReadOnlyList<MonitorInfo> newMonitors)
        {
            return oldMonitors.Where(old => !newMonitors.Any(n => n.HardwareId == old.HardwareId)).ToList();
        }

        /// <summary>
        /// Returns a list of monitors that were reconnected.
        /// </summary>
        /// <param name="oldMonitors">The previous list of monitors.</param>
        /// <param name="newMonitors">The new list of monitors.</param>
        /// <returns>List of reconnected monitors.</returns>
        public static List<MonitorInfo> GetReconnectedMonitors(IReadOnlyList<MonitorInfo> oldMonitors, IReadOnlyList<MonitorInfo> newMonitors)
        {
            return newMonitors.Where(n => !oldMonitors.Any(old => old.HardwareId == n.HardwareId)).ToList();
        }

        /// <summary>
        /// Parses the display number from a device name string.
        /// </summary>
        /// <param name="deviceName">The device name string.</param>
        /// <returns>The display number if found, otherwise -1.</returns>
        public static int ParseDisplayNumber(string deviceName)
        {
            var match = Regex.Match(deviceName, @"\d+$");
            if (match.Success && int.TryParse(match.Value, out int number))
            {
                return number;
            }
            return -1;
        }
    }
}