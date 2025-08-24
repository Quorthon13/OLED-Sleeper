// File: Services/IdleActivityService.cs
using OLED_Sleeper.Models;
using OLED_Sleeper.Native;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OLED_Sleeper.Services
{
    public class IdleActivityService : IIdleActivityService
    {
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private List<MonitorSettings> _managedMonitors = new List<MonitorSettings>();
        private readonly Dictionary<string, bool> _idleStateNotified = new Dictionary<string, bool>();
        private readonly object _lock = new object();

        public void Start()
        {
            Task.Run(() => IdleCheckLoop(_cancellationTokenSource.Token));
            Log.Information("IdleActivityService started.");
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            Log.Information("IdleActivityService stopped.");
        }

        public void UpdateSettings(List<MonitorSettings> monitorSettings)
        {
            lock (_lock)
            {
                _managedMonitors = monitorSettings.Where(s => s.IsManaged).ToList();
                // Clear notified states as settings may have changed
                _idleStateNotified.Clear();
            }
            Log.Information("IdleActivityService settings updated. Tracking {Count} monitors.", _managedMonitors.Count);
        }

        private async Task IdleCheckLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    uint systemIdleTime = GetSystemIdleTimeMilliseconds();

                    lock (_lock)
                    {
                        foreach (var monitor in _managedMonitors)
                        {
                            bool hasBeenNotified = _idleStateNotified.ContainsKey(monitor.HardwareId);

                            if (systemIdleTime >= monitor.IdleTimeMilliseconds)
                            {
                                if (!hasBeenNotified)
                                {
                                    Log.Information("Monitor {HardwareId} has exceeded its idle timer of {IdleTime}ms.",
                                        monitor.HardwareId, monitor.IdleTimeMilliseconds);
                                    _idleStateNotified[monitor.HardwareId] = true;
                                }
                            }
                            else
                            {
                                if (hasBeenNotified)
                                {
                                    // User is active again, reset notification state
                                    _idleStateNotified.Remove(monitor.HardwareId);
                                    Log.Information("User activity detected. Resetting idle state for monitor {HardwareId}.", monitor.HardwareId);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error in IdleCheckLoop.");
                }

                await Task.Delay(1000, token); // Check once per second
            }
        }

        private static uint GetSystemIdleTimeMilliseconds()
        {
            var lastInputInfo = new NativeMethods.LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(lastInputInfo);
            if (NativeMethods.GetLastInputInfo(ref lastInputInfo))
            {
                uint lastInputTick = lastInputInfo.dwTime;
                uint currentTick = (uint)Environment.TickCount;
                return currentTick - lastInputTick;
            }
            return 0;
        }
    }
}