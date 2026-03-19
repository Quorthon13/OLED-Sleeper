using OLED_Sleeper.Core.Interfaces;
using OLED_Sleeper.Features.MonitorBehavior.Commands;
using OLED_Sleeper.Features.MonitorDimming.Commands;
using OLED_Sleeper.Features.MonitorIdleDetection.Services.Interfaces;
using OLED_Sleeper.Features.MonitorState.Services.Interfaces;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Features.UserSettings.Services.Interfaces;
using Serilog;

namespace OLED_Sleeper.Core
{
    /// <summary>
    /// Central application orchestrator for monitor management in OLED-Sleeper.
    /// <para>
    /// This class is responsible for initializing and coordinating monitor-related services, applying user settings, and ensuring monitor state is restored on startup and shutdown.
    /// It subscribes to user settings changes and system notifications, and dispatches commands to synchronize and restore monitor state as needed.
    /// </para>
    /// <para>
    /// Key responsibilities:
    /// <list type="bullet">
    /// <item><description>Initializes monitor management services and applies persisted settings on startup.</description></item>
    /// <item><description>Restores all monitor brightness levels on startup and shutdown.</description></item>
    /// <item><description>Handles user settings changes and updates monitor idle detection accordingly.</description></item>
    /// <item><description>Dispatches commands to synchronize and restore monitor state as needed.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public class ApplicationOrchestrator : IApplicationOrchestrator
    {
        private readonly IMediator _mediator;
        private readonly IMonitorIdleDetectionService _monitorIdleDetectionService;
        private readonly IMonitorSettingsFileService _monitorSettingsFileService;
        private readonly IMonitorStateWatcher _monitorStateWatcher;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationOrchestrator"/> class.
        /// </summary>
        /// <param name="mediator">Mediator for dispatching monitor-related commands.</param>
        /// <param name="monitorIdleDetectionService">Service for detecting monitor idle state and applying idle/active behaviors.</param>
        /// <param name="monitorSettingsFileService">Service for loading and saving monitor settings.</param>
        /// <param name="monitorStateWatcher">Service for monitoring system monitor connection/disconnection.</param>
        public ApplicationOrchestrator(
            IMediator mediator,
            IMonitorIdleDetectionService monitorIdleDetectionService,
            IMonitorSettingsFileService monitorSettingsFileService,
            IMonitorStateWatcher monitorStateWatcher)
        {
            _mediator = mediator;
            _monitorIdleDetectionService = monitorIdleDetectionService;
            _monitorSettingsFileService = monitorSettingsFileService;
            _monitorStateWatcher = monitorStateWatcher;
        }

        #endregion Constructor

        #region Startup/Shutdown

        /// <summary>
        /// Starts the orchestrator, subscribes to relevant events, restores monitor brightness, and starts monitor state monitoring.
        /// </summary>
        public void Start()
        {
            SendRestoreBrightnessOnAllMonitorsCommand();
            SubscribeToEvents();
            InitializeStateWatcher();
        }

        /// <summary>
        /// Stops the orchestrator, restores all monitor brightness, unsubscribes from events, and stops monitor state monitoring.
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
        /// Subscribes to user settings and application notifications for monitor management.
        /// </summary>
        private void SubscribeToEvents()
        {
            _monitorSettingsFileService.SettingsChanged += OnSettingsChanged;
            ApplicationNotifications.RestoreAllMonitorsRequested += RestoreAllMonitors;
        }

        /// <summary>
        /// Unsubscribes from user settings and application notifications.
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            _monitorSettingsFileService.SettingsChanged -= OnSettingsChanged;
            ApplicationNotifications.RestoreAllMonitorsRequested -= RestoreAllMonitors;
        }

        #endregion Event Subscriptions

        #region Monitor State Initialization & Restoration

        /// <summary>
        /// Starts the monitor state watcher, which monitors system monitor connection/disconnection.
        /// </summary>
        private void InitializeStateWatcher()
        {
            _monitorStateWatcher.Start();
        }

        /// <summary>
        /// Restores all monitors' brightness levels to their normal state.
        /// </summary>
        public void RestoreAllMonitors()
        {
            Log.Information("Restoring all monitors brightness levels...");
            SendRestoreBrightnessOnAllMonitorsCommand();
        }

        #endregion Monitor State Initialization & Restoration

        #region Monitor State Event Handlers

        /// <summary>
        /// Handles user settings changes and updates the monitor idle detection service and restores monitor state as needed.
        /// </summary>
        private void OnSettingsChanged(List<MonitorSettings> settings)
        {
            _monitorIdleDetectionService.UpdateSettings(settings);
            foreach (var setting in settings)
            {
                SendRestoreMonitorStateCommand(setting.HardwareId);
            }
        }

        #endregion Monitor State Event Handlers

        #region Command Senders

        /// <summary>
        /// Sends a command to restore brightness for all monitors that may have been dimmed from a previous session.
        /// </summary>
        private void SendRestoreBrightnessOnAllMonitorsCommand()
        {
            var command = new RestoreBrightnessOnAllMonitorsCommand();
            _mediator.SendAsync(command);
            Log.Information("RestoreBrightnessOnAllMonitorsCommand sent.");
        }

        /// <summary>
        /// Sends a command to restore a monitor's state by undimming it and hiding any blackout overlay.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor to restore.</param>
        private void SendRestoreMonitorStateCommand(string hardwareId)
        {
            var command = new RestoreMonitorStateCommand { HardwareId = hardwareId };
            _mediator.SendAsync(command);
            Log.Information("RestoreMonitorStateCommand sent for monitor {HardwareId}.", hardwareId);
        }

        #endregion Command Senders
    }
}