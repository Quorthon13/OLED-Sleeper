using System.Windows;

namespace OLED_Sleeper.Features.MonitorIdleDetection.Media.Models
{
    /// <summary>
    /// Asks <see cref="Interfaces.IMediaActivityMonitor"/> to evaluate one monitor.
    /// </summary>
    /// <param name="HardwareId">The monitor's hardware ID, which keys the result.</param>
    /// <param name="Bounds">The monitor's bounds in physical screen pixels.</param>
    public sealed record MediaMonitorRequest(string HardwareId, Rect Bounds);
}
