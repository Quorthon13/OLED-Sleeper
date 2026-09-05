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
        /// Gets or sets the device name (e.g., \\.\DISPLAY1). Windows reassigns these between panels when
        /// the display set changes.
        /// </summary>
        public string DeviceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unique hardware ID for the monitor.
        /// Empty when no attached display device matched.
        /// </summary>
        public string HardwareId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets how Windows numbers the panels showing this desktop surface, such as <c>1|2</c> for a
        /// duplicated one. Empty when the surface is not duplicated.
        /// </summary>
        public string TopologyLabel { get; set; } = string.Empty;

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
        /// Gets or sets what a DDC/CI probe of this monitor reported.
        /// Null until the monitor has been probed.
        /// </summary>
        public DdcCiCapabilities? Capabilities { get; set; }
    }
}
