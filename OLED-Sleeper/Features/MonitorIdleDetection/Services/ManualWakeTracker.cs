using OLED_Sleeper.Features.MonitorInformation.Models;
using System.Windows;

namespace OLED_Sleeper.Features.MonitorIdleDetection.Services
{

    /// <summary>One-shot mouse wake tracking for manual blackouts, guarded by the idle service's lock.</summary>
    public sealed class ManualWakeTracker
    {
        private readonly Dictionary<string, WakeState> _monitors = new(StringComparer.OrdinalIgnoreCase);
        public void Track(string hardwareId, Rect bounds, Point cursorAtBlackout) => _monitors[hardwareId] = new(bounds, cursorAtBlackout);
        public void Clear(string hardwareId) => _monitors.Remove(hardwareId);

        public void SynchronizeBounds(IReadOnlyList<MonitorInfo> monitors)
        {
            var byId = monitors.ToDictionary(m => m.HardwareId);
            foreach (var id in _monitors.Keys.ToArray())
            {
                if (byId.TryGetValue(id, out var monitor)) _monitors[id] = _monitors[id] with { Bounds = monitor.Bounds };
                else _monitors.Remove(id);
            }
        }

        public IReadOnlyList<string> TakeAwakenedMonitors(Point cursor)
        {
            var awakened = _monitors.Where(pair => pair.Value.Bounds.Contains(cursor) && pair.Value.CursorAtBlackout != cursor)
                .Select(pair => pair.Key).ToArray();
            foreach (var id in awakened) _monitors.Remove(id);
            return awakened;
        }

        private readonly record struct WakeState(Rect Bounds, Point CursorAtBlackout);
    }
}
