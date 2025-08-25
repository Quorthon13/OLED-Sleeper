using OLED_Sleeper.Models;
using System.Windows;

namespace OLED_Sleeper.Events
{
    public class MonitorStateEventArgs(
        string hardwareId,
        int displayNumber,
        Rect bounds,
        MonitorSettings settings,
        IntPtr foregroundWindowHandle,
        ActivityReason reason)
        : EventArgs
    {
        public string HardwareId { get; } = hardwareId;
        public int DisplayNumber { get; } = displayNumber;
        public Rect Bounds { get; } = bounds;
        public MonitorSettings Settings { get; } = settings;
        public IntPtr ForegroundWindowHandle { get; } = foregroundWindowHandle;
        public ActivityReason Reason { get; } = reason;

        /// <summary>
        /// A subscriber can set this to true to prevent the sender from changing its internal state.
        /// </summary>
        public bool IsIgnored { get; set; } = false;
    }
}