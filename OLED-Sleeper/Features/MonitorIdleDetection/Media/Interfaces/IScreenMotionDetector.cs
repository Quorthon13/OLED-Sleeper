using System.Windows;

namespace OLED_Sleeper.Features.MonitorIdleDetection.Media.Interfaces
{
    /// <summary>
    /// Tells whether a monitor's picture is changing the way video does.
    /// </summary>
    public interface IScreenMotionDetector
    {
        /// <summary>
        /// Samples a monitor if a second has passed since its last sample, and reports whether it shows sustained motion.
        /// </summary>
        /// <param name="hardwareId">The monitor's hardware ID, which keys its history.</param>
        /// <param name="bounds">The monitor's bounds in physical screen pixels.</param>
        bool IsMoving(string hardwareId, Rect bounds);

        /// <summary>
        /// Drops the history of a monitor, so stale frames are not compared once sampling resumes.
        /// </summary>
        /// <param name="hardwareId">The monitor's hardware ID.</param>
        void Forget(string hardwareId);
    }
}
