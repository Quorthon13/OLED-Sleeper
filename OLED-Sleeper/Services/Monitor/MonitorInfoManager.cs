using OLED_Sleeper.Models;
using OLED_Sleeper.Services.Monitor.Interfaces;
using Serilog;
using System.Collections.Generic;

namespace OLED_Sleeper.Services.Monitor
{
    public class MonitorInfoManager : IMonitorInfoManager
    {
        private readonly IMonitorInfoProvider _monitorInfoProvider;
        private List<MonitorInfo> _cachedMonitors;
        private readonly object _lock = new object();

        public MonitorInfoManager(IMonitorInfoProvider monitorInfoProvider)
        {
            _monitorInfoProvider = monitorInfoProvider;
        }

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

        public void RefreshMonitors()
        {
            lock (_lock)
            {
                Log.Information("Manual refresh requested. Re-scanning monitors.");
                RefreshMonitorsInternal();
            }
        }

        private void RefreshMonitorsInternal()
        {
            // This is now the ONLY place where GetMonitors() is called.
            _cachedMonitors = _monitorInfoProvider.GetMonitors();
        }
    }
}