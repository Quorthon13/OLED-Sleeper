using OLED_Sleeper.Models;
using Serilog;
using System.Collections.Generic;

namespace OLED_Sleeper.Services
{
    public class MonitorManagerService : IMonitorManagerService
    {
        private readonly IMonitorService _monitorService;
        private List<MonitorInfo> _cachedMonitors;
        private readonly object _lock = new object();

        public MonitorManagerService(IMonitorService monitorService)
        {
            _monitorService = monitorService;
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
            _cachedMonitors = _monitorService.GetMonitors();
        }
    }
}