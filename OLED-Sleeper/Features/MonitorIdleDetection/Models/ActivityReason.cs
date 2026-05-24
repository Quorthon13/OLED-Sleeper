namespace OLED_Sleeper.Features.MonitorIdleDetection.Models
{
    /// <summary>
    /// Defines the specific reason why a monitor is considered active during idle detection.
    /// Used to communicate the trigger for a monitor's state change.
    /// </summary>
    public enum ActivityReason
    {
        /// <summary>
        /// No activity detected; monitor is considered idle.
        /// </summary>
        None,

        /// <summary>
        /// Mouse cursor is within the monitor's bounds.
        /// </summary>
        MousePosition,

        /// <summary>
        /// The monitor contains the active (foreground) window.
        /// </summary>
        ActiveWindow,

        /// <summary>
        /// A visible application window is present on this monitor even if it is not the foreground window.
        /// </summary>
        VisibleWindow,

        /// <summary>
        /// System input (keyboard or mouse activity) was detected.
        /// </summary>
        SystemInput
    }
}