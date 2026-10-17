using System.Windows;

namespace OLED_Sleeper.Features.MonitorIdleDetection.Media.Models
{
    /// <summary>
    /// The result of one window walk.
    /// </summary>
    /// <param name="Windows">The candidate windows in z-order, topmost first. Our own overlays are never in this list.</param>
    /// <param name="OverlayBounds">The bounds of the blackout overlays that are currently shown.</param>
    public sealed record WindowSnapshotSet(IReadOnlyList<WindowSnapshot> Windows, IReadOnlyList<Rect> OverlayBounds)
    {
        /// <summary>
        /// A set with no windows and no overlays.
        /// </summary>
        public static WindowSnapshotSet Empty { get; } = new(Array.Empty<WindowSnapshot>(), Array.Empty<Rect>());
    }
}
