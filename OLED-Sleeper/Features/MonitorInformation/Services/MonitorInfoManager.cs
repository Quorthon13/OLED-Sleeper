using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.MonitorInformation.Services.Interfaces;
using Serilog;

namespace OLED_Sleeper.Features.MonitorInformation.Services
{
    /// <summary>
    /// Manages monitor information, including caching and enrichment with DDC/CI support and hardware IDs.
    /// </summary>
    public class MonitorInfoManager : IMonitorInfoManager
    {
        #region Fields

        private readonly IMonitorInfoProvider _monitorInfoProvider;
        private readonly object _lock = new();
        private Task<IReadOnlyList<MonitorInfo>>? _cachedScan;
        private Task<IReadOnlyList<MonitorInfo>>? _inFlightRefresh;

        #endregion Fields

        #region Constructor

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
        /// <remarks>
        /// Callers arriving while a scan is running share that scan rather than starting another.
        /// </remarks>
        public Task<IReadOnlyList<MonitorInfo>> RefreshMonitorsAsync()
        {
            lock (_lock)
            {
                if (_inFlightRefresh is { IsCompleted: false })
                {
                    Log.Debug("Refresh requested while a scan is already running. Reusing the in-flight scan.");
                    return _inFlightRefresh;
                }

                Log.Information("Refresh requested. Re-scanning monitors.");
                return _cachedScan = _inFlightRefresh = StartScan();
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
                var capabilities = _monitorInfoProvider.GetDdcCiCapabilities(monitor);
                monitor.IsDdcCiSupported = capabilities.IsSupported;
                monitor.MaxBrightness = capabilities.MaxBrightness;
                monitor.HardwareId = _monitorInfoProvider.GetHardwareId(monitor);
            }
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Starts a background scan of the system's monitors. Must be called while holding <see cref="_lock"/>.
        /// A faulted scan is dropped from the cache so the next caller retries.
        /// </summary>
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
