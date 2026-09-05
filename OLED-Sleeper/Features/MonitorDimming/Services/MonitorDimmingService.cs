using OLED_Sleeper.Features.MonitorDimming.Helpers;
using OLED_Sleeper.Features.MonitorDimming.Services.Interfaces;
using OLED_Sleeper.Features.MonitorInformation.Services.Interfaces;
using Serilog;
using System.Collections.Concurrent;

namespace OLED_Sleeper.Features.MonitorDimming.Services
{
    /// <summary>
    /// Provides services for dimming and restoring monitor brightness using DDC/CI.
    /// Every public method is safe to call from any thread.
    /// </summary>
    public class MonitorDimmingService : IMonitorDimmingService
    {
        private readonly IMonitorInfoManager _monitorManager;
        private readonly IDdcCiAccess _ddcCiAccess;
        private readonly IOriginalBrightnessStore _originalBrightnessStore;

        /// <summary>Number of times a restore write is attempted before the recording is kept and the attempt abandoned.</summary>
        private const int RestoreAttempts = 3;

        /// <summary>Delay between restore attempts.</summary>
        private const int RestoreRetryDelayMs = 200;

        /// <summary>Largest difference between the requested and read-back brightness that still counts as applied.</summary>
        private const uint BrightnessReadBackTolerance = 2;

        /// <summary>
        /// One gate per hardware ID. Dim, undim and restore for the same monitor run one at a time.
        /// </summary>
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _monitorGates = new();

        public MonitorDimmingService(
            IMonitorInfoManager monitorManager,
            IDdcCiAccess ddcCiAccess,
            IOriginalBrightnessStore originalBrightnessStore)
        {
            _monitorManager = monitorManager;
            _ddcCiAccess = ddcCiAccess;
            _originalBrightnessStore = originalBrightnessStore;
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
            return _originalBrightnessStore.GetAll();
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

            await WithSessionAsync(hardwareId, session =>
            {
                var currentBrightness = GetCurrentBrightness(session, hardwareId);
                if (currentBrightness is null) return false;

                _originalBrightnessStore.RecordOriginal(hardwareId, currentBrightness.Value);
                return SetMonitorBrightness(session, hardwareId, targetBrightness);
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
            var maxBrightness = monitors.FirstOrDefault(m => m.HardwareId == hardwareId)?.Capabilities?.MaxBrightness ?? 0;

            var targetBrightness = BrightnessScale.ToRawBrightness(dimLevel, maxBrightness);
            if (targetBrightness != dimLevel)
            {
                Log.Debug("Dim level {DimLevel}% scales to brightness {TargetBrightness} on monitor {HardwareId}, whose range is 0-{MaxBrightness}.",
                    dimLevel, targetBrightness, hardwareId, maxBrightness);
            }

            return targetBrightness;
        }

        /// <summary>
        /// Sets the monitor back to its recorded original brightness and drops the recording.
        /// Does nothing if there is no recording. The recording is kept when the restore could not be confirmed,
        /// so a later reconnect, settings save, or the next launch retries it.
        /// </summary>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        private async Task UndimCoreAsync(string hardwareId)
        {
            if (!_originalBrightnessStore.TryGetOriginal(hardwareId, out var originalBrightness)) return;

            if (await RestoreCoreAsync(hardwareId, originalBrightness))
            {
                _originalBrightnessStore.RemoveOriginal(hardwareId);
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
                var outcome = await WithSessionAsync(hardwareId, session =>
                    SetMonitorBrightness(session, hardwareId, originalBrightness, isRestore: true)
                    && BrightnessWasApplied(session, hardwareId, originalBrightness));

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
        /// <param name="session">The open channel to the monitor.</param>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        /// <param name="expectedBrightness">The brightness value that was written.</param>
        /// <returns>True when the read-back is within <see cref="BrightnessReadBackTolerance"/> of the written value.</returns>
        private static bool BrightnessWasApplied(IDdcCiSession session, string hardwareId, uint expectedBrightness)
        {
            var actualBrightness = GetCurrentBrightness(session, hardwareId);
            if (actualBrightness is null) return false;

            var difference = actualBrightness.Value > expectedBrightness
                ? actualBrightness.Value - expectedBrightness
                : expectedBrightness - actualBrightness.Value;

            if (difference <= BrightnessReadBackTolerance) return true;

            Log.Warning("Monitor {HardwareId} reports brightness {ActualBrightness} after being set to {ExpectedBrightness}.",
                hardwareId, actualBrightness.Value, expectedBrightness);
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
        /// Opens a DDC/CI channel to the monitor for the duration of the operation and closes it afterwards.
        /// This is the only place a channel is opened.
        /// </summary>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        /// <param name="operation">The operation to perform on the channel. Returns whether it succeeded.</param>
        /// <returns><see cref="MonitorAccessOutcome.Unavailable"/> when the monitor could not be reached and the operation never ran.</returns>
        private async Task<MonitorAccessOutcome> WithSessionAsync(string hardwareId, Func<IDdcCiSession, bool> operation)
        {
            var monitors = await _monitorManager.GetCurrentMonitorsAsync();
            var targetMonitor = monitors.FirstOrDefault(m => m.HardwareId == hardwareId);

            using var session = targetMonitor is null ? null : _ddcCiAccess.OpenSession(targetMonitor.DeviceName);
            if (session is null)
            {
                Log.Warning("Could not find monitor handle for HardwareId {HardwareId}.", hardwareId);
                return MonitorAccessOutcome.Unavailable;
            }

            return operation(session) ? MonitorAccessOutcome.Succeeded : MonitorAccessOutcome.Failed;
        }

        /// <summary>
        /// Gets the current brightness of the monitor.
        /// </summary>
        /// <param name="session">The open channel to the monitor.</param>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        /// <returns>The current brightness, or null if the read failed.</returns>
        private static uint? GetCurrentBrightness(IDdcCiSession session, string hardwareId)
        {
            var currentBrightness = session.GetBrightness();
            if (currentBrightness is null)
            {
                Log.Warning("Failed to get current brightness for monitor {HardwareId}.", hardwareId);
            }

            return currentBrightness;
        }

        /// <summary>
        /// Sets the brightness of the monitor and logs the operation.
        /// </summary>
        /// <param name="session">The open channel to the monitor.</param>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        /// <param name="brightness">The brightness value to set.</param>
        /// <param name="isRestore">True if restoring, false if dimming.</param>
        /// <returns>True when the monitor accepted the write; otherwise, false.</returns>
        private static bool SetMonitorBrightness(IDdcCiSession session, string hardwareId, uint brightness, bool isRestore = false)
        {
            if (!session.SetBrightness(brightness))
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
