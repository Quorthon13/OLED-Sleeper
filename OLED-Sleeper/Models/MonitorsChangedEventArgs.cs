using OLED_Sleeper.Models;

namespace OLED_Sleeper.Models
{
    /// <summary>
    /// Provides data for monitor set change events, containing both the previous and current monitor lists.
    /// </summary>
    public class MonitorsChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the list of monitors before the change occurred.
        /// </summary>
        public IReadOnlyList<MonitorInfo> OldMonitors { get; }

        /// <summary>
        /// Gets the list of monitors after the change occurred.
        /// </summary>
        public IReadOnlyList<MonitorInfo> NewMonitors { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorsChangedEventArgs"/> class.
        /// </summary>
        /// <param name="oldMonitors">The list of monitors before the change.</param>
        /// <param name="newMonitors">The list of monitors after the change.</param>
        public MonitorsChangedEventArgs(IReadOnlyList<MonitorInfo> oldMonitors, IReadOnlyList<MonitorInfo> newMonitors)
        {
            OldMonitors = oldMonitors;
            NewMonitors = newMonitors;
        }
    }
}