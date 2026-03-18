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
using OLED_Sleeper.Infrastructure;

namespace OLED_Sleeper
{
    /// <summary>
    /// Interaction logic for the application. Handles startup, DI, logging, single-instance enforcement, and tray icon.
    /// </summary>
    /// <summary>
    /// WPF application class. Handles only WPF lifecycle events and delegates startup/shutdown to <see cref="ApplicationBootstrapper"/>.
    /// </summary>
    public partial class App : Application
    {
        private ApplicationBootstrapper? _bootstrapper;

        /// <summary>
        /// Handles application startup. Delegates all initialization to <see cref="ApplicationBootstrapper"/>.
        /// </summary>
        /// <param name="e">Startup event arguments.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            this.SessionEnding += App_SessionEnding;
            _bootstrapper = new ApplicationBootstrapper();
            _bootstrapper.Initialize();
        }

        /// <summary>
        /// Handles application exit. Delegates shutdown to <see cref="ApplicationBootstrapper"/>.
        /// </summary>
        /// <param name="e">Exit event arguments.</param>
        protected override void OnExit(ExitEventArgs e)
        {
            _bootstrapper?.ShutdownApp();
            _bootstrapper?.Dispose();
            base.OnExit(e);
        }

        /// <summary>
        /// Handles session ending events (e.g., user logoff or system shutdown).
        /// Delegates shutdown to <see cref="ApplicationBootstrapper"/>.
        /// </summary>
        private void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            _bootstrapper?.ShutdownApp();
        }
    }
}