using OLED_Sleeper.Features.MonitorInformation.Models;

namespace OLED_Sleeper.UI.Helpers
{
    /// <summary>
    /// Builds the title a monitor is shown under. This is a static helper class and does not hold any state.
    /// </summary>
    public static class MonitorTitleFormatter
    {
        /// <summary>
        /// Titles a monitor the way Windows titles it: the display number, or the numbers of every panel
        /// showing the surface when it is duplicated, such as <c>Monitor 1|2</c>.
        /// </summary>
        /// <param name="monitor">The monitor to title.</param>
        /// <returns>The title, marked as the primary display where applicable.</returns>
        public static string Format(MonitorInfo monitor)
        {
            string number = string.IsNullOrEmpty(monitor.TopologyLabel)
                ? monitor.DisplayNumber.ToString()
                : monitor.TopologyLabel;

            return monitor.IsPrimary ? $"Monitor {number} (Primary)" : $"Monitor {number}";
        }
    }
}
