using System;

namespace OLED_Sleeper.Events
{
    /// <summary>
    /// Provides application-wide notifications for global actions, such as restoring all monitors.
    /// Acts as a simple event aggregator for cross-component communication.
    /// </summary>
    public static class AppNotifications
    {
        /// <summary>
        /// Raised when any part of the application requests that all monitors be restored to their original state.
        /// </summary>
        public static event Action? RestoreAllMonitorsRequested;

        /// <summary>
        /// Triggers the <see cref="RestoreAllMonitorsRequested"/> event.
        /// </summary>
        public static void TriggerRestoreAllMonitors()
        {
            RestoreAllMonitorsRequested?.Invoke();
        }
    }
}