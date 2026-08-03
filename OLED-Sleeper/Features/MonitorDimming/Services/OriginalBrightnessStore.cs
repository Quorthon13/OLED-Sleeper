using OLED_Sleeper.Features.MonitorDimming.Services.Interfaces;
using Serilog;

namespace OLED_Sleeper.Features.MonitorDimming.Services
{
    /// <summary>
    /// Keeps each dimmed monitor's pre-dim brightness in memory and mirrors every change to the state file.
    /// Every member is safe to call from any thread.
    /// </summary>
    public class OriginalBrightnessStore : IOriginalBrightnessStore
    {
        private readonly IMonitorBrightnessStateService _brightnessStateService;

        /// <summary>Guards <see cref="_originalBrightnessLevels"/> and the state file write that follows each change to it.</summary>
        private readonly object _stateLock = new();

        private readonly Dictionary<string, uint> _originalBrightnessLevels;

        /// <summary>
        /// Loads the recorded original brightness levels from disk.
        /// </summary>
        public OriginalBrightnessStore(IMonitorBrightnessStateService brightnessStateService)
        {
            _brightnessStateService = brightnessStateService;
            _originalBrightnessLevels = _brightnessStateService.LoadState();
        }

        /// <inheritdoc />
        public bool TryGetOriginal(string hardwareId, out uint brightness)
        {
            lock (_stateLock)
            {
                return _originalBrightnessLevels.TryGetValue(hardwareId, out brightness);
            }
        }

        /// <inheritdoc />
        public void RecordOriginal(string hardwareId, uint brightness)
        {
            lock (_stateLock)
            {
                if (_originalBrightnessLevels.TryGetValue(hardwareId, out var recorded))
                {
                    Log.Debug("Monitor {HardwareId} already has a recorded original brightness of {Brightness}.", hardwareId, recorded);
                    return;
                }

                _originalBrightnessLevels[hardwareId] = brightness;
                _brightnessStateService.SaveState(new Dictionary<string, uint>(_originalBrightnessLevels));
            }
        }

        /// <inheritdoc />
        public void RemoveOriginal(string hardwareId)
        {
            lock (_stateLock)
            {
                if (!_originalBrightnessLevels.Remove(hardwareId)) return;

                _brightnessStateService.SaveState(new Dictionary<string, uint>(_originalBrightnessLevels));
            }
        }

        /// <inheritdoc />
        public Dictionary<string, uint> GetAll()
        {
            lock (_stateLock)
            {
                return new Dictionary<string, uint>(_originalBrightnessLevels);
            }
        }
    }
}
