using System.Runtime.InteropServices;
using OLED_Sleeper.Models;
using OLED_Sleeper.Native;
using OLED_Sleeper.Services.Monitor.Interfaces;
using Serilog;

namespace OLED_Sleeper.Services.Monitor
{
    /// <summary>
    /// Provides services for dimming and restoring monitor brightness using DDC/CI.
    /// </summary>
    public class MonitorDimmingService : IMonitorDimmingService // Assuming the interface is also updated to async
    {
        private readonly IMonitorInfoManager _monitorManager;
        private readonly IMonitorBrightnessStateService _brightnessStateService;
        private readonly Dictionary<string, uint> _originalBrightnessLevels;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorDimmingService"/> class.
        /// </summary>
        /// <param name="monitorManager">The monitor info manager.</param>
        /// <param name="brightnessStateService">The brightness state service.</param>
        public MonitorDimmingService(IMonitorInfoManager monitorManager, IMonitorBrightnessStateService brightnessStateService)
        {
            _monitorManager = monitorManager;
            _brightnessStateService = brightnessStateService;
            _originalBrightnessLevels = _brightnessStateService.LoadState();
        }

        /// <inheritdoc />
        public async Task DimMonitorAsync(string hardwareId, int dimLevel)
        {
            await WithPhysicalMonitorAsync(hardwareId, hPhysicalMonitor =>
            {
                var currentBrightness = GetCurrentBrightness(hPhysicalMonitor, hardwareId);
                if (currentBrightness == uint.MaxValue) return;
                SaveOriginalBrightness(hardwareId, currentBrightness);
                SetMonitorBrightness(hPhysicalMonitor, hardwareId, (uint)dimLevel);
            });
        }

        /// <inheritdoc />
        public async Task UndimMonitorAsync(string hardwareId)
        {
            if (_originalBrightnessLevels.TryGetValue(hardwareId, out var originalBrightness))
            {
                await RestoreBrightnessAsync(hardwareId, originalBrightness);
                RemoveOriginalBrightness(hardwareId);
            }
        }

        /// <inheritdoc />
        public async Task RestoreBrightnessAsync(string hardwareId, uint originalBrightness)
        {
            await WithPhysicalMonitorAsync(hardwareId, hPhysicalMonitor =>
            {
                SetMonitorBrightness(hPhysicalMonitor, hardwareId, originalBrightness, isRestore: true);
            });
        }

        /// <inheritdoc />
        public Dictionary<string, uint> GetDimmedMonitors() => new(_originalBrightnessLevels);

        #region Private Helpers

        /// <summary>
        /// Wraps the event-based monitor retrieval in a Task that can be awaited.
        /// </summary>
        /// <returns>A task that completes with the list of monitors.</returns>
        private Task<IReadOnlyList<MonitorInfo>> GetMonitorsAsync()
        {
            var tcs = new TaskCompletionSource<IReadOnlyList<MonitorInfo>>();

            EventHandler<IReadOnlyList<MonitorInfo>> handler = null;
            handler = (sender, monitors) =>
            {
                // Unsubscribe to prevent memory leaks.
                _monitorManager.MonitorListReady -= handler;
                // Set the result, which will complete the awaited Task.
                tcs.SetResult(monitors);
            };

            _monitorManager.MonitorListReady += handler;
            // This call starts the process that will eventually fire the MonitorListReady event.
            _monitorManager.GetCurrentMonitorsAsync();

            return tcs.Task;
        }

        /// <summary>
        /// Safely obtains and destroys a physical monitor handle, executing the provided action.
        /// </summary>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        /// <param name="action">The action to perform with the monitor handle.</param>
        private async Task WithPhysicalMonitorAsync(string hardwareId, Action<IntPtr> action)
        {
            var hMonitor = await FindMonitorHandleByHardwareIdAsync(hardwareId);
            if (hMonitor == IntPtr.Zero)
            {
                Log.Warning("Could not find monitor handle for HardwareId {HardwareId}.", hardwareId);
                return;
            }

            var physicalMonitors = new NativeMethods.PHYSICAL_MONITOR[1];
            if (NativeMethods.GetPhysicalMonitorsFromHMONITOR(hMonitor, 1, physicalMonitors))
            {
                var hPhysicalMonitor = physicalMonitors[0].hPhysicalMonitor;
                try
                {
                    action(hPhysicalMonitor);
                }
                finally
                {
                    NativeMethods.DestroyPhysicalMonitors(1, physicalMonitors);
                }
            }
            else
            {
                Log.Warning("Could not get physical monitor from HMONITOR for HardwareId {HardwareId}.", hardwareId);
            }
        }

