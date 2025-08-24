using OLED_Sleeper.Models;
using System;
using System.Windows;
using static OLED_Sleeper.Services.IdleActivityService; // Required for ActivityReason

namespace OLED_Sleeper.Events
{
    public class MonitorStateEventArgs : EventArgs
    {
        public string HardwareId { get; }
        public int DisplayNumber { get; }
        public Rect Bounds { get; }
        public MonitorSettings Settings { get; }
        public IntPtr ForegroundWindowHandle { get; }
        public ActivityReason Reason { get; }

        /// <summary>
        /// A subscriber can set this to true to prevent the sender from changing its internal state.
        /// </summary>
        public bool IsIgnored { get; set; }

        public MonitorStateEventArgs(
            string hardwareId,
            int displayNumber,
            Rect bounds,
            MonitorSettings settings,
            IntPtr foregroundWindowHandle,
            ActivityReason reason)
        {
            HardwareId = hardwareId;
            DisplayNumber = displayNumber;
            Bounds = bounds;
            Settings = settings;
            ForegroundWindowHandle = foregroundWindowHandle;
            Reason = reason;
            IsIgnored = false;
        }
    }
}