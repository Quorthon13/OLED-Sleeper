using System.Windows;

namespace OLED_Sleeper.Models
{
    /// <summary>
    /// Provides event data for monitor idle/active state transitions.
    /// Used by services to communicate monitor state changes, including context for the event.
    /// </summary>
    public class MonitorIdleStateEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the unique hardware ID of the monitor.
        /// </summary>
        public string HardwareId { get; }

        /// <summary>
        /// Gets the display number of the monitor.
        /// </summary>
        public int DisplayNumber { get; }

        /// <summary>
        /// Gets the bounds of the monitor in screen coordinates.
        /// </summary>
        public Rect Bounds { get; }

        /// <summary>
        /// Gets the user-configured settings for the monitor.
        /// </summary>
        public MonitorSettings Settings { get; }

        /// <summary>
        /// Gets the handle of the foreground window at the time of the event.
        /// </summary>
        public nint ForegroundWindowHandle { get; }

        /// <summary>
        /// Gets the reason why the monitor is considered active (e.g., mouse, window, input).
        /// </summary>
        public ActivityReason Reason { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the event should be ignored by the sender.
        /// Subscribers can set this to true to prevent the sender from changing its internal state.
        /// </summary>
        public bool IsIgnored { get; set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorIdleStateEventArgs"/> class.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        /// <param name="displayNumber">The display number of the monitor.</param>
        /// <param name="bounds">The bounds of the monitor in screen coordinates.</param>
        /// <param name="settings">The user-configured settings for the monitor.</param>
        /// <param name="foregroundWindowHandle">The handle of the foreground window at the time of the event.</param>
        /// <param name="reason">The reason why the monitor is considered active.</param>
        public MonitorIdleStateEventArgs(
            string hardwareId,
            int displayNumber,
            Rect bounds,
            MonitorSettings settings,
            nint foregroundWindowHandle,
            ActivityReason reason)
        {
            HardwareId = hardwareId;
            DisplayNumber = displayNumber;
            Bounds = bounds;
            Settings = settings;
            ForegroundWindowHandle = foregroundWindowHandle;
            Reason = reason;
        }
    }
}