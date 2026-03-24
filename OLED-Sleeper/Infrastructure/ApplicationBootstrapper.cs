using Microsoft.Extensions.DependencyInjection;
using OLED_Sleeper.Core;
using OLED_Sleeper.Core.Interfaces;
using OLED_Sleeper.UI.Services.Interfaces;
using Serilog;
using System;
using System.Linq;
using System.Windows;

namespace OLED_Sleeper.Infrastructure
{
    /// <summary>
    /// Handles application startup, dependency injection, single-instance enforcement, orchestrator startup, and shutdown logic.
    /// Keeps <see cref="Application"/> subclasses lightweight and focused on WPF lifecycle events.
    /// </summary>
    public class ApplicationBootstrapper(string[] args) : IDisposable
    {
        private readonly ApplicationOptions _applicationOptions = CommandLineHelper.ParseArguments(args);

        private IServiceProvider? _serviceProvider;
        private ITrayIconService? _trayIconService;
        private IMainWindowService? _mainWindowService;
        private ApplicationInstanceManager? _instanceManager;
        private bool _isExiting = false;

        /// <summary>
        /// Initializes the application: logging, single-instance, DI, orchestrator, main window, and tray icon.
        /// </summary>
        public void Initialize()
        {
            LoggingConfigurator.Configure();
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
        /// Configures dependency injection services and builds the service provider using <see cref="ServiceConfigurator"/>.
        /// </summary>
        private void ConfigureServices()
        {
            _serviceProvider = ServiceConfigurator.ConfigureServices(_instanceManager!, _applicationOptions);
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
                () => ShutdownApp()
            );
        }

        /// <summary>
        /// Hooks up the delegate for showing the main window after DI and services are ready.
        /// </summary>
        private void HookInstanceManagerShowWindow()
        {
            _instanceManager?.SetShowMainWindowAction(() => _mainWindowService?.ShowMainWindow());
        }

        /// <summary>
        /// Performs shutdown logic, including log flush and tray icon disposal.
        /// Only the first instance will restore monitor states.
        /// </summary>
        public void ShutdownApp()
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
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Disposes resources used by the bootstrapper.
        /// </summary>
        public void Dispose()
        {
            _trayIconService?.Dispose();
            _instanceManager?.Dispose();
            (_serviceProvider as IDisposable)?.Dispose();
        }
    }
}