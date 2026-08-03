using OLED_Sleeper.Core.Interfaces;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.MonitorInformation.Services.Interfaces;
using OLED_Sleeper.Features.MonitorState.Commands;
using OLED_Sleeper.Features.MonitorState.Services.Interfaces;
using Serilog;
using System.Timers;
using Timer = System.Timers.Timer;

namespace OLED_Sleeper.Features.MonitorState.Services
{
    /// <summary>
    /// Monitors the set of connected displays and dispatches synchronization commands when changes are detected.
    /// This class polls the system for monitor changes and uses the mediator pattern to notify the application of state changes.
    /// </summary>
    public class MonitorStateWatcher : IMonitorStateWatcher
    {
        #region Fields

        private readonly IMonitorInfoManager _monitorInfoManager;
        private readonly IMediator _mediator;
        private readonly Timer _pollTimer;
        private readonly object _lock = new();
        private IReadOnlyList<MonitorInfo> _lastKnownMonitors = Array.Empty<MonitorInfo>();
        private IReadOnlyList<MonitorInfo>? _pendingMonitors;
        private int _pollInProgress;
        private volatile bool _isStopped;

        #endregion Fields

        #region Constructor

        /// <param name="pollIntervalMs">Polling interval in milliseconds. Default is 2000ms.</param>
        public MonitorStateWatcher(IMonitorInfoManager monitorInfoManager, IMediator mediator, double pollIntervalMs = 2000)
        {
            _monitorInfoManager = monitorInfoManager;
            _mediator = mediator;
            _pollTimer = new Timer(pollIntervalMs) { AutoReset = true };
            _pollTimer.Elapsed += PollTimerElapsed;
        }

        #endregion Constructor

        #region Public Methods

        /// <summary>
        /// Starts monitoring for monitor state changes. The initial monitor list is retrieved and the timer is started.
        /// </summary>
        public void Start()
        {
            lock (_lock)
            {
                if (_pollTimer.Enabled) return;
            }

            _isStopped = false;
            _ = RetrieveInitialMonitorListAsync();
        }

        /// <summary>
        /// Stops monitoring for monitor state changes. A poll already in flight runs to completion but does
        /// not dispatch a synchronization.
        /// </summary>
        public void Stop()
        {
            _isStopped = true;

            lock (_lock)
            {
                _pollTimer.Stop();
            }
        }