        /// <summary>
        /// Finds the monitor handle (HMONITOR) for the given hardware ID.
        /// </summary>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        /// <returns>The HMONITOR handle, or IntPtr.Zero if not found.</returns>
        private async Task<IntPtr> FindMonitorHandleByHardwareIdAsync(string hardwareId)
        {
            var allMonitors = await GetMonitorsAsync();
            var targetMonitor = allMonitors.FirstOrDefault(m => m.HardwareId == hardwareId);
            if (targetMonitor == null) return IntPtr.Zero;

            IntPtr foundMonitor = IntPtr.Zero;
            NativeMethods.MonitorEnumProc callback = (IntPtr monitor, IntPtr hdc, ref NativeMethods.Rect rect, IntPtr data) =>
            {
                var mi = new NativeMethods.MonitorInfoEx { cbSize = Marshal.SizeOf(typeof(NativeMethods.MonitorInfoEx)) };
                if (NativeMethods.GetMonitorInfo(monitor, ref mi) && mi.szDevice == targetMonitor.DeviceName)
                {
                    foundMonitor = monitor;
                    return false; // Stop enumerating once we've found it
                }
                return true; // Continue enumerating
            };

            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
            return foundMonitor;
        }

        /// <summary>
        /// Gets the current brightness of the monitor, or uint.MaxValue if failed.
        /// </summary>
        /// <param name="hPhysicalMonitor">The physical monitor handle.</param>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        /// <returns>The current brightness, or uint.MaxValue if failed.</returns>
        private uint GetCurrentBrightness(IntPtr hPhysicalMonitor, string hardwareId)
        {
            if (NativeMethods.GetVCPFeatureAndVCPFeatureReply(hPhysicalMonitor, NativeMethods.VCP_CODE_BRIGHTNESS, IntPtr.Zero, out var currentBrightness, out _))
            {
                return currentBrightness;
            }
            Log.Warning("Failed to get current brightness for monitor {HardwareId}.", hardwareId);
            return uint.MaxValue;
        }

        /// <summary>
        /// Saves the original brightness for the monitor and persists the state.
        /// </summary>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        /// <param name="brightness">The brightness value to save.</param>
        private void SaveOriginalBrightness(string hardwareId, uint brightness)
        {
            _originalBrightnessLevels[hardwareId] = brightness;
            _brightnessStateService.SaveState(_originalBrightnessLevels);
        }

        /// <summary>
        /// Removes the original brightness entry for the monitor and persists the state.
        /// </summary>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        private void RemoveOriginalBrightness(string hardwareId)
        {
            _originalBrightnessLevels.Remove(hardwareId);
            _brightnessStateService.SaveState(_originalBrightnessLevels);
        }

        /// <summary>
        /// Sets the brightness of the monitor and logs the operation.
        /// </summary>
        /// <param name="hPhysicalMonitor">The physical monitor handle.</param>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        /// <param name="brightness">The brightness value to set.</param>
        /// <param name="isRestore">True if restoring, false if dimming.</param>
        private void SetMonitorBrightness(IntPtr hPhysicalMonitor, string hardwareId, uint brightness, bool isRestore = false)
        {
            if (NativeMethods.SetVCPFeature(hPhysicalMonitor, NativeMethods.VCP_CODE_BRIGHTNESS, brightness))
            {
                if (isRestore)
                {
                    Log.Information("Restored original brightness {OriginalBrightness} for monitor {HardwareId}.", brightness, hardwareId);
                }
                else
                {
                    Log.Information("Successfully dimmed monitor {HardwareId} to {DimLevel}%.", hardwareId, brightness);
                }
            }
            else
            {
                var action = isRestore ? "restore brightness on" : "dim";
                Log.Warning("Failed to {Action} monitor {HardwareId}.", action, hardwareId);
            }
        }

        #endregion Private Helpers
    }
}