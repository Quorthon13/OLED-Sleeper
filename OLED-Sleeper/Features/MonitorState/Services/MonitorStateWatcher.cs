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

            _ = RetrieveInitialMonitorListAsync();
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

                lock (_lock)
                {
                    _lastKnownMonitors = monitors;
                }

                await _mediator.SendAsync(new SynchronizeMonitorStateCommand([], monitors));
                _pollTimer.Start();
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
        /// The lock covers only the comparison and the swap of <see cref="_lastKnownMonitors"/>. The mediator
        /// dispatch runs outside it: <c>SynchronizeMonitorStateCommandHandler</c> reaches back into the
        /// monitor manager and the dimming service, so holding this lock across the dispatch would reintroduce
        /// a lock held across foreign code.
        /// </remarks>
        private void PollTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            IReadOnlyList<MonitorInfo> oldMonitors;
            List<MonitorInfo> currentMonitors;

            lock (_lock)
            {
                currentMonitors = _monitorInfoManager.GetLatestMonitorsBasicInfo();
                if (AreMonitorListsEqual(_lastKnownMonitors, currentMonitors)) return;

                EnrichMonitorInfoList(currentMonitors);
                oldMonitors = _lastKnownMonitors;
                _lastKnownMonitors = currentMonitors;
            }

            _ = DispatchSynchronizationAsync(oldMonitors, currentMonitors);
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
        /// Compares two monitor lists for equality based on device name set and count.
        /// </summary>
        /// <param name="a">First monitor list.</param>
        /// <param name="b">Second monitor list.</param>
        /// <returns>True if the lists are equal; otherwise, false.</returns>
        private static bool AreMonitorListsEqual(IReadOnlyList<MonitorInfo>? a, IReadOnlyList<MonitorInfo>? b)
        {
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;
            var aNames = new HashSet<string>(a.Select(m => m.DeviceName).OfType<string>());
            var bNames = new HashSet<string>(b.Select(m => m.DeviceName).OfType<string>());
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

        #endregion Private Methods
    }
}