        /// <summary>
        /// Releases resources used by the watcher.
        /// </summary>
        public void Dispose()
        {
            _pollTimer?.Dispose();
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Retrieves the initial monitor list and starts the polling timer.
        /// Dispatches a synchronization command for the initial state.
        /// </summary>
        private async Task RetrieveInitialMonitorListAsync()
        {
            try
            {
                var monitors = await _monitorInfoManager.GetCurrentMonitorsAsync();

                if (_isStopped) return;

                lock (_lock)
                {
                    _lastKnownMonitors = monitors;
                }

                await _mediator.SendAsync(new SynchronizeMonitorStateCommand([], monitors));

                lock (_lock)
                {
                    if (_isStopped) return;
                    _pollTimer.Start();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to retrieve the initial monitor list. Monitor state watching is not active.");
            }
        }

        /// <summary>
        /// Polls for monitor changes and dispatches a synchronization command if a change is detected.
        /// A tick that arrives while the previous one is still working is dropped, not queued.
        /// </summary>
        private void PollTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (_isStopped) return;

            if (Interlocked.CompareExchange(ref _pollInProgress, 1, 0) != 0)
            {
                Log.Debug("Skipping a monitor poll because the previous one is still running.");
                return;
            }

            _ = PollForChangesAsync();
        }

        /// <summary>
        /// Reads the current display set, and when a change is confirmed, refreshes the shared monitor cache
        /// and dispatches a synchronization command carrying the freshly enriched list.
        /// <see cref="_lock"/> covers only the comparison and the swap of <see cref="_lastKnownMonitors"/>;
        /// neither the refresh nor the mediator dispatch runs under it.
        /// </summary>
        private async Task PollForChangesAsync()
        {
            try
            {
                var currentMonitors = _monitorInfoManager.GetLatestMonitorsBasicInfo();

                if (currentMonitors.Count == 0)
                {
                    Log.Debug("Monitor poll returned an empty display set. Treating it as transient.");
                    ClearPendingChange();
                    return;
                }

                if (!IsChangeConfirmed(currentMonitors)) return;

                var refreshedMonitors = await _monitorInfoManager.RefreshMonitorsAsync();

                if (refreshedMonitors.Count == 0)
                {
                    Log.Warning("Monitor re-scan returned an empty display set. Keeping the last known monitor list.");
                    return;
                }

                if (_isStopped)
                {
                    Log.Debug("The watcher was stopped during this poll. Skipping synchronization.");
                    return;
                }

                IReadOnlyList<MonitorInfo> oldMonitors;
                lock (_lock)
                {
                    oldMonitors = _lastKnownMonitors;
                    _lastKnownMonitors = refreshedMonitors;
                }

                await DispatchSynchronizationAsync(oldMonitors, refreshedMonitors);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to poll for display changes.");
            }
            finally
            {
                Interlocked.Exchange(ref _pollInProgress, 0);
            }
        }

        /// <summary>
        /// Determines whether a detected difference from the last known monitor list is stable enough to act on.
        /// </summary>
        /// <param name="currentMonitors">The display set read by this poll.</param>
        /// <returns>True if the same change has now been observed twice in a row; otherwise, false.</returns>
        private bool IsChangeConfirmed(IReadOnlyList<MonitorInfo> currentMonitors)
        {
            lock (_lock)
            {
                if (AreMonitorListsEqual(_lastKnownMonitors, currentMonitors))
                {
                    _pendingMonitors = null;
                    return false;
                }

                if (!AreMonitorListsEqual(_pendingMonitors, currentMonitors))
                {
                    Log.Debug("Display change observed. Waiting for a second matching reading before acting.");
                    _pendingMonitors = currentMonitors;
                    return false;
                }

                _pendingMonitors = null;
                return true;
            }
        }

        /// <summary>
        /// Discards any unconfirmed change so a transient reading cannot later be mistaken for a confirmation.
        /// </summary>
        private void ClearPendingChange()
        {
            lock (_lock)
            {
                _pendingMonitors = null;
            }
        }

        /// <summary>
        /// Dispatches a synchronization command for a detected monitor change, logging any failure.
        /// </summary>
        /// <param name="oldMonitors">The previously known monitor list.</param>
        /// <param name="currentMonitors">The newly detected monitor list.</param>
        private async Task DispatchSynchronizationAsync(IReadOnlyList<MonitorInfo> oldMonitors, IReadOnlyList<MonitorInfo> currentMonitors)
        {
            try
            {
                await _mediator.SendAsync(new SynchronizeMonitorStateCommand(oldMonitors, currentMonitors));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to synchronize monitor state after a display change.");
            }
        }

        /// <summary>
        /// Compares two monitor lists for equality by device name and per-monitor geometry.
        /// </summary>
        /// <param name="a">First monitor list.</param>
        /// <param name="b">Second monitor list.</param>
        /// <returns>True if the lists describe the same displays with the same geometry; otherwise, false.</returns>
        /// <remarks>
        /// <see cref="MonitorInfo.HardwareId"/> is not compared — the lists come from
        /// <c>GetLatestMonitorsBasicInfo</c>, which does not populate it. A port swap that puts a different
        /// panel under the same device name with identical geometry is therefore not detected.
        /// </remarks>
        private static bool AreMonitorListsEqual(IReadOnlyList<MonitorInfo>? a, IReadOnlyList<MonitorInfo>? b)
        {
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;

            var byDeviceName = new Dictionary<string, MonitorInfo>(a.Count);
            foreach (var monitor in a)
            {
                if (monitor.DeviceName == null) return false;
                if (!byDeviceName.TryAdd(monitor.DeviceName, monitor)) return false;
            }

            foreach (var monitor in b)
            {
                if (monitor.DeviceName == null) return false;
                if (!byDeviceName.TryGetValue(monitor.DeviceName, out var counterpart)) return false;
                if (!HasSameGeometry(monitor, counterpart)) return false;
            }

            return true;
        }

        /// <summary>
        /// Compares the geometry-bearing fields of two records describing the same device name.
        /// </summary>
        /// <param name="a">First monitor record.</param>
        /// <param name="b">Second monitor record.</param>
        /// <returns>True if both describe the same rectangle, scaling and role; otherwise, false.</returns>
        private static bool HasSameGeometry(MonitorInfo a, MonitorInfo b)
        {
            return a.Bounds == b.Bounds
                && a.Dpi == b.Dpi
                && a.IsPrimary == b.IsPrimary
                && a.DisplayNumber == b.DisplayNumber;
        }

        #endregion Private Methods
    }
}