// File: Extensions/MonitorInfoExtensions.cs
using OLED_Sleeper.Models;
using System.Text.RegularExpressions;

namespace OLED_Sleeper.Extensions
{
    internal static class MonitorInfoExtensions
    {
        /// <summary>
        /// Parses the display number from a monitor's device name (e.g., extracts '1' from "\\.\DISPLAY1").
        /// </summary>
        /// <param name="monitorInfo">The MonitorInfo object.</param>
        /// <returns>The parsed display number, or -1 if not found.</returns>
        public static int GetDisplayNumber(this MonitorInfo monitorInfo)
        {
            var match = Regex.Match(monitorInfo.DeviceName, @"\d+$");
            if (match.Success && int.TryParse(match.Value, out int number))
            {
                return number;
            }
            // Fallback, though a device name should always have a number.
            return -1;
        }
    }
}