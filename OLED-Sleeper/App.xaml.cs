using Microsoft.Extensions.DependencyInjection;
using OLED_Sleeper.Core;
using OLED_Sleeper.Core.Interfaces;
using OLED_Sleeper.Features.MonitorBehavior.Commands;
using OLED_Sleeper.Features.MonitorBehavior.Handlers;
using OLED_Sleeper.Features.MonitorBlackout.Commands;
using OLED_Sleeper.Features.MonitorBlackout.Handlers;
using OLED_Sleeper.Features.MonitorBlackout.Services;
using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;
using OLED_Sleeper.Features.MonitorDimming.Commands;
using OLED_Sleeper.Features.MonitorDimming.Handlers;
using OLED_Sleeper.Features.MonitorDimming.Services;
using OLED_Sleeper.Features.MonitorDimming.Services.Interfaces;
using OLED_Sleeper.Features.MonitorIdleDetection.Services;
using OLED_Sleeper.Features.MonitorIdleDetection.Services.Interfaces;
using OLED_Sleeper.Features.MonitorInformation.Services;
using OLED_Sleeper.Features.MonitorInformation.Services.Interfaces;
using OLED_Sleeper.Features.MonitorState.Commands;
using OLED_Sleeper.Features.MonitorState.Handlers;
using OLED_Sleeper.Features.MonitorState.Services;
using OLED_Sleeper.Features.MonitorState.Services.Interfaces;
using OLED_Sleeper.Features.UserSettings.Services;
using OLED_Sleeper.Features.UserSettings.Services.Interfaces;
using OLED_Sleeper.UI.Services;
using OLED_Sleeper.UI.Services.Interfaces;
using OLED_Sleeper.UI.ViewModels;
using Serilog;
using System.Windows;
using System.Windows.Controls;

namespace OLED_Sleeper
{
    /// <summary>
    /// Interaction logic for the application. Handles startup, DI, logging, single-instance enforcement, and tray icon.
    /// </summary>
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;
        private bool _isExiting = false;
        private ITrayIconService? _trayIconService;
        private IMainWindowService? _mainWindowService;
        private ApplicationInstanceManager? _instanceManager;

        /// <summary>
        /// Application entry point. Sets up logging, single-instance enforcement, services, and main window.
        /// </summary>
        /// <param name="e">Startup event arguments.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            this.SessionEnding += App_SessionEnding;

