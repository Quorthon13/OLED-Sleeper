using Hardcodet.Wpf.TaskbarNotification;
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
    /// Interaction logic for the application. Handles startup, DI, logging, and tray icon.
    /// </summary>
    public partial class App : Application
    {
        private TaskbarIcon? _notifyIcon;
        private IServiceProvider? _serviceProvider;
        private bool _isExiting = false;

        /// <summary>
        /// Application entry point. Sets up logging, services, and main window.
        /// </summary>
        /// <param name="e">Startup event arguments.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            this.SessionEnding += App_SessionEnding;

            SetupLogging();
            ConfigureServices();
            StartOrchestrator();
            SetupTrayIcon();
            SetupMainWindow();
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
        /// Sets up the main window and its data context.
        /// </summary>
        private void SetupMainWindow()
        {
            if (_serviceProvider == null) return;
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
            MainWindow = mainWindow;
            mainWindow.Show();
        }

        /// <summary>
        /// Configures and displays the tray icon and its context menu.
        /// </summary>
        private void SetupTrayIcon()
        {
            _notifyIcon = new TaskbarIcon
            {
                ToolTipText = "OLED Sleeper"
            };
            _notifyIcon.TrayMouseDoubleClick += (_, _) => ShowMainWindow();

            var contextMenu = new ContextMenu();
            var showMenuItem = new MenuItem { Header = "Show Settings" };
            showMenuItem.Click += (_, _) => ShowMainWindow();
            contextMenu.Items.Add(showMenuItem);

            var exitMenuItem = new MenuItem { Header = "Exit" };
            exitMenuItem.Click += (_, _) => ExitApplication();
            contextMenu.Items.Add(exitMenuItem);

            _notifyIcon.ContextMenu = contextMenu;

            try
            {
                var iconUri = new Uri("pack://application:,,,/Assets/icon.ico", UriKind.RelativeOrAbsolute);
                _notifyIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(iconUri).Stream);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load tray icon from resources.");
            }
        }

        /// <summary>
        /// Brings the main window to the foreground and restores it if minimized.
        /// </summary>
        private void ShowMainWindow()
        {
            if (MainWindow != null)
            {
                MainWindow.Show();
                if (MainWindow.WindowState == WindowState.Minimized)
                {
                    MainWindow.WindowState = WindowState.Normal;
                }
                MainWindow.Activate();
            }
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
        /// </summary>
        private void ShutdownApp()
        {
            if (_isExiting) return; // Prevent re-entrancy
            _isExiting = true;

            Log.Information("Shutdown initiated. Restoring all monitors...");

            ApplicationNotifications.TriggerRestoreAllMonitors();

            Log.Information("--- Application Exiting ---");
            Log.CloseAndFlush();
            _notifyIcon?.Dispose();
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

        private void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            // This event is raised when the user is logging off or the system is shutting down.
            // We can perform our cleanup here.
            ShutdownApp();
        }
    }
}