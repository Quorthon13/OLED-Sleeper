using OLED_Sleeper.Features.MonitorDimming.Commands;
using OLED_Sleeper.Features.MonitorIdleDetection.Services.Interfaces;
using OLED_Sleeper.Features.MonitorState.Services.Interfaces;
using OLED_Sleeper.Features.UserSettings.Commands;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Features.UserSettings.Services.Interfaces;
using OLED_Sleeper.Infrastructure.Hosting.Interfaces;
using OLED_Sleeper.Messaging.Interfaces;
using Serilog;

namespace OLED_Sleeper.Infrastructure.Hosting
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
            _ = RestoreAllMonitorsAsync();
            SubscribeToEvents();
            InitializeStateWatcher();
        }

        /// <summary>
        /// Unsubscribes from events, stops the idle loop and the state watcher, then restores all monitor
        /// brightness. The returned task completes when the restore has finished.
        /// </summary>
        public async Task StopAsync()
        {
            Log.Information("ApplicationOrchestrator is stopping.");

            UnsubscribeFromEvents();
            _monitorIdleDetectionService.Stop();
            _monitorStateWatcher.Stop();

            await RestoreAllMonitorsAsync();
        }

        #endregion Startup/Shutdown

        #region Event Subscriptions

        /// <summary>
        /// Subscribes to user settings changes.
        /// </summary>
        private void SubscribeToEvents()
        {
            _monitorSettingsFileService.SettingsChanged += OnSettingsChanged;
        }

        /// <summary>
        /// Unsubscribes from user settings changes.
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            _monitorSettingsFileService.SettingsChanged -= OnSettingsChanged;
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
        private async Task RestoreAllMonitorsAsync()
        {
            Log.Information("Restoring all monitors brightness levels...");

            try
            {
                await _mediator.SendAsync(new RestoreBrightnessOnAllMonitorsCommand());
                Log.Information("RestoreBrightnessOnAllMonitorsCommand completed.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to restore monitor brightness levels.");
            }
        }

        #endregion Monitor State Initialization & Restoration

        #region Monitor State Event Handlers

        /// <summary>
        /// Sends an <see cref="ApplySettingsChangeCommand"/> for the settings that changed.
        /// </summary>
        private void OnSettingsChanged(List<MonitorSettings> settings)
        {
            _mediator.SendAsync(new ApplySettingsChangeCommand { Settings = settings });
        }

        #endregion Monitor State Event Handlers
    }
}