using OLED_Sleeper.Features.MonitorIdleDetection.Media.Models;

namespace OLED_Sleeper.Features.MonitorIdleDetection.Media.Interfaces
{
    /// <summary>
    /// Decides, per monitor, whether a video is playing on it. The expensive probes are throttled and cached inside.
    /// </summary>
    public interface IMediaActivityMonitor
    {
        /// <summary>
        /// Evaluates the requested monitors. With no requests nothing is probed or captured.
        /// </summary>
        /// <param name="requests">The monitors to evaluate.</param>
        /// <returns>The decision for each requested monitor, keyed by hardware ID.</returns>
        IReadOnlyDictionary<string, MediaDecision> Evaluate(IReadOnlyList<MediaMonitorRequest> requests);
    }
}
