using OLED_Sleeper.Events;
using OLED_Sleeper.Models;
using OLED_Sleeper.Services.Application.Interfaces;
using OLED_Sleeper.Services.Monitor.Interfaces;
using OLED_Sleeper.Helpers;
using Serilog;
using System.Collections.Generic;
using System.Linq;

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
        private readonly IMonitorIdleDetectionService _monitorIdleDetectionService;
        private readonly IMonitorBlackoutService _monitorBlackoutService;
        private readonly IMonitorSettingsFileService _monitorSettingsFileService;
        private readonly IMonitorDimmingService _monitorDimmingService;
        private readonly IMonitorBrightnessStateService _monitorBrightnessStateService;
        private readonly IMonitorStateWatcher _monitorStateWatcher;
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
        /// Starts the orchestrator, subscribing to idle events and applying initial settings.
        /// </summary>
        public void Start()
        {
            RestoreBrightnessOnStartup();
            SubscribeToIdleDetectionEvents();
            SubscribeToMonitorStateEvents();
            SubscribeToSettingsChangedEvent();
            InitializeMonitorSettings();
        }

        /// <summary>
        /// Subscribes to the settings changed event.
        /// </summary>
        private void SubscribeToSettingsChangedEvent()
        {
            _monitorSettingsFileService.SettingsChanged += OnSettingsChanged;
        }

        /// <summary>
        /// Subscribes to monitor idle detection and application events.
        /// </summary>
        private void SubscribeToIdleDetectionEvents()
        {
            _monitorIdleDetectionService.MonitorBecameIdle += OnMonitorBecameIdle;
            _monitorIdleDetectionService.MonitorBecameActive += OnMonitorBecameActive;
            AppNotifications.RestoreAllMonitorsRequested += RestoreAllMonitors;
        }

        /// <summary>
        /// Subscribes to monitor state watcher events.
        /// </summary>
        private void SubscribeToMonitorStateEvents()
        {
            _monitorStateWatcher.MonitorsChanged += OnMonitorsChanged;
        }

        /// <summary>
        /// Initializes monitor settings and starts monitoring.
        /// </summary>
        private void InitializeMonitorSettings()
        {
            _lastKnownMonitors = null;
            _lastKnownSettings = _monitorSettingsFileService.LoadSettings();
            _monitorStateWatcher.Start();
            var initialSettings = _lastKnownSettings;
            _monitorIdleDetectionService.UpdateSettings(initialSettings);
            _monitorIdleDetectionService.Start();
        }

        /// <summary>
        /// Stops the orchestrator and restores all monitors.
        /// </summary>
        public void Stop()
        {
            Log.Information("ApplicationOrchestrator is stopping.");
            RestoreAllMonitors();
            AppNotifications.RestoreAllMonitorsRequested -= RestoreAllMonitors;
            _monitorIdleDetectionService.Stop();
            _monitorStateWatcher.MonitorsChanged -= OnMonitorsChanged;
            _monitorStateWatcher.Stop();
        }

        #endregion Startup/Shutdown

        #region Monitor State Management

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
                    _monitorDimmingService.RestoreBrightness(entry.Key, entry.Value);
                }
                _monitorBrightnessStateService.SaveState(new Dictionary<string, uint>());
            }
        }

        /// <summary>
        /// Restores all monitors' brightness levels, clears dimmed state, and removes all blackout overlays.
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
                    _monitorDimmingService.RestoreBrightness(entry.Key, entry.Value);
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

        #endregion Monitor State Management

        #region Event Handlers

        /// <summary>
        /// Handles the event when the set of connected monitors changes.
        /// Logs the change and handles disconnect/reconnect events.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="newMonitors">The new list of connected monitors.</param>
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
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
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
        /// Handles blackout behavior for a monitor, including overlay and DDC/CI brightness if supported.
        /// </summary>
        /// <param name="e">The monitor state event arguments.</param>
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
        /// <param name="e">The monitor state event arguments.</param>
        private void HandleMonitorDim(MonitorStateEventArgs e)
        {
            _monitorDimmingService.DimMonitor(e.HardwareId, (int)e.Settings.DimLevel);
        }

        /// <summary>
        /// Sets the brightness of the specified monitor to 0 using DDC/CI.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        private void DimMonitorToZero(string hardwareId)
        {
            _monitorDimmingService.DimMonitor(hardwareId, 0);
        }

        /// <summary>
        /// Gets the MonitorInfo for a given hardware ID from the last known monitors.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        /// <returns>The MonitorInfo if found; otherwise, null.</returns>
        private MonitorInfo? GetMonitorInfoByHardwareId(string hardwareId)
        {
            return _lastKnownMonitors?.FirstOrDefault(m => m.HardwareId == hardwareId);
        }

        /// <summary>
        /// Handles the event when a monitor becomes active. Restores monitor state if not triggered by overlay window.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
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
            _monitorDimmingService.UndimMonitor(e.HardwareId);
        }

        /// <summary>
        /// Called when settings are changed and saved
        /// </summary>
        /// <param name="settings">The updated list of monitor settings.</param>
        private void OnSettingsChanged(List<MonitorSettings> settings)
        {
            _lastKnownSettings = settings;
            _monitorIdleDetectionService.UpdateSettings(settings);
        }

        #endregion Event Handlers

        #region Monitor Change Handling

        /// <summary>
        /// Handles all disconnected monitors.
        /// </summary>
        /// <param name="disconnected">List of disconnected monitors.</param>
        private void HandleDisconnectedMonitors(List<MonitorInfo> disconnected)
        {
            foreach (var monitor in disconnected)
                HandleMonitorDisconnect(monitor);
        }

        /// <summary>
        /// Handles all reconnected monitors.
        /// </summary>
        /// <param name="reconnected">List of reconnected monitors.</param>
        private void HandleReconnectedMonitors(List<MonitorInfo> reconnected)
        {
            foreach (var monitor in reconnected)
                HandleMonitorReconnect(monitor);
        }

        /// <summary>
        /// Handles a single monitor disconnect event.
        /// </summary>
        /// <param name="monitor">The disconnected monitor.</param>
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
        /// Gets the monitor settings for a given hardware ID.
        /// </summary>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        /// <returns>The monitor settings, or null if not found.</returns>
        private MonitorSettings? GetMonitorSettings(string hardwareId)
        {
            return _lastKnownSettings?.FirstOrDefault(s => s.HardwareId == hardwareId);
        }

        /// <summary>
        /// Logs information about a monitor disconnect event.
        /// </summary>
        /// <param name="monitor">The monitor info.</param>
        /// <param name="settings">The monitor settings.</param>
        private void LogMonitorDisconnect(MonitorInfo monitor, MonitorSettings? settings)
        {
            Log.Information("Monitor disconnected: {HardwareId} ({DeviceName}, #{DisplayNumber}). Managed: {IsManaged}", monitor.HardwareId, monitor.DeviceName, monitor.DisplayNumber, settings?.IsManaged ?? false);
        }

        /// <summary>
        /// Removes a monitor from idle detection and updates settings.
        /// </summary>
        /// <param name="settings">The monitor settings to remove.</param>
        private void RemoveMonitorFromIdleDetection(MonitorSettings settings)
        {
            _lastKnownSettings?.Remove(settings);
            _monitorIdleDetectionService.UpdateSettings(_lastKnownSettings);
            Log.Information("Removed monitor {HardwareId} from idle detection.", settings.HardwareId);
        }

        /// <summary>
        /// Hides the blackout overlay for a monitor.
        /// </summary>
        /// <param name="hardwareId">The hardware ID of the monitor.</param>
        private void HideMonitorOverlay(string hardwareId)
        {
            _monitorBlackoutService.HideOverlay(hardwareId);
            Log.Information("Overlay hidden for monitor {HardwareId}.", hardwareId);
        }

        /// <summary>
        /// Handles a single monitor reconnect event.
        /// </summary>
        /// <param name="monitor">The reconnected monitor.</param>
        private void HandleMonitorReconnect(MonitorInfo monitor)
        {
            var brightnessState = _monitorBrightnessStateService.LoadState();
            var settings = GetMonitorSettings(monitor.HardwareId);
            LogMonitorReconnect(monitor, settings);
            if (settings?.IsManaged == true)
            {
                AddMonitorToIdleDetectionIfNeeded(monitor);
                RestoreMonitorBrightnessIfNeeded(monitor, brightnessState);
            }
            else
            {
                Log.Debug("Monitor {HardwareId} is not managed, no idle/brightness action taken.", monitor.HardwareId);
            }
        }

        /// <summary>
        /// Logs information about a monitor reconnect event.
        /// </summary>
        /// <param name="monitor">The monitor info.</param>
        /// <param name="settings">The monitor settings.</param>
        private void LogMonitorReconnect(MonitorInfo monitor, MonitorSettings? settings)
        {
            Log.Information("Monitor reconnected: {HardwareId} ({DeviceName}, #{DisplayNumber}). Managed: {IsManaged}", monitor.HardwareId, monitor.DeviceName, monitor.DisplayNumber, settings?.IsManaged ?? false);
        }

        /// <summary>
        /// Adds a monitor to idle detection if it is not already present.
        /// </summary>
        /// <param name="monitor">The monitor info.</param>
        private void AddMonitorToIdleDetectionIfNeeded(MonitorInfo monitor)
        {
            if (!_lastKnownSettings.Any(s => s.HardwareId == monitor.HardwareId))
            {
                _lastKnownSettings.Add(new MonitorSettings
                {
                    HardwareId = monitor.HardwareId,
                    IsManaged = true
                    // Optionally set other defaults
                });
                _monitorIdleDetectionService.UpdateSettings(_lastKnownSettings);
                Log.Information("Added monitor {HardwareId} to idle detection.", monitor.HardwareId);
            }
        }

        /// <summary>
        /// Restores the brightness for a monitor if a saved brightness value exists.
        /// </summary>
        /// <param name="monitor">The monitor info.</param>
        /// <param name="brightnessState">The dictionary of saved brightness states.</param>
        private void RestoreMonitorBrightnessIfNeeded(MonitorInfo monitor, Dictionary<string, uint> brightnessState)
        {
            if (brightnessState.TryGetValue(monitor.HardwareId, out var savedBrightness))
            {
                _monitorDimmingService.RestoreBrightness(monitor.HardwareId, savedBrightness);
                Log.Information("Restored brightness for {HardwareId} to {Brightness}.", monitor.HardwareId, savedBrightness);
            }
        }

        #endregion Monitor Change Handling
    }
}