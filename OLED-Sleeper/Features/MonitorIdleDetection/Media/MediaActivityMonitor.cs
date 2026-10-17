using OLED_Sleeper.Features.MonitorIdleDetection.Media.Helpers;
using OLED_Sleeper.Features.MonitorIdleDetection.Media.Interfaces;
using OLED_Sleeper.Features.MonitorIdleDetection.Media.Models;

namespace OLED_Sleeper.Features.MonitorIdleDetection.Media
{
    /// <summary>
    /// Combines the display-required, audio, window and motion signals into a per-monitor video decision.
    /// </summary>
    public class MediaActivityMonitor : IMediaActivityMonitor
    {
        private readonly IDisplayRequiredSignal _displayRequired;
        private readonly IAudioActivityProbe _audio;
        private readonly IWindowSnapshotProvider _windows;
        private readonly IScreenMotionDetector _motion;

        /// <summary>
        /// The monitors whose screens were sampled on the last call, so their history can be dropped once they stop being requested.
        /// </summary>
        private HashSet<string> _sampled = new();

        public MediaActivityMonitor(
            IDisplayRequiredSignal displayRequired,
            IAudioActivityProbe audio,
            IWindowSnapshotProvider windows,
            IScreenMotionDetector motion)
        {
            _displayRequired = displayRequired;
            _audio = audio;
            _windows = windows;
            _motion = motion;
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, MediaDecision> Evaluate(IReadOnlyList<MediaMonitorRequest> requests)
        {
            var requested = requests.Select(request => request.HardwareId).ToHashSet();
            foreach (var hardwareId in _sampled.Where(id => !requested.Contains(id)))
            {
                _motion.Forget(hardwareId);
            }
            _sampled = requested;

            var decisions = new Dictionary<string, MediaDecision>();
            if (requests.Count == 0) return decisions;

            bool displayRequired = _displayRequired.IsDisplayRequired();
            var audibleFamilies = _audio.GetAudibleFamilies();
            var snapshot = _windows.GetSnapshot();

            foreach (var request in requests)
            {
                // A shown overlay is itself a fullscreen window: nothing is playing behind it that this monitor should react to.
                if (snapshot.OverlayBounds.Any(overlay => MediaActivityEvaluator.Covers(overlay, request.Bounds)))
                {
                    _motion.Forget(request.HardwareId);
                    decisions[request.HardwareId] = MediaDecision.None;
                    continue;
                }

                bool isMoving = _motion.IsMoving(request.HardwareId, request.Bounds);
                decisions[request.HardwareId] = MediaActivityEvaluator.Evaluate(
                    displayRequired, audibleFamilies, snapshot.Windows, request.Bounds, isMoving);
            }

            return decisions;
        }
    }
}
