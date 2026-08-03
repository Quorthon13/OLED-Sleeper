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

        /// <summary>Number of times a restore write is attempted before the recording is kept and the attempt abandoned.</summary>
        private const int RestoreAttempts = 3;

        /// <summary>Delay between restore attempts.</summary>
        private const int RestoreRetryDelayMs = 200;

        /// <summary>Largest difference between the requested and read-back brightness that still counts as applied.</summary>
        private const uint BrightnessReadBackTolerance = 2;

        /// <summary>Returned by <see cref="GetCurrentBrightness"/> when the monitor could not be read.</summary>
        private const uint BrightnessUnknown = uint.MaxValue;

        /// <summary>Upper bound of the percentage scale that the settings and the UI express the dim level on.</summary>
        private const uint DimLevelPercentageMax = 100;

        /// <summary>Guards <see cref="_originalBrightnessLevels"/> and the state file write that follows each change to it.</summary>
        private readonly object _stateLock = new();

        private readonly Dictionary<string, uint> _originalBrightnessLevels;

        /// <summary>
        /// One gate per hardware ID. Dim, undim and restore for the same monitor run one at a time.
        /// </summary>
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _monitorGates = new();

        /// <summary>
        /// Loads the recorded original brightness levels from disk.
        /// </summary>
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
        /// Result of an operation that needs a physical monitor handle.
        /// </summary>
        private enum MonitorAccessOutcome
        {
            /// <summary>The monitor handle could not be found or opened. Nothing was written.</summary>
            Unavailable,

            /// <summary>The operation ran against the monitor and did not succeed.</summary>
            Failed,

            /// <summary>The operation ran against the monitor and succeeded.</summary>
            Succeeded
        }

        /// <summary>
        /// Scales the dim level into the monitor's brightness range, reads the current brightness,
        /// records it as the original, then writes the scaled value.
        /// </summary>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        /// <param name="dimLevel">The dim level as a percentage.</param>
        private async Task DimCoreAsync(string hardwareId, int dimLevel)
        {
            var targetBrightness = await ScaleToMonitorRangeAsync(hardwareId, dimLevel);

            await WithPhysicalMonitorAsync(hardwareId, hPhysicalMonitor =>
            {
                var currentBrightness = GetCurrentBrightness(hPhysicalMonitor, hardwareId);
                if (currentBrightness == BrightnessUnknown) return false;

                RecordOriginalBrightness(hardwareId, currentBrightness);
                return SetMonitorBrightness(hPhysicalMonitor, hardwareId, targetBrightness);
            });
        }

        /// <summary>
        /// Converts a dim level percentage into a raw brightness value using the range the monitor reported
        /// when it was last probed.
        /// </summary>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        /// <param name="dimLevel">The dim level as a percentage.</param>
        /// <returns>The value to write to the monitor.</returns>
        private async Task<uint> ScaleToMonitorRangeAsync(string hardwareId, int dimLevel)
        {
            var monitors = await _monitorManager.GetCurrentMonitorsAsync();
            var maxBrightness = monitors.FirstOrDefault(m => m.HardwareId == hardwareId)?.MaxBrightness ?? 0;

            var targetBrightness = ScaleToRange(dimLevel, maxBrightness);
            if (targetBrightness != dimLevel)
            {
                Log.Debug("Dim level {DimLevel}% scales to brightness {TargetBrightness} on monitor {HardwareId}, whose range is 0-{MaxBrightness}.",
                    dimLevel, targetBrightness, hardwareId, maxBrightness);
            }

            return targetBrightness;
        }

        /// <summary>
        /// Maps a percentage onto a monitor's brightness range.
        /// </summary>
        /// <param name="dimLevelPercentage">The dim level as a percentage. Values outside 0-100 are clamped.</param>
        /// <param name="maxBrightness">The monitor's highest accepted brightness value.</param>
        /// <returns>
        /// The percentage itself when the monitor reported no range or already runs on a 0-100 scale;
        /// otherwise the percentage of <paramref name="maxBrightness"/>.
        /// </returns>
        private static uint ScaleToRange(int dimLevelPercentage, uint maxBrightness)
        {
            var percentage = (uint)Math.Clamp(dimLevelPercentage, 0, (int)DimLevelPercentageMax);
            if (maxBrightness == 0 || maxBrightness == DimLevelPercentageMax) return percentage;

            return (uint)Math.Round(percentage * maxBrightness / (double)DimLevelPercentageMax, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Sets the monitor back to its recorded original brightness and drops the recording.
        /// Does nothing if there is no recording. The recording is kept when the restore could not be confirmed,
        /// so a later reconnect, settings save, or the next launch retries it.
        /// </summary>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        private async Task UndimCoreAsync(string hardwareId)
        {
            uint originalBrightness;
            lock (_stateLock)
            {
                if (!_originalBrightnessLevels.TryGetValue(hardwareId, out originalBrightness)) return;
            }

            if (await RestoreCoreAsync(hardwareId, originalBrightness))
            {
                RemoveOriginalBrightness(hardwareId);
            }
        }

        /// <summary>
        /// Writes the given brightness value to the monitor and reads it back to confirm it was applied.
        /// Retries up to <see cref="RestoreAttempts"/> times while the monitor is reachable.
        /// </summary>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        /// <param name="originalBrightness">The brightness value to restore.</param>
        /// <returns>True when the monitor reported the restored value; otherwise, false.</returns>
        private async Task<bool> RestoreCoreAsync(string hardwareId, uint originalBrightness)
        {
            for (var attempt = 1; attempt <= RestoreAttempts; attempt++)
            {
                var outcome = await WithPhysicalMonitorAsync(hardwareId, hPhysicalMonitor =>
                    SetMonitorBrightness(hPhysicalMonitor, hardwareId, originalBrightness, isRestore: true)
                    && BrightnessWasApplied(hPhysicalMonitor, hardwareId, originalBrightness));

                if (outcome == MonitorAccessOutcome.Succeeded)
                {
                    Log.Information("Restored original brightness {OriginalBrightness} for monitor {HardwareId}.", originalBrightness, hardwareId);
                    return true;
                }

                if (outcome == MonitorAccessOutcome.Unavailable)
                {
                    Log.Warning("Monitor {HardwareId} is unreachable. Keeping its recorded brightness {OriginalBrightness} for a later attempt.",
                        hardwareId, originalBrightness);
                    return false;
                }

                if (attempt < RestoreAttempts)
                {
                    await Task.Delay(RestoreRetryDelayMs);
                }
            }

            Log.Warning("Brightness {OriginalBrightness} was not applied on monitor {HardwareId} after {Attempts} attempts. Keeping the recording.",
                originalBrightness, hardwareId, RestoreAttempts);
            return false;
        }

        /// <summary>
        /// Reads the monitor's brightness back and compares it against the value that was written.
        /// </summary>
        /// <param name="hPhysicalMonitor">The physical monitor handle.</param>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        /// <param name="expectedBrightness">The brightness value that was written.</param>
        /// <returns>True when the read-back is within <see cref="BrightnessReadBackTolerance"/> of the written value.</returns>
        private bool BrightnessWasApplied(nint hPhysicalMonitor, string hardwareId, uint expectedBrightness)
        {
            var actualBrightness = GetCurrentBrightness(hPhysicalMonitor, hardwareId);
            if (actualBrightness == BrightnessUnknown) return false;

            var difference = actualBrightness > expectedBrightness
                ? actualBrightness - expectedBrightness
                : expectedBrightness - actualBrightness;

            if (difference <= BrightnessReadBackTolerance) return true;

            Log.Warning("Monitor {HardwareId} reports brightness {ActualBrightness} after being set to {ExpectedBrightness}.",
                hardwareId, actualBrightness, expectedBrightness);
            return false;
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
        /// Safely obtains and destroys a physical monitor handle, executing the provided operation.
        /// </summary>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        /// <param name="operation">The operation to perform with the monitor handle. Returns whether it succeeded.</param>
        /// <returns><see cref="MonitorAccessOutcome.Unavailable"/> when the handle could not be obtained and the operation never ran.</returns>
        private async Task<MonitorAccessOutcome> WithPhysicalMonitorAsync(string hardwareId, Func<nint, bool> operation)
        {
            var hMonitor = await FindMonitorHandleByHardwareIdAsync(hardwareId);
            if (hMonitor == nint.Zero)
            {
                Log.Warning("Could not find monitor handle for HardwareId {HardwareId}.", hardwareId);
                return MonitorAccessOutcome.Unavailable;
            }

            var physicalMonitors = new NativeMethods.PHYSICAL_MONITOR[1];
            if (!NativeMethods.GetPhysicalMonitorsFromHMONITOR(hMonitor, 1, physicalMonitors))
            {
                Log.Warning("Could not get physical monitor from HMONITOR for HardwareId {HardwareId}.", hardwareId);
                return MonitorAccessOutcome.Unavailable;
            }

            var hPhysicalMonitor = physicalMonitors[0].hPhysicalMonitor;
            try
            {
                return operation(hPhysicalMonitor) ? MonitorAccessOutcome.Succeeded : MonitorAccessOutcome.Failed;
            }
            finally
            {
                NativeMethods.DestroyPhysicalMonitors(1, physicalMonitors);
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
        /// Gets the current brightness of the monitor.
        /// </summary>
        /// <param name="hPhysicalMonitor">The physical monitor handle.</param>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        /// <returns>The current brightness, or <see cref="BrightnessUnknown"/> if the read failed.</returns>
        private uint GetCurrentBrightness(nint hPhysicalMonitor, string hardwareId)
        {
            if (NativeMethods.GetVCPFeatureAndVCPFeatureReply(hPhysicalMonitor, NativeMethods.VCP_CODE_BRIGHTNESS, nint.Zero, out var currentBrightness, out _))
            {
                return currentBrightness;
            }
            Log.Warning("Failed to get current brightness for monitor {HardwareId}.", hardwareId);
            return BrightnessUnknown;
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
        /// <returns>True when the monitor accepted the write; otherwise, false.</returns>
        private bool SetMonitorBrightness(nint hPhysicalMonitor, string hardwareId, uint brightness, bool isRestore = false)
        {
            if (!NativeMethods.SetVCPFeature(hPhysicalMonitor, NativeMethods.VCP_CODE_BRIGHTNESS, brightness))
            {
                var action = isRestore ? "restore brightness on" : "dim";
                Log.Warning("Failed to {Action} monitor {HardwareId}.", action, hardwareId);
                return false;
            }

            if (isRestore)
            {
                Log.Debug("Wrote original brightness {OriginalBrightness} to monitor {HardwareId}, pending read-back.", brightness, hardwareId);
            }
            else
            {
                Log.Information("Successfully dimmed monitor {HardwareId} to brightness {Brightness}.", hardwareId, brightness);
            }

            return true;
        }

        #endregion Private Helpers
    }
}
