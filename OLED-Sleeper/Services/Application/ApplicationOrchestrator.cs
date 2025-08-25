using OLED_Sleeper.Events;
using OLED_Sleeper.Models;
using OLED_Sleeper.Services.Application.Interfaces;
using OLED_Sleeper.Services.Monitor.Interfaces;
using Serilog;

namespace OLED_Sleeper.Services.Application
{
    /// <summary>
    /// Coordinates monitor idle/active state transitions and applies user settings.
    /// </summary>
    public class ApplicationOrchestrator : IApplicationOrchestrator
    {
        private readonly IMonitorIdleDetectionService _monitorIdleDetectionService;
        private readonly IMonitorBlackoutService _monitorBlackoutService;
        private readonly IMonitorSettingsFileService _monitorSettingsFileService;
        private readonly IMonitorDimmingService _monitorDimmingService;
        private readonly IMonitorBrightnessStateService _monitorBrightnessStateService;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationOrchestrator"/> class.
        /// </summary>
        public ApplicationOrchestrator(
            IMonitorIdleDetectionService monitorIdleDetectionService,
            IMonitorBlackoutService monitorBlackoutService,
            IMonitorSettingsFileService monitorSettingsFileService,
            IMonitorDimmingService monitorDimmingService,
            IMonitorBrightnessStateService monitorBrightnessStateService)
        {
            _monitorIdleDetectionService = monitorIdleDetectionService;
            _monitorBlackoutService = monitorBlackoutService;
            _monitorSettingsFileService = monitorSettingsFileService;
            _monitorDimmingService = monitorDimmingService;
            _monitorBrightnessStateService = monitorBrightnessStateService;
        }

        #endregion Constructor

        #region Startup/Shutdown

        /// <summary>
        /// Starts the orchestrator, subscribing to idle events and applying initial settings.
        /// </summary>
        public void Start()
        {
            RestoreBrightnessOnStartup();

            _monitorIdleDetectionService.MonitorBecameIdle += OnMonitorBecameIdle;
            _monitorIdleDetectionService.MonitorBecameActive += OnMonitorBecameActive;
            AppEvents.RestoreAllMonitorsRequested += RestoreAllMonitors;

            var initialSettings = _monitorSettingsFileService.LoadSettings();
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

            AppEvents.RestoreAllMonitorsRequested -= RestoreAllMonitors;
            _monitorIdleDetectionService.Stop();
        }

        #endregion Startup/Shutdown

        #region Event Handlers

        /// <summary>
        /// Handles the event when a monitor becomes idle. Applies blackout or dimming as configured.
        /// </summary>
        private void OnMonitorBecameIdle(object sender, MonitorStateEventArgs e)
        {
            Log.Information("Orchestrator received MonitorBecameIdle event for Monitor #{DisplayNumber}.", e.DisplayNumber);

            switch (e.Settings.Behavior)
            {
                case MonitorBehavior.Blackout:
                    _monitorBlackoutService.ShowBlackoutOverlay(e.HardwareId, e.Bounds);
                    break;

                case MonitorBehavior.Dim:
                    _monitorDimmingService.DimMonitor(e.HardwareId, (int)e.Settings.DimLevel);
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Handles the event when a monitor becomes active. Restores monitor state if not triggered by overlay window.
        /// </summary>
        private void OnMonitorBecameActive(object sender, MonitorStateEventArgs e)
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

        #endregion Event Handlers

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
        /// Restores all monitors' brightness levels and clears dimmed state.
        /// </summary>
        public void RestoreAllMonitors()
        {
            Log.Information("Restoring all monitors brightness levels...");
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

        #endregion Monitor State Management
    }
}