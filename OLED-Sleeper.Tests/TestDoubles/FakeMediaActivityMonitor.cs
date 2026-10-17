using OLED_Sleeper.Features.MonitorIdleDetection.Media.Interfaces;
using OLED_Sleeper.Features.MonitorIdleDetection.Media.Models;

namespace OLED_Sleeper.Tests.TestDoubles
{
    /// <summary>
    /// A media monitor that answers with whatever decisions a test sets and records what it was asked.
    /// </summary>
    public class FakeMediaActivityMonitor : IMediaActivityMonitor
    {
        /// <summary>
        /// Gets the decisions returned for every call, keyed by hardware ID. They are returned whether or not the monitor was requested.
        /// </summary>
        public Dictionary<string, MediaDecision> Decisions { get; } = new();

        /// <summary>
        /// Gets the requests received by each call to <see cref="Evaluate"/>.
        /// </summary>
        public List<IReadOnlyList<MediaMonitorRequest>> Calls { get; } = new();

        /// <summary>
        /// Gets the hardware IDs requested across all calls.
        /// </summary>
        public IEnumerable<string> RequestedIds => Calls.SelectMany(call => call).Select(request => request.HardwareId);

        /// <inheritdoc />
        public IReadOnlyDictionary<string, MediaDecision> Evaluate(IReadOnlyList<MediaMonitorRequest> requests)
        {
            Calls.Add(requests.ToList());
            return Decisions;
        }
    }
}
