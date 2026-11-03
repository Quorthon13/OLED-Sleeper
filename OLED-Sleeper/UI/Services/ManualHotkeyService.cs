using OLED_Sleeper.Features.MonitorBehavior.Commands;
using OLED_Sleeper.Features.MonitorBlackout.Commands;
using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;
using OLED_Sleeper.Features.MonitorIdleDetection.Services.Interfaces;
using OLED_Sleeper.Features.MonitorInformation.Services.Interfaces;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Messaging.Interfaces;
using OLED_Sleeper.UI.Models;
using OLED_Sleeper.UI.Services.Interfaces;
using Serilog;
using System.Windows;

namespace OLED_Sleeper.UI.Services
{

    /// <summary>Connects buttons and global shortcuts to the existing blackout and restore commands.</summary>
    public sealed class ManualHotkeyService : IManualBlackoutService
    {
        private readonly IMediator _mediator;
        private readonly IMonitorBlackoutService _blackoutService;
        private readonly IMonitorIdleDetectionService _idleDetection;
        private readonly IMonitorInfoManager _monitorInfoManager;
        private readonly IGlobalHotkeyRegistrar _registrar;
        private readonly SemaphoreSlim _toggleGate = new(1, 1);
        private readonly Dictionary<int, string> _registered = new();
        private readonly Dictionary<string, string> _errors = new(StringComparer.OrdinalIgnoreCase);
        private MonitorSettings[] _settings = Array.Empty<MonitorSettings>();
        private bool _disposed;
        private bool _captureActive;
        public event Action<string>? BlackoutStateChanged;
        public event Action? ShortcutsChanged;

        public ManualHotkeyService(IMediator mediator, IMonitorBlackoutService blackoutService,
            IMonitorIdleDetectionService idleDetection, IMonitorInfoManager monitorInfoManager, IGlobalHotkeyRegistrar registrar)
        {
            _mediator = mediator;
            _blackoutService = blackoutService;
            _idleDetection = idleDetection;
            _monitorInfoManager = monitorInfoManager;
            _registrar = registrar;
            _registrar.HotkeyPressed += OnHotkeyPressed;
            _blackoutService.BlackoutStateChanged += OnBlackoutStateChanged;
        }

        public void Attach(Window window)
        {
            _registrar.Attach(window);
            RegisterSettings();
        }

        public void Update(IEnumerable<MonitorSettings> settings)
        {
            _settings = settings.ToArray();
            RegisterSettings();
        }

        public void SetCaptureActive(bool active)
        {
            _captureActive = active;
            RegisterSettings();
        }

        public bool IsBlackoutActive(string hardwareId) => _blackoutService.IsBlackoutShown(hardwareId);
        public string GetShortcutError(string hardwareId) => _errors.GetValueOrDefault(hardwareId, string.Empty);

        private void RegisterSettings()
        {
            if (_disposed || !_registrar.IsReady) return;
            foreach (var id in _registered.Keys) _registrar.Unregister(id);
            _registered.Clear();
            _errors.Clear();
            if (_captureActive) { ShortcutsChanged?.Invoke(); return; }
            int nextId = 0x5100;
            var gestures = new Dictionary<ManualHotkeyGesture, string>();
            foreach (var setting in _settings.Where(s => !string.IsNullOrWhiteSpace(s.ManualHotkey)))
            {
                if (!ManualHotkeyGesture.TryParse(setting.ManualHotkey, out var gesture))
                {
                    _errors[setting.HardwareId] = "Choose Ctrl, Alt, Shift, or Win together with another key.";
                    continue;
                }
                if (gestures.TryGetValue(gesture, out var otherMonitor))
                {
                    _errors[otherMonitor] = _errors[setting.HardwareId] = "This shortcut is assigned to more than one monitor.";
                    continue;
                }
                gestures[gesture] = setting.HardwareId;
                int id = ++nextId;
                if (_registrar.TryRegister(id, gesture, out var error)) _registered[id] = setting.HardwareId;
                else
                {
                    _errors[setting.HardwareId] = "Shortcut unavailable: Windows or another application may reserve it. Choose another combination.";
                    Log.Warning("Cannot register blackout shortcut {Shortcut}: Windows error {Error}.", setting.ManualHotkey, error);
                }
            }
            // Ambiguous assignments must never leave the first monitor responding to the duplicated shortcut.
            foreach (var id in _registered.Where(pair => _errors.ContainsKey(pair.Value)).Select(pair => pair.Key).ToArray())
            {
                _registrar.Unregister(id);
                _registered.Remove(id);
            }
            ShortcutsChanged?.Invoke();
        }

        private async void OnHotkeyPressed(int id)
        {
            if (_registered.TryGetValue(id, out var hardwareId)) await ToggleAsync(hardwareId);
        }

        private void OnBlackoutStateChanged(string hardwareId)
        {
            if (!_blackoutService.IsBlackoutShown(hardwareId)) _idleDetection.ClearManualWake(hardwareId);
            BlackoutStateChanged?.Invoke(hardwareId);
        }

        public async Task ToggleAsync(string hardwareId)
        {
            await _toggleGate.WaitAsync();
            try
            {
                if (_disposed) return;
                if (_blackoutService.IsBlackoutShown(hardwareId))
                {
                    _idleDetection.ClearManualWake(hardwareId);
                    await _mediator.SendAsync(new RestoreMonitorStateCommand { HardwareId = hardwareId });
                }
                else
                {
                    var monitor = (await _monitorInfoManager.GetCurrentMonitorsAsync()).FirstOrDefault(m => m.HardwareId == hardwareId);
                    if (monitor == null) return;
                    bool lowerBrightness = _settings.FirstOrDefault(s => s.HardwareId == hardwareId)?.LowerBrightnessOnBlackout ?? false;
                    await _mediator.SendAsync(new ApplyBlackoutOverlayCommand { HardwareId = hardwareId, LowerBrightness = lowerBrightness });
                    if (_blackoutService.IsBlackoutShown(hardwareId))
                        _idleDetection.TrackManualWake(hardwareId, monitor.Bounds, _registrar.CursorPosition);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Manual blackout failed for monitor {HardwareId}.", hardwareId);
            }
            finally
            {
                _toggleGate.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var id in _registered.Keys) _registrar.Unregister(id);
            _registered.Clear();
            _registrar.HotkeyPressed -= OnHotkeyPressed;
            _blackoutService.BlackoutStateChanged -= OnBlackoutStateChanged;
            _registrar.Dispose();
        }
    }
}
