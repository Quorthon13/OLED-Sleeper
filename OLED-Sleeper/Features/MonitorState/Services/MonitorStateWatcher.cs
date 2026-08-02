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

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorStateWatcher"/> class.
        /// </summary>
        /// <param name="monitorInfoManager">Service for querying current monitor information.</param>
        /// <param name="mediator">Mediator for dispatching monitor state commands.</param>
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
        /// </summary>
        /// <remarks>
        /// The timer is <c>AutoReset</c> at a shorter interval than a full monitor scan can take, so a tick
        /// that arrives while the previous one is still working is dropped rather than queued. Without this
        /// the poll would stack refreshes, each serialising DDC/CI probes on the same bus.
        /// </remarks>
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
        /// </summary>
        /// <remarks>
        /// Neither the refresh nor the mediator dispatch runs under <see cref="_lock"/>. The lock covers only
        /// the comparison and the swap of <see cref="_lastKnownMonitors"/>: a refresh performs slow native
        /// probing, and <c>SynchronizeMonitorStateCommandHandler</c> reaches back into the monitor manager and
        /// the dimming service, so holding this lock across either would reintroduce a lock held across
        /// foreign code.
        /// </remarks>
        private async Task PollForChangesAsync()
        {
            try
            {
                var currentMonitors = _monitorInfoManager.GetLatestMonitorsBasicInfo();

                if (currentMonitors.Count == 0)
                {
                    // A scan taken mid mode-change can succeed and still report nothing attached. An empty
                    // list never compares equal to the last known one, so it must be rejected explicitly or
                    // it would be acted on as though every monitor had been unplugged.
                    Log.Debug("Monitor poll returned an empty display set. Treating it as transient.");
                    ClearPendingChange();
                    return;
                }

                if (!IsChangeConfirmed(currentMonitors)) return;

                // Refresh the shared cache rather than enriching a private copy: the overlay handler and the
                // settings UI read their geometry from that cache, and nothing else ever invalidates it.
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
        /// <remarks>
        /// A display mode change is not atomic — mid-transition polls can observe geometry that exists for a
        /// few hundred milliseconds and never again. Requiring two consecutive identical readings costs one
        /// extra poll interval of latency and avoids synchronizing the whole application against a rectangle
        /// that has already stopped being true.
        /// </remarks>
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
        /// Comparing only the set of device names made every geometry change invisible to the watcher: a
        /// resolution change, a rearrangement in Windows display settings and a DPI/scaling change all
        /// preserve both the count and the names, so no synchronization was ever dispatched and the rest of
        /// the application kept acting on the bounds it had cached at startup.
        /// <para>
        /// <see cref="MonitorInfo.HardwareId"/> is deliberately *not* compared. This runs on every poll, and
        /// the lists reaching it come from <c>GetLatestMonitorsBasicInfo</c>, which does not populate it —
        /// obtaining it means a nested <c>EnumDisplayDevices</c> walk per monitor. The residual gap is a port
        /// swap that substitutes a different panel under the same device name with identical geometry and no
        /// observable intermediate state; unplugging and replugging passes through a changed display set and
        /// is detected normally.
        /// </para>
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