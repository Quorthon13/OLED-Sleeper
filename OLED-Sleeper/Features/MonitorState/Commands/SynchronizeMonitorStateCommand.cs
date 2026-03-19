using OLED_Sleeper.Core.Interfaces;
using OLED_Sleeper.Features.MonitorInformation.Models;

namespace OLED_Sleeper.Features.MonitorState.Commands
{
    /// <summary>
    /// Command to synchronize the application's monitor state with the current set of connected monitors and settings.
    /// This command is typically dispatched when the set of physical or logical monitors changes (e.g., connection/disconnection, hotplug, etc.).
    /// It provides both the previous and current monitor lists so that the handler can reconcile overlays, brightness, and idle detection state.
    /// </summary>
    public class SynchronizeMonitorStateCommand : ICommand
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
        /// Initializes a new instance of the <see cref="SynchronizeMonitorStateCommand"/> class.
        /// </summary>
        /// <param name="oldMonitors">The list of monitors before the change.</param>
        /// <param name="newMonitors">The list of monitors after the change.</param>
        public SynchronizeMonitorStateCommand(IReadOnlyList<MonitorInfo> oldMonitors, IReadOnlyList<MonitorInfo> newMonitors)
        {
            OldMonitors = oldMonitors;
            NewMonitors = newMonitors;
        }
    }
}