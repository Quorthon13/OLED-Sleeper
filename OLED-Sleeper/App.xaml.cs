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