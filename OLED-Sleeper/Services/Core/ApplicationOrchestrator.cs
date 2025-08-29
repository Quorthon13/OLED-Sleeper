using OLED_Sleeper.Commands.Monitor.Behavior;
using OLED_Sleeper.Commands.Monitor.Blackout;
using OLED_Sleeper.Commands.Monitor.Dimming;
using OLED_Sleeper.Core;
using OLED_Sleeper.Core.Interfaces;
using OLED_Sleeper.Models;
using OLED_Sleeper.Services.Core.Interfaces;
using OLED_Sleeper.Services.Monitor.Blackout.Interfaces;
using OLED_Sleeper.Services.Monitor.IdleDetection.Interfaces;
using OLED_Sleeper.Services.Monitor.Settings.Interfaces;
using Serilog;

namespace OLED_Sleeper.Services.Core
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
    /// <item><description><see cref="OnSettingsChanged"/>: Handles monitor settings changes and updates idle detection service.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public class ApplicationOrchestrator : IApplicationOrchestrator
    {
        // Dependencies
        private readonly IMediator _mediator;

        private readonly IMonitorIdleDetectionService _monitorIdleDetectionService;
        private readonly IMonitorSettingsFileService _monitorSettingsFileService;
        private readonly IMonitorStateWatcher _monitorStateWatcher;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationOrchestrator"/> class.
        /// </summary>
        /// <param name="mediator">The mediator used to send application commands, enabling decoupled command handling and asynchronous operations.</param>
        /// <param name="monitorIdleDetectionService">Service for detecting monitor idle state.</param>
        /// <param name="monitorSettingsFileService">Service for loading/saving monitor settings.</param>
        /// <param name="monitorStateWatcher">Service for watching monitor state changes.</param>
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
        /// Starts the orchestrator, subscribing to events and applying initial settings.
        /// </summary>
        public void Start()
        {
            SendRestoreBrightnessOnAllMonitorsCommand();
            SubscribeToEvents();
            InitializeStateWatcher();
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
        private void InitializeStateWatcher()
        {
            _monitorStateWatcher.Start();
        }

        /// <summary>
        /// Restores all monitors' brightness levels.
        /// </summary>
        public void RestoreAllMonitors()
        {
            Log.Information("Restoring all monitors brightness levels...");
            SendRestoreBrightnessOnAllMonitorsCommand();
        }

        #endregion Monitor State Initialization & Restoration

        #region Monitor State Event Handlers

        /// <summary>
        /// Handles monitor connection/disconnection events and triggers a full synchronization of monitor state.
        /// Dispatches a command to reconcile overlays, brightness, and idle detection state based on the old and new monitor lists.
        /// </summary>
        private void OnMonitorsChanged(object? sender, MonitorsChangedEventArgs e)
        {
            SendSynchronizeMonitorStateCommand(e.OldMonitors, e.NewMonitors);
        }

        /// <summary>
        /// Handles the event when a monitor becomes idle. Applies blackout or dimming as configured.
        /// </summary>
        private void OnMonitorBecameIdle(object? sender, MonitorIdleStateEventArgs e)
        {
            Log.Information("Orchestrator received MonitorBecameIdle event for Monitor #{DisplayNumber}.", e.DisplayNumber);
            switch (e.Settings.Behavior)
            {
                case MonitorBehavior.Blackout:
                    SendApplyBlackoutOverlayCommand(e);
                    break;

                case MonitorBehavior.Dim:
                    SendApplyDimCommand(e);
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Handles the event when a monitor becomes active. Dispatches a command to apply the active behavior, including any necessary filtering or restoration logic.
        /// </summary>
        private void OnMonitorBecameActive(object? sender, MonitorIdleStateEventArgs e)
        {
            SendApplyMonitorActiveBehaviorCommand(e);
        }

        /// <summary>
        /// Handles settings changed event and updates idle detection service.
        /// </summary>
        private void OnSettingsChanged(List<MonitorSettings> settings)
        {
            _monitorIdleDetectionService.UpdateSettings(settings);
            foreach (var setting in settings)
            {
                SendHideBlackoutOverlayCommand(setting.HardwareId);
                SendApplyUndimCommand(setting.HardwareId);
            }
        }

        #endregion Monitor State Event Handlers

        #region Command Senders

        /// <summary>
        /// Sends an apply blackout command for a monitor, including overlay and DDC/CI brightness if supported.
        /// </summary>
        /// <remarks>
        /// The blackout operation is performed asynchronously and is not awaited to avoid blocking the orchestrator's event loop.
        /// Any exceptions in the handler will be logged by the handler itself.
        /// </remarks>
        private void SendApplyBlackoutOverlayCommand(MonitorIdleStateEventArgs e)
        {
            var command = new ApplyBlackoutOverlayCommand
            {
                HardwareId = e.HardwareId
            };
            _mediator.SendAsync(command);
        }

        /// <summary>
        /// Sends a command to dim a monitor to the specified brightness level.
        /// </summary>
        /// <param name="e">The monitor state event arguments containing hardware ID and dim level.</param>
        /// <remarks>
        /// The dim operation is performed asynchronously and is not awaited to avoid blocking the orchestrator's event loop.
        /// Any exceptions in the handler will be logged by the handler itself.
        /// </remarks>
        private void SendApplyDimCommand(MonitorIdleStateEventArgs e)
        {
            var command = new ApplyDimCommand
            {
                HardwareId = e.HardwareId,
                DimLevel = (int)e.Settings.DimLevel
            };
            _mediator.SendAsync(command);
            Log.Information("ApplyDimCommand sent for monitor {HardwareId} to level {DimLevel}.", e.HardwareId, (int)e.Settings.DimLevel);
        }

        /// <summary>
        /// Sends a command to undim a monitor, restoring its original brightness.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        /// <remarks>
        /// The undim operation is performed asynchronously and is not awaited to avoid blocking the orchestrator's event loop.
        /// Any exceptions in the handler will be logged by the handler itself.
        /// </remarks>
        private void SendApplyUndimCommand(string hardwareId)
        {
            var command = new ApplyUndimCommand()
            {
                HardwareId = hardwareId
            };
            _mediator.SendAsync(command);
            Log.Information("UndimMonitorCommand sent for monitor {HardwareId}.", hardwareId);
        }

        /// <summary>
        /// Sends a command to hide the blackout overlay for a monitor.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        /// <remarks>
        /// The hide overlay operation is performed asynchronously and is not awaited to avoid blocking the orchestrator's event loop.
        /// Any exceptions in the handler will be logged by the handler itself.
        /// </remarks>
        private void SendHideBlackoutOverlayCommand(string hardwareId)
        {
            var command = new HideBlackoutOverlayCommand
            {
                HardwareId = hardwareId
            };
            _mediator.SendAsync(command);
            Log.Information("HideBlackoutOverlayCommand sent for monitor {HardwareId}.", hardwareId);
        }

        /// <summary>
        /// Sends a command to restore brightness for all monitors that were left dimmed from a previous session.
        /// </summary>
        private void SendRestoreBrightnessOnAllMonitorsCommand()
        {
            var command = new RestoreBrightnessOnAllMonitorsCommand();
            _mediator.SendAsync(command);
            Log.Information("RestoreBrightnessOnAllMonitorsCommand sent.");
        }

        /// <summary>
        /// Sends a command to synchronize the application's monitor state with the current set of connected monitors.
        /// This command triggers a full reconciliation of overlays, brightness, and idle detection state based on the provided monitor lists.
        /// </summary>
        /// <param name="oldMonitors">The list of monitors before the change.</param>
        /// <param name="newMonitors">The list of monitors after the change.</param>
        private void SendSynchronizeMonitorStateCommand(IReadOnlyList<MonitorInfo> oldMonitors, IReadOnlyList<MonitorInfo> newMonitors)
        {
            var command = new OLED_Sleeper.Commands.Monitor.State.SynchronizeMonitorStateCommand(oldMonitors, newMonitors);
            _mediator.SendAsync(command);
            Log.Information("SynchronizeMonitorStateCommand sent. Old: {OldCount}, New: {NewCount}", oldMonitors.Count, newMonitors.Count);
        }

        /// <summary>
        /// Sends a command to apply the active behavior to a monitor when it becomes active.
        /// This command encapsulates all logic for restoring the monitor's normal state, including filtering overlay-initiated activations.
        /// </summary>
        /// <param name="e">The monitor state event arguments containing hardware ID, state, and context.</param>
        private void SendApplyMonitorActiveBehaviorCommand(MonitorIdleStateEventArgs e)
        {
            var command = new ApplyMonitorActiveBehaviorCommand(e);
            _mediator.SendAsync(command);
            Log.Information("ApplyMonitorActiveBehaviorCommand sent for monitor {HardwareId}.", e.HardwareId);
        }

        #endregion Command Senders
    }
}