            SetupLogging();
            InitializeInstanceManager();
            ConfigureServices();
            StartOrchestrator();
            SetupMainWindowService();
            SetupTrayIconService();
            HookInstanceManagerShowWindow();
        }

        /// <summary>
        /// Initializes the single-instance manager before any other services.
        /// </summary>
        private void InitializeInstanceManager()
        {
            _instanceManager = new ApplicationInstanceManager();
            _instanceManager.Initialize();
        }

        /// <summary>
        /// Hooks up the delegate for showing the main window after DI and services are ready.
        /// </summary>
        private void HookInstanceManagerShowWindow()
        {
            _instanceManager?.SetShowMainWindowAction(() => _mainWindowService?.ShowMainWindow());
        }

        /// <summary>
        /// Configures dependency injection services.
        /// </summary>
        private void ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IMediator, Mediator>();

            services.AddTransient<ICommandHandler<ApplyMonitorActiveBehaviorCommand>, ApplyMonitorActiveBehaviorCommandHandler>();
            services.AddTransient<ICommandHandler<ApplyBlackoutOverlayCommand>, ApplyBlackoutOverlayCommandHandler>();
            services.AddTransient<ICommandHandler<HideBlackoutOverlayCommand>, HideBlackoutOverlayCommandHandler>();
            services.AddTransient<ICommandHandler<ApplyDimCommand>, ApplyDimCommandHandler>();
            services.AddTransient<ICommandHandler<ApplyUndimCommand>, ApplyUndimCommandHandler>();
            services.AddTransient<ICommandHandler<RestoreBrightnessOnAllMonitorsCommand>, RestoreBrightnessOnAllMonitorsCommandHandler>();
            services.AddTransient<ICommandHandler<SynchronizeMonitorStateCommand>, SynchronizeMonitorStateCommandHandler>();

            services.AddSingleton<IMonitorInfoManager, MonitorInfoManager>();
            services.AddSingleton<IMonitorStateWatcher, MonitorStateWatcher>();
            services.AddSingleton<IMonitorBrightnessStateService, MonitorBrightnessStateService>();
            services.AddSingleton<IMonitorDimmingService, MonitorDimmingService>();
            services.AddSingleton<IMonitorBlackoutService, MonitorBlackoutService>();
            services.AddSingleton<IApplicationOrchestrator, ApplicationOrchestrator>();
            services.AddSingleton<IWorkspaceService, WorkspaceService>();
            services.AddSingleton<IMonitorInfoProvider, MonitorInfoProvider>();
            services.AddSingleton<IMonitorLayoutService, MonitorLayoutService>();
            services.AddSingleton<IMonitorSettingsFileService, MonitorSettingsFileService>();
            services.AddSingleton<IMonitorIdleDetectionService, MonitorIdleDetectionService>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();
            services.AddSingleton<ITrayIconService, TrayIconService>();
            services.AddSingleton<IMainWindowService, MainWindowService>();
            services.AddSingleton<IApplicationInstanceManager>(_ => _instanceManager!);

            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Starts the application orchestrator service.
        /// </summary>
        private void StartOrchestrator()
        {
            if (_serviceProvider != null)
            {
                var orchestrator = _serviceProvider.GetRequiredService<IApplicationOrchestrator>();
                orchestrator.Start();
            }
        }

        /// <summary>
        /// Sets up the main window service and its data context.
        /// </summary>
        private void SetupMainWindowService()
        {
            if (_serviceProvider == null) return;
            _mainWindowService = _serviceProvider.GetRequiredService<IMainWindowService>();
            _mainWindowService.SetupMainWindow();
        }

        /// <summary>
        /// Configures and displays the tray icon using the tray icon service.
        /// </summary>
        private void SetupTrayIconService()
        {
            if (_serviceProvider == null) return;
            _trayIconService = _serviceProvider.GetRequiredService<ITrayIconService>();
            _trayIconService.Initialize(
                () => _mainWindowService?.ShowMainWindow(),
                () => ExitApplication()
            );
        }

        /// <summary>
        /// Initiates application shutdown.
        /// </summary>
        private void ExitApplication()
        {
            ShutdownApp();
        }

        /// <summary>
        /// Handles application exit, ensuring resources are disposed and logs are flushed.
        /// </summary>
        /// <param name="e">Exit event arguments.</param>
        protected override void OnExit(ExitEventArgs e)
        {
            ShutdownApp();
            base.OnExit(e);
        }

        /// <summary>
        /// Performs shutdown logic, including log flush and tray icon disposal.
        /// Only the first instance will restore monitor states.
        /// </summary>
        private void ShutdownApp()
        {
            if (_isExiting) return; // Prevent re-entrancy
            _isExiting = true;

            if (_instanceManager?.IsFirstInstance == true)
            {
                Log.Information("Shutdown initiated. Restoring all monitors...");
                ApplicationNotifications.TriggerRestoreAllMonitors();
            }

            Log.Information("--- Application Exiting ---");
            Log.CloseAndFlush();

            _trayIconService?.Dispose();
            _instanceManager?.Dispose();

            Current.Shutdown();
        }

        /// <summary>
        /// Configures Serilog logging for the application.
        /// </summary>
        private static void SetupLogging()
        {
            var logPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OLED-Sleeper", "Logs", "log-.txt");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("--- Application Starting ---");
        }

        /// <summary>
        /// Handles session ending events (e.g., user logoff or system shutdown).
        /// </summary>
        private void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            ShutdownApp();
        }
    }
}