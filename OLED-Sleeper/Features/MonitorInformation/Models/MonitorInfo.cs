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
        public string DeviceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unique hardware ID for the monitor.
        /// Empty until the monitor is enriched; an enriched list never contains an empty one.
        /// </summary>
        public string HardwareId { get; set; } = string.Empty;

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

        /// <summary>
        /// Gets or sets the highest value the monitor accepts for the brightness VCP code.
        /// Zero when the monitor has not been probed or reported no range.
        /// </summary>
        public uint MaxBrightness { get; set; }
    }
}