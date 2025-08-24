using System;

namespace OLED_Sleeper.Events
{
    /// <summary>
    /// A static class that holds application-wide events, acting as a simple event aggregator or "hook".
    /// </summary>
    public static class AppEvents
    {
        /// <summary>
        /// Raised when any part of the application requests that all monitors be restored to their original state.
        /// </summary>
        public static event Action RestoreAllMonitorsRequested;

        /// <summary>
        /// Triggers the RestoreAllMonitorsRequested event.
        /// </summary>
        public static void TriggerRestoreAllMonitors()
        {
            RestoreAllMonitorsRequested?.Invoke();
        }
    }
}