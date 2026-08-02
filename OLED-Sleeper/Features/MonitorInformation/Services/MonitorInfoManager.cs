using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.MonitorInformation.Services.Interfaces;
using Serilog;

namespace OLED_Sleeper.Features.MonitorInformation.Services
{
    /// <summary>
    /// Manages monitor information, including caching and enrichment with DDC/CI support and hardware IDs.
    /// </summary>
    /// <remarks>
    /// The lock here guards only the cached task reference. It is never held while running the native
    /// enumeration and never held while invoking a caller's code — callers observe completion by awaiting
    /// the returned task, on their own thread. Holding this lock across a callback is what previously
    /// allowed a lock-order inversion against <c>MonitorIdleDetectionService</c>'s lock, and a deadlock
    /// against a blocking <c>Dispatcher.Invoke</c>; keep it that way.
    /// </remarks>
    public class MonitorInfoManager : IMonitorInfoManager
    {
        #region Fields

        private readonly IMonitorInfoProvider _monitorInfoProvider;
        private readonly object _lock = new();
        private Task<IReadOnlyList<MonitorInfo>>? _cachedScan;

        #endregion Fields

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorInfoManager"/> class.
        /// </summary>
        /// <param name="monitorInfoProvider">The monitor info provider dependency.</param>
        public MonitorInfoManager(IMonitorInfoProvider monitorInfoProvider)
        {
            _monitorInfoProvider = monitorInfoProvider;
        }

        #endregion Constructor

        #region Public Methods

        /// <inheritdoc />
        public Task<IReadOnlyList<MonitorInfo>> GetCurrentMonitorsAsync()
        {
            lock (_lock)
            {
                return _cachedScan ??= StartScan();
            }
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<MonitorInfo>> RefreshMonitorsAsync()
        {
            Log.Information("Manual refresh requested. Re-scanning monitors.");
            lock (_lock)
            {
                return _cachedScan = StartScan();
            }
        }

        /// <inheritdoc />
        public List<MonitorInfo> GetLatestMonitorsBasicInfo()
        {
            return _monitorInfoProvider.GetAllMonitorsBasicInfo();
        }

        /// <inheritdoc />
        public void EnrichMonitorInfoList(List<MonitorInfo>? monitors)
        {
            if (monitors == null) return;
            foreach (var monitor in monitors)
            {
                monitor.IsDdcCiSupported = _monitorInfoProvider.GetDdcCiSupport(monitor);
                monitor.HardwareId = _monitorInfoProvider.GetHardwareId(monitor);
            }
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Starts a background scan of the system's monitors. Must be called while holding <see cref="_lock"/>.
        /// </summary>
        /// <remarks>
        /// A faulted scan is evicted from the cache rather than retained. Caching a faulted task would make
        /// every later call rethrow the original failure forever; dropping it lets the next caller retry the
        /// native enumeration. The fault continuation also observes the exception so it is logged rather than
        /// disappearing as an unobserved task exception.
        /// </remarks>
        private Task<IReadOnlyList<MonitorInfo>> StartScan()
        {
            var scan = Task.Run<IReadOnlyList<MonitorInfo>>(() =>
            {
                var monitors = _monitorInfoProvider.GetAllMonitorsBasicInfo();
                EnrichMonitorInfoList(monitors);
                return monitors;
            });

            _ = scan.ContinueWith(
                faulted =>
                {
                    Log.Error(faulted.Exception?.GetBaseException(),
                        "Monitor enumeration failed. Dropping the cached scan so the next request retries.");

                    lock (_lock)
                    {
                        if (ReferenceEquals(_cachedScan, faulted))
                        {
                            _cachedScan = null;
                        }
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);

            return scan;
        }

        #endregion Private Methods
    }
}
