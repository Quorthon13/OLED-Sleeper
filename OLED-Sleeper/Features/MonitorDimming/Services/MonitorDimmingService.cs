using OLED_Sleeper.Features.MonitorDimming.Services.Interfaces;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.MonitorInformation.Services.Interfaces;
using OLED_Sleeper.Native;
using Serilog;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace OLED_Sleeper.Features.MonitorDimming.Services
{
    /// <summary>
    /// Provides services for dimming and restoring monitor brightness using DDC/CI.
    /// Every public method is safe to call from any thread.
    /// </summary>
    public class MonitorDimmingService : IMonitorDimmingService
    {
        private readonly IMonitorInfoManager _monitorManager;
        private readonly IMonitorBrightnessStateService _brightnessStateService;

        /// <summary>Guards <see cref="_originalBrightnessLevels"/> and the state file write that follows each change to it.</summary>
        private readonly object _stateLock = new();

        private readonly Dictionary<string, uint> _originalBrightnessLevels;

        /// <summary>
        /// One gate per hardware ID. Dim, undim and restore for the same monitor run one at a time.
        /// </summary>
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _monitorGates = new();

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
        public async Task DimMonitorAsync(string? hardwareId, int dimLevel)
        {
            if (string.IsNullOrEmpty(hardwareId))
            {
                Log.Warning("DimMonitorAsync called without a hardware ID.");
                return;
            }

            await WithMonitorGateAsync(hardwareId, () => DimCoreAsync(hardwareId, dimLevel));
        }

        /// <inheritdoc />
        public async Task UndimMonitorAsync(string hardwareId)
        {
            if (string.IsNullOrEmpty(hardwareId))
            {
                Log.Warning("UndimMonitorAsync called without a hardware ID.");
                return;
            }

            await WithMonitorGateAsync(hardwareId, () => UndimCoreAsync(hardwareId));
        }

        /// <inheritdoc />
        public Dictionary<string, uint> GetDimmedMonitors()
        {
            lock (_stateLock)
            {
                return new Dictionary<string, uint>(_originalBrightnessLevels);
            }
        }

        #region Private Helpers

        /// <summary>
        /// Reads the current brightness, records it as the original, then sets the dim level.
        /// </summary>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        /// <param name="dimLevel">The brightness level to set.</param>
        private Task DimCoreAsync(string hardwareId, int dimLevel)
        {
            return WithPhysicalMonitorAsync(hardwareId, hPhysicalMonitor =>
            {
                var currentBrightness = GetCurrentBrightness(hPhysicalMonitor, hardwareId);
                if (currentBrightness == uint.MaxValue) return;

                RecordOriginalBrightness(hardwareId, currentBrightness);
                SetMonitorBrightness(hPhysicalMonitor, hardwareId, (uint)dimLevel);
            });
        }

        /// <summary>
        /// Sets the monitor back to its recorded original brightness and drops the recording.
        /// Does nothing if there is no recording.
        /// </summary>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        private async Task UndimCoreAsync(string hardwareId)
        {
            uint originalBrightness;
            lock (_stateLock)
            {
                if (!_originalBrightnessLevels.TryGetValue(hardwareId, out originalBrightness)) return;
            }

            await RestoreCoreAsync(hardwareId, originalBrightness);
            RemoveOriginalBrightness(hardwareId);
        }

        /// <summary>
        /// Sets the given brightness value on the monitor.
        /// </summary>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        /// <param name="originalBrightness">The brightness value to restore.</param>
        private Task RestoreCoreAsync(string hardwareId, uint originalBrightness)
        {
            return WithPhysicalMonitorAsync(hardwareId, hPhysicalMonitor =>
            {
                SetMonitorBrightness(hPhysicalMonitor, hardwareId, originalBrightness, isRestore: true);
            });
        }

        /// <summary>
        /// Runs an operation while holding the monitor's gate.
        /// </summary>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        /// <param name="operation">The operation to run.</param>
        private async Task WithMonitorGateAsync(string hardwareId, Func<Task> operation)
        {
            var gate = _monitorGates.GetOrAdd(hardwareId, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();
            try
            {
                await operation();
            }
            finally
            {
                gate.Release();
            }
        }

        /// <summary>
        /// Safely obtains and destroys a physical monitor handle, executing the provided action.
        /// </summary>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        /// <param name="action">The action to perform with the monitor handle.</param>
        private async Task WithPhysicalMonitorAsync(string hardwareId, Action<nint> action)
        {
            var hMonitor = await FindMonitorHandleByHardwareIdAsync(hardwareId);
            if (hMonitor == nint.Zero)
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
        private async Task<nint> FindMonitorHandleByHardwareIdAsync(string hardwareId)
        {
            var allMonitors = await _monitorManager.GetCurrentMonitorsAsync();
            var targetMonitor = allMonitors.FirstOrDefault(m => m.HardwareId == hardwareId);
            if (targetMonitor == null) return nint.Zero;

            nint foundMonitor = nint.Zero;
            NativeMethods.MonitorEnumProc callback = (IntPtr hMonitor, IntPtr hdcMonitor, ref NativeMethods.Rect lprcMonitor, IntPtr dwData) =>
            {
                var mi = new NativeMethods.MonitorInfoEx { cbSize = Marshal.SizeOf(typeof(NativeMethods.MonitorInfoEx)) };
                if (NativeMethods.GetMonitorInfo(hMonitor, ref mi) && mi.szDevice == targetMonitor.DeviceName)
                {
                    foundMonitor = hMonitor;
                    return false; // Stop enumerating once we've found it
                }
                return true; // Continue enumerating
            };

            NativeMethods.EnumDisplayMonitors(nint.Zero, nint.Zero, callback, nint.Zero);
            return foundMonitor;
        }

        /// <summary>
        /// Gets the current brightness of the monitor, or uint.MaxValue if failed.
        /// </summary>
        /// <param name="hPhysicalMonitor">The physical monitor handle.</param>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        /// <returns>The current brightness, or uint.MaxValue if failed.</returns>
        private uint GetCurrentBrightness(nint hPhysicalMonitor, string hardwareId)
        {
            if (NativeMethods.GetVCPFeatureAndVCPFeatureReply(hPhysicalMonitor, NativeMethods.VCP_CODE_BRIGHTNESS, nint.Zero, out var currentBrightness, out _))
            {
                return currentBrightness;
            }
            Log.Warning("Failed to get current brightness for monitor {HardwareId}.", hardwareId);
            return uint.MaxValue;
        }

        /// <summary>
        /// Records the original brightness for the monitor and saves the state.
        /// An existing entry is kept, not overwritten.
        /// </summary>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        /// <param name="brightness">The brightness value to record.</param>
        private void RecordOriginalBrightness(string hardwareId, uint brightness)
        {
            lock (_stateLock)
            {
                if (_originalBrightnessLevels.ContainsKey(hardwareId))
                {
                    Log.Debug("Monitor {HardwareId} already has a recorded original brightness of {Brightness}.", hardwareId, _originalBrightnessLevels[hardwareId]);
                    return;
                }

                _originalBrightnessLevels[hardwareId] = brightness;
                _brightnessStateService.SaveState(new Dictionary<string, uint>(_originalBrightnessLevels));
            }
        }

        /// <summary>
        /// Removes the original brightness entry for the monitor and saves the state.
        /// </summary>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        private void RemoveOriginalBrightness(string hardwareId)
        {
            lock (_stateLock)
            {
                if (!_originalBrightnessLevels.Remove(hardwareId)) return;
                _brightnessStateService.SaveState(new Dictionary<string, uint>(_originalBrightnessLevels));
            }
        }

        /// <summary>
        /// Sets the brightness of the monitor and logs the operation.
        /// </summary>
        /// <param name="hPhysicalMonitor">The physical monitor handle.</param>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        /// <param name="brightness">The brightness value to set.</param>
        /// <param name="isRestore">True if restoring, false if dimming.</param>
        private void SetMonitorBrightness(nint hPhysicalMonitor, string hardwareId, uint brightness, bool isRestore = false)
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
