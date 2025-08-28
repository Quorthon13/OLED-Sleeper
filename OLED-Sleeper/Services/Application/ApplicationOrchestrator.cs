using OLED_Sleeper.Events;
using OLED_Sleeper.Models;
using OLED_Sleeper.Services.Application.Interfaces;
using OLED_Sleeper.Services.Monitor.Interfaces;
using OLED_Sleeper.Helpers;
using Serilog;

namespace OLED_Sleeper.Services.Application
{
    /// <summary>
    /// The central orchestrator for monitor management in OLED-Sleeper.
    /// <para>
    /// This class coordinates the detection of monitor idle/active states, applies user-configured settings (such as dimming and blackout),
    /// manages monitor connection/disconnection events, and ensures monitor brightness is restored appropriately.
    /// </para>
    /// <para>
    /// Key responsibilities:
    /// <list type="bullet">
    /// <item><description>Subscribes to monitor state changes and idle/active events.</description></item>
    /// <item><description>Handles monitor connect/disconnect, updating settings and overlays as needed.</description></item>
    /// <item><description>Restores brightness for monitors on startup and after dimming.</description></item>
    /// <item><description>Coordinates blackout overlays and dimming based on user preferences.</description></item>
    /// <item><description>Subscribes to system-wide monitor settings changes and updates idle detection accordingly.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Important methods:
    /// <list type="bullet">
    /// <item><description><see cref="Start"/>: Initializes and starts all monitor management logic.</description></item>
    /// <item><description><see cref="Stop"/>: Stops monitoring and restores all monitors to their normal state.</description></item>
    /// <item><description><see cref="OnMonitorsChanged"/>: Handles monitor connection/disconnection events.</description></item>
    /// <item><description><see cref="OnMonitorBecameIdle"/> / <see cref="OnMonitorBecameActive"/>: Respond to monitor idle/active transitions.</description></item>
    /// <item><description><see cref="SubscribeToSettingsChangedEvent"/>: Subscribes to system-wide monitor settings changes and updates idle detection service.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public class ApplicationOrchestrator : IApplicationOrchestrator
    {
        // Dependencies
        private readonly IMonitorIdleDetectionService _monitorIdleDetectionService;

        private readonly IMonitorBlackoutService _monitorBlackoutService;
        private readonly IMonitorSettingsFileService _monitorSettingsFileService;
        private readonly IMonitorDimmingService _monitorDimmingService;
        private readonly IMonitorBrightnessStateService _monitorBrightnessStateService;
        private readonly IMonitorStateWatcher _monitorStateWatcher;

        // State
        private IReadOnlyList<MonitorInfo>? _lastKnownMonitors;

        private List<MonitorSettings>? _lastKnownSettings;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationOrchestrator"/> class.
        /// </summary>
        /// <param name="monitorIdleDetectionService">Service for detecting monitor idle state.</param>
        /// <param name="monitorBlackoutService">Service for monitor blackout overlays.</param>
        /// <param name="monitorSettingsFileService">Service for loading/saving monitor settings.</param>
        /// <param name="monitorDimmingService">Service for monitor dimming.</param>
        /// <param name="monitorBrightnessStateService">Service for monitor brightness state.</param>
        /// <param name="monitorStateWatcher">Service for watching monitor state changes.</param>
        public ApplicationOrchestrator(
            IMonitorIdleDetectionService monitorIdleDetectionService,
            IMonitorBlackoutService monitorBlackoutService,
            IMonitorSettingsFileService monitorSettingsFileService,
            IMonitorDimmingService monitorDimmingService,
            IMonitorBrightnessStateService monitorBrightnessStateService,
            IMonitorStateWatcher monitorStateWatcher)
        {
            _monitorIdleDetectionService = monitorIdleDetectionService;
            _monitorBlackoutService = monitorBlackoutService;
            _monitorSettingsFileService = monitorSettingsFileService;
            _monitorDimmingService = monitorDimmingService;
            _monitorBrightnessStateService = monitorBrightnessStateService;
            _monitorStateWatcher = monitorStateWatcher;
        }

        #endregion Constructor

        #region Startup/Shutdown

        /// <summary>
        /// Starts the orchestrator, subscribing to events and applying initial settings.
        /// </summary>
        public void Start()
        {
            RestoreBrightnessOnStartup();
            SubscribeToEvents();
            InitializeMonitorSettings();
        }

        /// <summary>
        /// Stops the orchestrator and restores all monitors.
        /// </summary>
        public void Stop()
        {
            Log.Information("ApplicationOrchestrator is stopping.");
            RestoreAllMonitors();
            UnsubscribeFromEvents();
            _monitorIdleDetectionService.Stop();
            _monitorStateWatcher.Stop();
        }

        #endregion Startup/Shutdown

        #region Event Subscriptions

        /// <summary>
        /// Subscribes to all relevant events for monitor and settings changes.
        /// </summary>
        private void SubscribeToEvents()
        {
            _monitorIdleDetectionService.MonitorBecameIdle += OnMonitorBecameIdle;
            _monitorIdleDetectionService.MonitorBecameActive += OnMonitorBecameActive;
            _monitorSettingsFileService.SettingsChanged += OnSettingsChanged;
            _monitorStateWatcher.MonitorsChanged += OnMonitorsChanged;
            AppNotifications.RestoreAllMonitorsRequested += RestoreAllMonitors;
        }

        /// <summary>
        /// Unsubscribes from all events.
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            _monitorIdleDetectionService.MonitorBecameIdle -= OnMonitorBecameIdle;
            _monitorIdleDetectionService.MonitorBecameActive -= OnMonitorBecameActive;
            _monitorSettingsFileService.SettingsChanged -= OnSettingsChanged;
            _monitorStateWatcher.MonitorsChanged -= OnMonitorsChanged;
            AppNotifications.RestoreAllMonitorsRequested -= RestoreAllMonitors;
        }

