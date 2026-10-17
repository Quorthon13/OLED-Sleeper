using OLED_Sleeper.Features.MonitorIdleDetection.Media.Models;

namespace OLED_Sleeper.Features.MonitorIdleDetection.Media.Interfaces
{
    /// <summary>
    /// Walks the top-level windows in z-order.
    /// </summary>
    public interface IWindowSnapshotProvider
    {
        /// <summary>
        /// Gets the candidate windows, topmost first. The walk is repeated at most about once a second; calls in between return the previous result.
        /// </summary>
        WindowSnapshotSet GetSnapshot();
    }
}
