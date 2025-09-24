namespace OLED_Sleeper.Features.MonitorBehavior.Models
{
    /// <summary>
    /// Specifies the behavior to apply to a monitor when it becomes idle.
    /// </summary>
    public enum MonitorBehaviorType
    {
        /// <summary>
        /// Dim the monitor's brightness.
        /// </summary>
        Dim,

        /// <summary>
        /// Apply a blackout overlay to the monitor.
        /// </summary>
        Blackout
    }
}