        #endregion Event Subscriptions

        #region Monitor State Initialization & Restoration

        /// <summary>
        /// Loads persisted monitor settings and starts monitoring.
        /// </summary>
        private void InitializeMonitorSettings()
        {
            _lastKnownMonitors = null;
            _lastKnownSettings = _monitorSettingsFileService.LoadSettings();
            _monitorStateWatcher.Start();
            _monitorIdleDetectionService.UpdateSettings(_lastKnownSettings);
            _monitorIdleDetectionService.Start();
        }

        /// <summary>
        /// Restores brightness for all monitors that were left dimmed from a previous session.
        /// </summary>
        private void RestoreBrightnessOnStartup()
        {
            Log.Information("Checking for monitors with unrestored brightness...");
            var state = _monitorBrightnessStateService.LoadState();
            if (state.Any())
            {
                Log.Warning("Found {Count} monitors that were left dimmed from a previous session. Attempting to restore.", state.Count);
                foreach (var entry in state)
                {
                    _monitorDimmingService.RestoreBrightnessAsync(entry.Key, entry.Value);
                }
                _monitorBrightnessStateService.SaveState(new Dictionary<string, uint>());
            }
        }

        /// <summary>
        /// Restores all monitors' brightness levels and removes all blackout overlays.
        /// </summary>
        public void RestoreAllMonitors()
        {
            Log.Information("Restoring all monitors brightness levels and removing overlays...");
            RestoreAllBrightness();
            RemoveAllOverlays();
        }

        /// <summary>
        /// Restores brightness for all dimmed monitors and clears the dimmed state.
        /// </summary>
        private void RestoreAllBrightness()
        {
            var dimmedMonitors = _monitorDimmingService.GetDimmedMonitors();
            if (dimmedMonitors.Any())
            {
                foreach (var entry in dimmedMonitors)
                {
                    _monitorDimmingService.RestoreBrightnessAsync(entry.Key, entry.Value);
                }
                _monitorBrightnessStateService.SaveState(new Dictionary<string, uint>());
            }
        }

        /// <summary>
        /// Removes all blackout overlays from all known monitors.
        /// </summary>
        private void RemoveAllOverlays()
        {
            if (_lastKnownMonitors != null)
            {
                foreach (var monitor in _lastKnownMonitors)
                {
                    _monitorBlackoutService.HideOverlay(monitor.HardwareId);
                }
            }
        }

        #endregion Monitor State Initialization & Restoration

        #region Monitor State Event Handlers

        /// <summary>
        /// Handles monitor connection/disconnection events and updates state accordingly.
        /// </summary>
        private void OnMonitorsChanged(object? sender, IReadOnlyList<MonitorInfo> newMonitors)
        {
            if (_lastKnownMonitors == null)
            {
                Log.Information("MonitorStateWatcher initial monitor info received. Count: {Count}", newMonitors.Count);
                _lastKnownMonitors = newMonitors;
                return;
            }

            var disconnected = MonitorHelper.GetDisconnectedMonitors(_lastKnownMonitors, newMonitors);
            var reconnected = MonitorHelper.GetReconnectedMonitors(_lastKnownMonitors, newMonitors);

            HandleDisconnectedMonitors(disconnected);
            HandleReconnectedMonitors(reconnected);

            if (disconnected.Count > 0 || reconnected.Count > 0)
            {
                Log.Information("Monitor configuration changed. Disconnected: {DisconnectedCount}, Reconnected: {ReconnectedCount}", disconnected.Count, reconnected.Count);
            }
            else
            {
                Log.Debug("Monitor configuration polled, no changes detected.");
            }
            _lastKnownMonitors = newMonitors;
        }

