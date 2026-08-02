using Microsoft.Extensions.DependencyInjection;
using OLED_Sleeper.Core;
using OLED_Sleeper.Core.Interfaces;
using OLED_Sleeper.Infrastructure.Helpers;
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
        /// <summary>
        /// How long shutdown waits for the monitor restore to finish.
        /// </summary>
        private const int RestoreOnShutdownTimeoutMs = 10000;

        private readonly ApplicationOptions _applicationOptions = CommandLineHelper.ParseArguments(args);

        private IServiceProvider? _serviceProvider;
        private ITrayIconService? _trayIconService;
        private IMainWindowService? _mainWindowService;
        private ApplicationInstanceManager? _instanceManager;
        private IApplicationOrchestrator? _orchestrator;
        private bool _isExiting = false;

        /// <summary>
        /// Initializes the application: logging, single-instance, DI, orchestrator, main window, and tray icon.
        /// A second instance stops after the single-instance check and starts no services.
        /// </summary>
        public void Initialize()
        {
            LoggingConfigurator.Configure();
            InitializeInstanceManager();

            if (!_instanceManager!.IsFirstInstance)
            {
                Log.Information("Another instance is already running. Exiting without starting any services.");
                return;
            }

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
                _orchestrator = _serviceProvider.GetRequiredService<IApplicationOrchestrator>();
                _orchestrator.Start();
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
        /// Stops the orchestrator, disposes the tray icon and the instance manager, flushes the log, and exits.
        /// A second instance has no orchestrator and restores nothing.
        /// </summary>
        public void ShutdownApp()
        {
            if (_isExiting) return; // Prevent re-entrancy
            _isExiting = true;

            StopOrchestrator();

            _trayIconService?.Dispose();
            _instanceManager?.Dispose();

            Log.Information("--- Application Exiting ---");
            Log.CloseAndFlush();

            Application.Current?.Shutdown();
        }

        /// <summary>
        /// Stops the orchestrator and blocks until the monitor restore finishes or
        /// <see cref="RestoreOnShutdownTimeoutMs"/> elapses.
        /// </summary>
        /// <remarks>
        /// The restore runs on the thread pool: by the time <c>OnExit</c> reaches this, the UI thread is no
        /// longer processing dispatcher messages. On timeout the brightness state file is left unchanged and
        /// the next launch restores the monitor.
        /// </remarks>
        private void StopOrchestrator()
        {
            if (_orchestrator == null) return;

            Log.Information("Shutdown initiated. Restoring all monitors...");

            try
            {
                var stopTask = Task.Run(() => _orchestrator.StopAsync());
                if (!stopTask.Wait(RestoreOnShutdownTimeoutMs))
                {
                    Log.Warning("Monitor restore did not finish within {TimeoutMs} ms. Exiting anyway.", RestoreOnShutdownTimeoutMs);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to stop the orchestrator during shutdown.");
            }
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