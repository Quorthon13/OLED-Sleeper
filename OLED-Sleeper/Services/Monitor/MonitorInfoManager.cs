using OLED_Sleeper.Models;
using OLED_Sleeper.Services.Monitor.Interfaces;
using Serilog;

namespace OLED_Sleeper.Services.Monitor
{
    /// <summary>
    /// Manages monitor information, including caching and enrichment with DDC/CI support and hardware IDs.
    /// </summary>
    public class MonitorInfoManager : IMonitorInfoManager
    {
        #region Fields

        private readonly IMonitorInfoProvider _monitorInfoProvider;
        private List<MonitorInfo> _cachedMonitors;
        private readonly object _lock = new object();

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

        /// <summary>
        /// Gets the current list of monitors, from the cache if available.
        /// </summary>
        /// <returns>The current list of enriched <see cref="MonitorInfo"/> objects.</returns>
        public List<MonitorInfo> GetCurrentMonitors()
        {
            lock (_lock)
            {
                if (_cachedMonitors == null)
                {
                    Log.Information("Monitor cache is empty. Performing initial scan of monitors.");
                    RefreshMonitorsInternal();
                }
                return _cachedMonitors;
            }
        }

        /// <summary>
        /// Forces a refresh of the monitor list from the system.
        /// </summary>
        public void RefreshMonitors()
        {
            lock (_lock)
            {
                Log.Information("Manual refresh requested. Re-scanning monitors.");
                RefreshMonitorsInternal();
            }
        }

        /// <summary>
        /// Gets the latest, up-to-date list of monitors from the system (basic info only, no enrichment).
        /// </summary>
        /// <returns>The latest list of <see cref="MonitorInfo"/> objects (basic info only).</returns>
        public List<MonitorInfo> GetLatestMonitorsBasicInfo()
        {
            return _monitorInfoProvider.GetAllMonitorsBasicInfo();
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Refreshes the monitor cache by retrieving basic info and enriching each monitor with DDC/CI support and hardware ID.
        /// </summary>
        private void RefreshMonitorsInternal()
        {
            var monitors = _monitorInfoProvider.GetAllMonitorsBasicInfo();
            foreach (var monitor in monitors)
            {
                monitor.IsDdcCiSupported = _monitorInfoProvider.GetDdcCiSupport(monitor);
                monitor.HardwareId = _monitorInfoProvider.GetHardwareId(monitor);
            }
            _cachedMonitors = monitors;
        }

        #endregion Private Methods
    }
}