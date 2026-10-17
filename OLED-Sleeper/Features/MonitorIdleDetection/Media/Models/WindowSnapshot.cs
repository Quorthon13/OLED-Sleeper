using System.Windows;

namespace OLED_Sleeper.Features.MonitorIdleDetection.Media.Models
{
    /// <summary>
    /// A top-level window that is a candidate for showing video, captured during a window walk.
    /// </summary>
    /// <param name="Family">The process family of the window's content: the lowercased executable path.</param>
    /// <param name="Bounds">The extended frame bounds in physical screen pixels.</param>
    /// <param name="HasCaption">Whether the window has a title bar. A window that is fullscreen has dropped it; a maximized one has not.</param>
    public sealed record WindowSnapshot(string Family, Rect Bounds, bool HasCaption);
}
