namespace OLED_Sleeper.Features.MonitorState.Services.Interfaces
{
    /// <summary>
    /// Defines a service that monitors the set of connected displays and dispatches synchronization commands when changes are detected.
    /// </summary>
    public interface IMonitorStateWatcher : IDisposable
    {
        /// <summary>
        /// Starts monitoring for monitor state changes. The initial monitor list is retrieved and the timer is started.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops monitoring for monitor state changes.
        /// </summary>
        void Stop();
    }
}