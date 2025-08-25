using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using OLED_Sleeper.Events;
using OLED_Sleeper.Services;
using OLED_Sleeper.ViewModels;
using Serilog;
using System;
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
            SetupMainWindow();
            SetupTaskbarIcon();
        }

        /// <summary>
        /// Configures dependency injection services.
        /// </summary>
        private void ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IMonitorManagerService, MonitorManagerService>();
            services.AddSingleton<IBrightnessStateService, BrightnessStateService>();
            services.AddSingleton<IDimmerService, DimmerService>();
            services.AddSingleton<IOverlayService, OverlayService>();
            services.AddSingleton<IApplicationOrchestrator, ApplicationOrchestrator>();
            services.AddSingleton<IWorkspaceService, WorkspaceService>();
            services.AddSingleton<ISaveValidationService, SaveValidationService>();
            services.AddSingleton<IMonitorService, MonitorService>();
            services.AddSingleton<IMonitorLayoutService, MonitorLayoutService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IIdleActivityService, IdleActivityService>();
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
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
            MainWindow = mainWindow;
            mainWindow.Show();
        }

        /// <summary>
        /// Configures and displays the tray icon and its context menu.
        /// </summary>
        private void SetupTaskbarIcon()
        {
            _notifyIcon = new TaskbarIcon
            {
                ToolTipText = "OLED Sleeper"
            };
            _notifyIcon.TrayMouseDoubleClick += (sender, args) => ShowMainWindow();

            var contextMenu = new ContextMenu();
            var showMenuItem = new MenuItem { Header = "Show Settings" };
            showMenuItem.Click += (sender, args) => ShowMainWindow();
            contextMenu.Items.Add(showMenuItem);

            var exitMenuItem = new MenuItem { Header = "Exit" };
            exitMenuItem.Click += (sender, args) => ExitApplication();
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

            AppEvents.TriggerRestoreAllMonitors();

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