        /// <summary>
        /// Handles the event when a monitor becomes idle. Applies blackout or dimming as configured.
        /// </summary>
        private void OnMonitorBecameIdle(object? sender, MonitorStateEventArgs e)
        {
            Log.Information("Orchestrator received MonitorBecameIdle event for Monitor #{DisplayNumber}.", e.DisplayNumber);
            switch (e.Settings.Behavior)
            {
                case MonitorBehavior.Blackout:
                    HandleMonitorBlackout(e);
                    break;

                case MonitorBehavior.Dim:
                    HandleMonitorDim(e);
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Handles the event when a monitor becomes active. Restores monitor state if not triggered by overlay window.
        /// </summary>
        private void OnMonitorBecameActive(object? sender, MonitorStateEventArgs e)
        {
            if (e.Reason == ActivityReason.ActiveWindow && _monitorBlackoutService.IsOverlayWindow(e.ForegroundWindowHandle))
            {
                Log.Debug("Monitor #{DisplayNumber} became active due to an overlay window. Flagging event as ignored.", e.DisplayNumber);
                e.IsIgnored = true;
                return;
            }
            Log.Information("Orchestrator received MonitorBecameActive event for Monitor #{DisplayNumber}. Commanding services to restore state.", e.DisplayNumber);
            _monitorBlackoutService.HideOverlay(e.HardwareId);
            _monitorDimmingService.UndimMonitorAsync(e.HardwareId);
        }

        /// <summary>
        /// Handles settings changed event and updates idle detection service.
        /// </summary>
        private void OnSettingsChanged(List<MonitorSettings> settings)
        {
            _lastKnownSettings = settings;
            _monitorIdleDetectionService.UpdateSettings(settings);
        }

        #endregion Monitor State Event Handlers

        #region Monitor Idle/Blackout/Dimming Handlers

        /// <summary>
        /// Handles blackout behavior for a monitor, including overlay and DDC/CI brightness if supported.
        /// </summary>
        private void HandleMonitorBlackout(MonitorStateEventArgs e)
        {
            _monitorBlackoutService.ShowBlackoutOverlay(e.HardwareId, e.Bounds);
            var monitorInfo = GetMonitorInfoByHardwareId(e.HardwareId);
            if (monitorInfo != null && monitorInfo.IsDdcCiSupported)
            {
                DimMonitorToZero(e.HardwareId);
                Log.Information("Monitor {HardwareId} supports DDC/CI. Brightness set to 0 for blackout.", e.HardwareId);
            }
        }

        /// <summary>
        /// Handles dim behavior for a monitor.
        /// </summary>
        private void HandleMonitorDim(MonitorStateEventArgs e)
        {
            _monitorDimmingService.DimMonitorAsync(e.HardwareId, (int)e.Settings.DimLevel);
        }

        /// <summary>
        /// Sets the brightness of the specified monitor to 0 using DDC/CI.
        /// </summary>
        private void DimMonitorToZero(string hardwareId)
        {
            _monitorDimmingService.DimMonitorAsync(hardwareId, 0);
        }

        /// <summary>
        /// Gets the MonitorInfo for a given hardware ID from the last known monitors.
        /// </summary>
        private MonitorInfo? GetMonitorInfoByHardwareId(string hardwareId)
        {
            return _lastKnownMonitors?.FirstOrDefault(m => m.HardwareId == hardwareId);
        }

        #endregion Monitor Idle/Blackout/Dimming Handlers

        #region Monitor Change Handling

        /// <summary>
        /// Handles all disconnected monitors by removing them from idle detection and hiding overlays.
        /// </summary>
        private void HandleDisconnectedMonitors(List<MonitorInfo> disconnected)
        {
            foreach (var monitor in disconnected)
            {
                HandleMonitorDisconnect(monitor);
            }
        }

        /// <summary>
        /// Handles all reconnected monitors by restoring settings and brightness if needed.
        /// </summary>
        private void HandleReconnectedMonitors(List<MonitorInfo> reconnected)
        {
            foreach (var monitor in reconnected)
            {
                HandleMonitorReconnect(monitor);
            }
        }

        /// <summary>
        /// Handles a single monitor disconnect event: removes from idle detection, hides overlay, and logs.
        /// </summary>
        private void HandleMonitorDisconnect(MonitorInfo monitor)
        {
            var settings = GetMonitorSettings(monitor.HardwareId);
            LogMonitorDisconnect(monitor, settings);
            if (settings?.IsManaged == true)
            {
                RemoveMonitorFromIdleDetection(settings);
                HideMonitorOverlay(monitor.HardwareId);
            }
            else
            {
                Log.Debug("Monitor {HardwareId} was not managed, no idle/overlay action taken.", monitor.HardwareId);
            }
        }

        /// <summary>
        /// Handles a single monitor reconnect event: ensures settings are loaded, idle detection is updated, and brightness is restored if needed.
        /// </summary>
        private void HandleMonitorReconnect(MonitorInfo monitor)
        {
            var brightnessState = _monitorBrightnessStateService.LoadState();
            // Always load the latest settings from disk for authoritative user settings
            var persistedSettings = _monitorSettingsFileService.LoadSettings();
            var monitorSettings = persistedSettings.FirstOrDefault(s => s.HardwareId == monitor.HardwareId);
            LogMonitorReconnect(monitor, monitorSettings);

            if (monitorSettings != null && monitorSettings.IsManaged)
            {
                AddMonitorToIdleDetectionIfNeeded(monitorSettings);
                RestoreMonitorBrightnessIfNeeded(monitor, brightnessState);
            }
            else
            {
                Log.Debug("Monitor {HardwareId} is not managed or has no saved settings.", monitor.HardwareId);
            }
        }

        /// <summary>
        /// Gets the monitor settings for a given hardware ID from the in-memory settings list.
        /// </summary>
        private MonitorSettings? GetMonitorSettings(string hardwareId)
        {
            return _lastKnownSettings?.FirstOrDefault(s => s.HardwareId == hardwareId);
        }

        /// <summary>
        /// Logs information about a monitor disconnect event.
        /// </summary>
        private void LogMonitorDisconnect(MonitorInfo monitor, MonitorSettings? settings)
        {
            Log.Information("Monitor disconnected: {HardwareId} ({DeviceName}, #{DisplayNumber}). Managed: {IsManaged}", monitor.HardwareId, monitor.DeviceName, monitor.DisplayNumber, settings?.IsManaged ?? false);
        }

        /// <summary>
        /// Removes a monitor from idle detection and updates settings.
        /// </summary>
        private void RemoveMonitorFromIdleDetection(MonitorSettings settings)
        {
            _lastKnownSettings?.Remove(settings);
            _monitorIdleDetectionService.UpdateSettings(_lastKnownSettings);
            Log.Information("Removed monitor {HardwareId} from idle detection.", settings.HardwareId);
        }

        /// <summary>
        /// Hides the blackout overlay for a monitor.
        /// </summary>
        private void HideMonitorOverlay(string hardwareId)
        {
            _monitorBlackoutService.HideOverlay(hardwareId);
            Log.Information("Overlay hidden for monitor {HardwareId}.", hardwareId);
        }

        /// <summary>
        /// Logs information about a monitor reconnect event.
        /// </summary>
        private void LogMonitorReconnect(MonitorInfo monitor, MonitorSettings? settings)
        {
            Log.Information("Monitor reconnected: {HardwareId} ({DeviceName}, #{DisplayNumber}). Managed: {IsManaged}", monitor.HardwareId, monitor.DeviceName, monitor.DisplayNumber, settings?.IsManaged ?? false);
        }

        /// <summary>
        /// Adds a monitor's settings to idle detection if not already present in the in-memory settings list.
        /// </summary>
        private void AddMonitorToIdleDetectionIfNeeded(MonitorSettings settings)
        {
            if (!_lastKnownSettings.Any(s => s.HardwareId == settings.HardwareId))
            {
                _lastKnownSettings.Add(settings);
                _monitorIdleDetectionService.UpdateSettings(_lastKnownSettings);
                Log.Information("Added monitor {HardwareId} to idle detection.", settings.HardwareId);
            }
        }

        /// <summary>
        /// Restores the brightness for a monitor if a saved brightness value exists.
        /// </summary>
        private void RestoreMonitorBrightnessIfNeeded(MonitorInfo monitor, Dictionary<string, uint> brightnessState)
        {
            if (brightnessState.TryGetValue(monitor.HardwareId, out var savedBrightness))
            {
                _monitorDimmingService.RestoreBrightnessAsync(monitor.HardwareId, savedBrightness);
                Log.Information("Restored brightness for {HardwareId} to {Brightness}.", monitor.HardwareId, savedBrightness);
            }
        }

        #endregion Monitor Change Handling
    }
}