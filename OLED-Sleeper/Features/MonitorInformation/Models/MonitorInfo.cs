using System.Windows;

namespace OLED_Sleeper.Features.MonitorInformation.Models
{
    /// <summary>
    /// Represents a physical or logical monitor attached to the system.
    /// Contains identifying information, geometry, and capabilities.
    /// </summary>
    public class MonitorInfo
    {
        /// <summary>
        /// Gets or sets the device name (e.g., \\.\DISPLAY1).
        /// </summary>
        public string? DeviceName { get; set; }

        /// <summary>
        /// Gets or sets the unique hardware ID for the monitor.
        /// </summary>
        public string? HardwareId { get; set; }

        /// <summary>
        /// Gets or sets the bounding rectangle of the monitor in screen coordinates.
        /// </summary>
        public Rect Bounds { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this monitor is the primary display.
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Gets or sets the DPI (dots per inch) of the monitor.
        /// </summary>
        public uint Dpi { get; set; }

        /// <summary>
        /// Gets or sets the display number (e.g., 1 for DISPLAY1).
        /// </summary>
        public int DisplayNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether DDC/CI is supported by this monitor.
        /// </summary>
        public bool IsDdcCiSupported { get; set; }
    }
}