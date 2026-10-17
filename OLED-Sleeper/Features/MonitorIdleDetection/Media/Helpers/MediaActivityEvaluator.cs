using OLED_Sleeper.Features.MonitorIdleDetection.Media.Models;
using System.Windows;

namespace OLED_Sleeper.Features.MonitorIdleDetection.Media.Helpers
{
    /// <summary>
    /// The decision rule for the video option. Pure, so every signal combination is unit-testable.
    /// </summary>
    public static class MediaActivityEvaluator
    {
        /// <summary>
        /// The slack, in pixels, allowed on each edge when deciding that a window covers a monitor.
        /// Frame bounds can fall a pixel or two short of the monitor.
        /// </summary>
        public const double CoverTolerance = 2;

        /// <summary>
        /// Decides whether a video is playing on one monitor.
        /// </summary>
        /// <remarks>
        /// A video is playing when either:
        /// the topmost window on the monitor is fullscreen, and display-required is set or that window's process family is
        /// producing audio; or the monitor shows sustained motion together with display-required, or with audio from a window on the monitor.
        /// Audio alone never counts, and neither does display-required on a static monitor.
        /// A maximized window is not fullscreen, even when an auto-hidden taskbar lets it cover the whole monitor, so it counts only
        /// through the motion rule. That keeps a video on another monitor, which sets display-required system-wide, from holding this one awake.
        /// </remarks>
        /// <param name="displayRequired">Whether the system reports a display-required request. System-wide, not per monitor.</param>
        /// <param name="audibleFamilies">The process families that produced audio recently.</param>
        /// <param name="windowsTopFirst">The candidate windows in z-order, topmost first, across all monitors.</param>
        /// <param name="monitorBounds">The monitor's bounds in physical screen pixels.</param>
        /// <param name="isMoving">Whether the monitor's picture shows sustained motion.</param>
        /// <returns>The decision, with the signals that made it.</returns>
        public static MediaDecision Evaluate(
            bool displayRequired,
            IReadOnlySet<string> audibleFamilies,
            IReadOnlyList<WindowSnapshot> windowsTopFirst,
            Rect monitorBounds,
            bool isMoving)
        {
            var onMonitor = windowsTopFirst.Where(window => Intersects(window.Bounds, monitorBounds)).ToList();

            var top = onMonitor.FirstOrDefault();
            if (top != null && !top.HasCaption && Covers(top.Bounds, monitorBounds))
            {
                if (displayRequired)
                    return new MediaDecision(true, "fullscreen+display-required");
                if (audibleFamilies.Contains(top.Family))
                    return new MediaDecision(true, $"fullscreen+audio({System.IO.Path.GetFileName(top.Family)})");
            }

            if (isMoving)
            {
                if (displayRequired)
                    return new MediaDecision(true, "motion+display-required");

                var audible = onMonitor.FirstOrDefault(window => audibleFamilies.Contains(window.Family));
                if (audible != null)
                    return new MediaDecision(true, $"motion+audio({System.IO.Path.GetFileName(audible.Family)})");
            }

            return MediaDecision.None;
        }

        /// <summary>
        /// Gets whether a rectangle covers a whole monitor, within <see cref="CoverTolerance"/> on each edge.
        /// </summary>
        public static bool Covers(Rect window, Rect monitor)
        {
            return window.Left <= monitor.Left + CoverTolerance &&
                   window.Top <= monitor.Top + CoverTolerance &&
                   window.Right >= monitor.Right - CoverTolerance &&
                   window.Bottom >= monitor.Bottom - CoverTolerance;
        }

        /// <summary>
        /// Gets whether a window's rectangle shares an area with a monitor.
        /// </summary>
        private static bool Intersects(Rect window, Rect monitor)
        {
            var intersection = Rect.Intersect(window, monitor);
            return !intersection.IsEmpty && intersection.Width > 0 && intersection.Height > 0;
        }
    }
}
