using OLED_Sleeper.Events;
using OLED_Sleeper.Models;
using Serilog;

namespace OLED_Sleeper.Services
{
    /// <summary>
    /// Coordinates application services to manage monitor idle/active state transitions and applies user settings.
    /// </summary>
    public class ApplicationOrchestrator : IApplicationOrchestrator
    {
        private readonly IIdleActivityService _idleService;
        private readonly IOverlayService _overlayService;
        private readonly ISettingsService _settingsService;
        private readonly IDimmerService _dimmerService;
        private readonly IBrightnessStateService _brightnessStateService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationOrchestrator"/> class.
        /// </summary>
        /// <param name="idleService">Service for monitoring idle activity.</param>
        /// <param name="overlayService">Service for managing overlay windows.</param>
        /// <param name="settingsService">Service for loading and saving monitor settings.</param>
        /// <param name="dimmerService">Service for dimming and restoring monitor brightness.</param>
        public ApplicationOrchestrator(
            IIdleActivityService idleService,
            IOverlayService overlayService,
            ISettingsService settingsService,
            IDimmerService dimmerService,
            IBrightnessStateService brightnessStateService)
        {
            _idleService = idleService;
            _overlayService = overlayService;
            _settingsService = settingsService;
            _dimmerService = dimmerService;
            _brightnessStateService = brightnessStateService;
        }

        /// <summary>
        /// Starts the orchestrator, subscribing to idle events and applying initial settings.
        /// </summary>
        public void Start()
        {
            RestoreBrightnessOnStartup();

            _idleService.MonitorBecameIdle += OnMonitorBecameIdle;
            _idleService.MonitorBecameActive += OnMonitorBecameActive;
            AppEvents.RestoreAllMonitorsRequested += RestoreAllMonitors;

            var initialSettings = _settingsService.LoadSettings();
            _idleService.UpdateSettings(initialSettings);
            _idleService.Start();
        }

        private void RestoreBrightnessOnStartup()
        {
            Log.Information("Checking for monitors with unrestored brightness...");
            var state = _brightnessStateService.LoadState();
            if (state.Any())
            {
                Log.Warning("Found {Count} monitors that were left dimmed from a previous session. Attempting to restore.", state.Count);
                foreach (var entry in state)
                {
                    _dimmerService.RestoreBrightness(entry.Key, entry.Value);
                }

                // Clear the state file now that we've attempted a restore.
                _brightnessStateService.SaveState(new Dictionary<string, uint>());
            }
        }

        #region Event Handlers

        /// <summary>
        /// Handles the event when a monitor becomes idle. Applies blackout or dimming as configured.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">Event arguments containing monitor state and settings.</param>
        private void OnMonitorBecameIdle(object sender, MonitorStateEventArgs e)
        {
            Log.Information("Orchestrator received MonitorBecameIdle event for Monitor #{DisplayNumber}.", e.DisplayNumber);

            switch (e.Settings.Behavior)
            {
                case MonitorBehavior.Blackout:
                    _overlayService.ShowBlackoutOverlay(e.HardwareId, e.Bounds);
                    break;

                case MonitorBehavior.Dim:
                    _dimmerService.DimMonitor(e.HardwareId, (int)e.Settings.DimLevel);
                    break;

                default:
                    // No action for other behaviors
                    break;
            }
        }

        /// <summary>
        /// Handles the event when a monitor becomes active. Restores monitor state if not triggered by overlay window.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">Event arguments containing monitor state and settings.</param>
        private void OnMonitorBecameActive(object sender, MonitorStateEventArgs e)
        {
            if (e.Reason == ActivityReason.ActiveWindow && _overlayService.IsOverlayWindow(e.ForegroundWindowHandle))
            {
                Log.Debug("Monitor #{DisplayNumber} became active due to an overlay window. Flagging event as ignored.", e.DisplayNumber);
                e.IsIgnored = true;
                return;
            }

            Log.Information("Orchestrator received MonitorBecameActive event for Monitor #{DisplayNumber}. Commanding services to restore state.", e.DisplayNumber);

            _overlayService.HideOverlay(e.HardwareId);
            _dimmerService.UndimMonitor(e.HardwareId);
        }

        #endregion Event Handlers

        public void Stop()
        {
            Log.Information("ApplicationOrchestrator is stopping.");
            RestoreAllMonitors();

            AppEvents.RestoreAllMonitorsRequested -= RestoreAllMonitors;
            _idleService.Stop();
        }

        public void RestoreAllMonitors()
        {
            Log.Information("Restoring all monitors brightness levels...");
            var dimmedMonitors = _dimmerService.GetDimmedMonitors();

            if (dimmedMonitors.Any())
            {
                foreach (var entry in dimmedMonitors)
                {
                    // Use the restore method that doesn't re-save the state
                    _dimmerService.RestoreBrightness(entry.Key, entry.Value);
                }

                // Clear the state file since we've handled the restore
                _brightnessStateService.SaveState(new Dictionary<string, uint>());
            }
        }
    }
}