using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using OLED_Sleeper.Core;
using OLED_Sleeper.Core.Interfaces;
using OLED_Sleeper.Services.Workspace;
using OLED_Sleeper.Services.Workspace.Interfaces;
using OLED_Sleeper.ViewModels;
using Serilog;
using System.Windows;
using System.Windows.Controls;
using OLED_Sleeper.Commands.Monitor.Blackout;
using OLED_Sleeper.Commands.Monitor.Dimming;
using OLED_Sleeper.Handlers.Monitor.Dim;
using OLED_Sleeper.Handlers.Monitor.Blackout;
using OLED_Sleeper.Handlers;
using OLED_Sleeper.Services.Core;
using OLED_Sleeper.Services.Core.Interfaces;
using OLED_Sleeper.Services.Monitor.Blackout;
using OLED_Sleeper.Services.Monitor.Blackout.Interfaces;
using OLED_Sleeper.Services.Monitor.Dimming;
using OLED_Sleeper.Services.Monitor.Dimming.Interfaces;
using OLED_Sleeper.Services.Monitor.IdleDetection;
using OLED_Sleeper.Services.Monitor.IdleDetection.Interfaces;
using OLED_Sleeper.Services.Monitor.Info;
using OLED_Sleeper.Services.Monitor.Info.Interfaces;
using OLED_Sleeper.Services.UI;
using OLED_Sleeper.Services.UI.Interfaces;
using OLED_Sleeper.Services.Monitor.Settings.Interfaces;
using OLED_Sleeper.Services.Monitor.Settings;

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

            services.AddTransient<ICommandHandler<ApplyBlackoutOverlayCommand>, ApplyBlackoutOverlayCommandHandler>();
            services.AddTransient<ICommandHandler<HideBlackoutOverlayCommand>, HideBlackoutOverlayCommandHandler>();
            services.AddTransient<ICommandHandler<ApplyDimCommand>, ApplyDimCommandHandler>();
            services.AddTransient<ICommandHandler<ApplyUndimCommand>, ApplyUndimCommandHandler>();
            services.AddTransient<ICommandHandler<RestoreBrightnessOnStartupCommand>, RestoreBrightnessOnStartupCommandHandler>();

            services.AddSingleton<IMonitorInfoManager, MonitorInfoManager>();
            services.AddSingleton<IMonitorStateWatcher, MonitorStateWatcher>();
            services.AddSingleton<IMonitorBrightnessStateService, MonitorBrightnessStateService>();
            services.AddSingleton<IMonitorDimmingService, MonitorDimmingService>();
            services.AddSingleton<IMonitorBlackoutService, MonitorBlackoutService>();
            services.AddSingleton<IApplicationOrchestrator, ApplicationOrchestrator>();
            services.AddSingleton<IWorkspaceService, WorkspaceService>();
            services.AddSingleton<IMonitorSettingsValidationService, MonitorSettingsValidationService>();
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

            AppNotifications.TriggerRestoreAllMonitors();

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