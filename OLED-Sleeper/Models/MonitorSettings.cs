namespace OLED_Sleeper.Models
{
    /// <summary>
    /// Represents user-configurable settings for a monitor, including idle detection and behavior.
    /// </summary>
    public class MonitorSettings
    {
        /// <summary>
        /// Gets or sets the unique hardware ID for the monitor.
        /// </summary>
        public string HardwareId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this monitor is managed by the application.
        /// </summary>
        public bool IsManaged { get; set; } = false;

        /// <summary>
        /// Gets or sets the behavior to apply when the monitor becomes idle (e.g., dim or blackout).
        /// </summary>
        public MonitorBehavior Behavior { get; set; } = MonitorBehavior.Dim;

        /// <summary>
        /// Gets or sets the dimming level to apply when the monitor is dimmed.
        /// </summary>
        public double DimLevel { get; set; } = 15;

        /// <summary>
        /// Gets or sets the idle timeout value (unit specified by <see cref="IdleUnit"/>).
        /// </summary>
        public int? IdleValue { get; set; } = 30;

        /// <summary>
        /// Gets or sets the time unit for the idle timeout.
        /// </summary>
        public TimeUnit IdleUnit { get; set; } = TimeUnit.Seconds;

        /// <summary>
        /// Gets or sets a value indicating whether system input (keyboard/mouse) should reset idle state.
        /// </summary>
        public bool IsActiveOnInput { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether mouse position should reset idle state.
        /// </summary>
        public bool IsActiveOnMousePosition { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the active window should reset idle state.
        /// </summary>
        public bool IsActiveOnActiveWindow { get; set; } = false;

        /// <summary>
        /// Gets the idle timeout in milliseconds, based on <see cref="IdleValue"/> and <see cref="IdleUnit"/>.
        /// </summary>
        public int IdleTimeMilliseconds
        {
            get
            {
                if (IdleValue == null) return 0;
                return IdleUnit switch
                {
                    TimeUnit.Minutes => IdleValue.Value * 60 * 1000,
                    TimeUnit.Hours => IdleValue.Value * 60 * 60 * 1000,
                    _ => IdleValue.Value * 1000
                };
            }
        }
    }
}