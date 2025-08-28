using System.Timers;
using OLED_Sleeper.Models;
using Timer = System.Timers.Timer;
using OLED_Sleeper.Services.Monitor.Info.Interfaces;
using OLED_Sleeper.Services.Core.Interfaces;

namespace OLED_Sleeper.Services.Core
{
    /// <summary>
    /// Periodically polls the current state of connected monitors and raises an event when the set of monitors changes.
    /// </summary>
    public class MonitorStateWatcher : IMonitorStateWatcher, IDisposable
    {
        private readonly IMonitorInfoManager _monitorInfoManager;
        private readonly Timer _pollTimer;
        private IReadOnlyList<MonitorInfo> _lastKnownMonitors;
        private readonly object _lock = new();

        /// <summary>
        /// Occurs when the set of connected monitors changes.
        /// </summary>
        public event EventHandler<IReadOnlyList<MonitorInfo>> MonitorsChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorStateWatcher"/> class.
        /// </summary>
        /// <param name="monitorInfoManager">The monitor info manager to query for monitor state.</param>
        /// <param name="pollIntervalMs">Polling interval in milliseconds. Default is 2000ms.</param>
        public MonitorStateWatcher(IMonitorInfoManager monitorInfoManager, double pollIntervalMs = 2000)
        {
            _monitorInfoManager = monitorInfoManager;
            _pollTimer = new Timer(pollIntervalMs)
            {
                AutoReset = true
            };
            _pollTimer.Elapsed += PollTimerElapsed;
        }

        /// <summary>
        /// Starts monitoring for monitor state changes.
        /// </summary>
        public void Start()
        {
            lock (_lock)
            {
                if (!_pollTimer.Enabled)
                {
                    GetCurrentMonitorsAsyncAndSetInitial();
                }
            }
        }

        private void GetCurrentMonitorsAsyncAndSetInitial()
        {
            EventHandler<IReadOnlyList<MonitorInfo>> handler = null;
            handler = (sender, monitors) =>
            {
                _monitorInfoManager.MonitorListReady -= handler;
                _lastKnownMonitors = monitors;
                MonitorsChanged?.Invoke(this, _lastKnownMonitors);
                _pollTimer.Start();
            };
            _monitorInfoManager.MonitorListReady += handler;
            _monitorInfoManager.GetCurrentMonitorsAsync();
        }

        /// <summary>
        /// Stops monitoring for monitor state changes.
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                _pollTimer.Stop();
            }
        }

        /// <summary>
        /// Handles the timer elapsed event to poll for monitor changes.
        /// </summary>
        private void PollTimerElapsed(object sender, ElapsedEventArgs e)
        {
            lock (_lock)
            {
                // Use the latest basic info, then enrich and compare
                var currentMonitors = _monitorInfoManager.GetLatestMonitorsBasicInfo();
                if (!AreMonitorListsEqual(_lastKnownMonitors, currentMonitors))
                {
                    EnrichMonitorInfoList(currentMonitors);
                    _lastKnownMonitors = currentMonitors;
                    MonitorsChanged?.Invoke(this, currentMonitors);
                }
            }
        }

        /// <summary>
        /// Compares two monitor lists for equality based on device name set and count.
        /// </summary>
        /// <param name="a">First monitor list.</param>
        /// <param name="b">Second monitor list.</param>
        /// <returns>True if the lists are equal; otherwise, false.</returns>
        private static bool AreMonitorListsEqual(IReadOnlyList<MonitorInfo>? a, IReadOnlyList<MonitorInfo>? b)
        {
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;
            var aNames = new HashSet<string>();
            var bNames = new HashSet<string>();
            foreach (var m in a) aNames.Add(m.DeviceName);
            foreach (var m in b) bNames.Add(m.DeviceName);
            return aNames.SetEquals(bNames);
        }

        /// <summary>
        /// Enriches a list of MonitorInfo objects with DDC/CI support and hardware ID.
        /// </summary>
        /// <param name="monitors">The list of monitors to enrich.</param>
        private void EnrichMonitorInfoList(List<MonitorInfo> monitors)
        {
            _monitorInfoManager.EnrichMonitorInfoList(monitors);
        }

        /// <summary>
        /// Disposes the watcher and releases resources.
        /// </summary>
        public void Dispose()
        {
            _pollTimer?.Dispose();
        }
    }
}