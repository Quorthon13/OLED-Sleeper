// File: App.xaml.cs
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using OLED_Sleeper.Services;
using OLED_Sleeper.ViewModels;
using Serilog;
using System;
using System.Windows;
using System.Windows.Controls;

namespace OLED_Sleeper
{
    public partial class App : Application
    {
        private TaskbarIcon? notifyIcon;

        // Refactored: Use IServiceProvider for DI.
        private IServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LogSetup();
            ServiceSetup();
            WindowSetup();
        }

        private void WindowSetup()
        {
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
            this.MainWindow = mainWindow;
            mainWindow.Show();
            SetupTaskbarIcon();
        }

        private void ServiceSetup()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();

            var idleActivityService = _serviceProvider.GetRequiredService<IIdleActivityService>();
            idleActivityService.UpdateSettings(settingsService.LoadSettings());
            idleActivityService.Start();
        }

        private static void LogSetup()
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

        // Refactored: Method to configure services for DI.
        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IMonitorService, MonitorService>();
            services.AddSingleton<IMonitorLayoutService, MonitorLayoutService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IIdleActivityService, IdleActivityService>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();
        }

        private void SetupTaskbarIcon()
        {
            notifyIcon = new TaskbarIcon();
            notifyIcon.ToolTipText = "OLED Sleeper";
            notifyIcon.TrayMouseDoubleClick += (sender, args) => ShowMainWindow();

            notifyIcon.ContextMenu = new ContextMenu();
            var showMenuItem = new MenuItem { Header = "Show Settings" };
            showMenuItem.Click += (sender, args) => ShowMainWindow();
            notifyIcon.ContextMenu.Items.Add(showMenuItem);

            var exitMenuItem = new MenuItem { Header = "Exit" };
            exitMenuItem.Click += (sender, args) => ExitApplication();
            notifyIcon.ContextMenu.Items.Add(exitMenuItem);

            try
            {
                var iconUri = new Uri("pack://application:,,,/Assets/icon.ico", UriKind.RelativeOrAbsolute);
                notifyIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(iconUri).Stream);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load tray icon from resources.");
                notifyIcon.Icon = System.Drawing.SystemIcons.Application; // Fallback
            }
        }

        private void ShowMainWindow()
        {
            if (this.MainWindow != null)
            {
                if (this.MainWindow.IsVisible)
                {
                    if (this.MainWindow.WindowState == WindowState.Minimized)
                    {
                        this.MainWindow.WindowState = WindowState.Normal;
                    }
                    this.MainWindow.Activate();
                }
                else
                {
                    this.MainWindow.Show();
                }
            }
        }

        private void ExitApplication()
        {
            ShutdownApp();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ShutdownApp();
            base.OnExit(e);
        }

        private void ShutdownApp()
        {
            Log.Information("--- Application Exiting ---");
            Log.CloseAndFlush();
            notifyIcon?.Dispose();
            Current.Shutdown();
        }
    }
}