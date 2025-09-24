using OLED_Sleeper.Features.MonitorState.Models;

namespace OLED_Sleeper.Features.MonitorState.Services.Interfaces
{
    /// <summary>
    /// Provides an interface for a service that watches for changes in the set of connected monitors.
    /// </summary>
    public interface IMonitorStateWatcher : IDisposable
    {
        /// <summary>
        /// Occurs when the set of connected monitors changes.
        /// The event argument contains both the previous and current monitor lists.
        /// </summary>
        event EventHandler<MonitorsChangedEventArgs> MonitorsChanged;

        /// <summary>
        /// Starts monitoring for monitor state changes.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops monitoring for monitor state changes.
        /// </summary>
        void Stop();
    }
}