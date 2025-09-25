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
        /// System input (keyboard or mouse activity) was detected.
        /// </summary>
        SystemInput
    }
}