using System.Windows;
using OLED_Sleeper.Infrastructure;

namespace OLED_Sleeper
{
    /// <summary>
    /// Entry point for the WPF application. 
    /// Handles WPF lifecycle events and delegates application initialization
    /// and shutdown to <see cref="ApplicationBootstrapper"/>.
    /// </summary>
    /// <remarks>
    /// This class intentionally contains minimal logic. All application
    /// initialization, dependency setup, and runtime orchestration are handled
    /// by <see cref="ApplicationBootstrapper"/>.
    /// </remarks>
    public partial class App : Application
    {
        private ApplicationBootstrapper? _bootstrapper;

        /// <summary>
        /// Invoked when the application starts. Creates and initializes the
        /// <see cref="ApplicationBootstrapper"/> using the provided startup arguments.
        /// </summary>
        /// <param name="e">Startup event arguments containing command-line parameters.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            this.SessionEnding += App_SessionEnding;
            StartBootstrapper(e);
        }

        /// <summary>
        /// Invoked when the application is exiting. Ensures the bootstrapper
        /// performs its shutdown logic and releases resources.
        /// </summary>
        /// <param name="e">Exit event arguments.</param>
        protected override void OnExit(ExitEventArgs e)
        {
            _bootstrapper?.ShutdownApp();
            _bootstrapper?.Dispose();
            base.OnExit(e);
        }

        /// <summary>
        /// Handles Windows session ending events (e.g., user logoff or system shutdown)
        /// and forwards shutdown handling to the bootstrapper.
        /// </summary>
        private void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            _bootstrapper?.ShutdownApp();
        }

        /// <summary>
        /// Creates and initializes the <see cref="ApplicationBootstrapper"/>
        /// using the provided startup arguments.
        /// </summary>
        private void StartBootstrapper(StartupEventArgs e)
        {
            _bootstrapper = new ApplicationBootstrapper(e.Args);
            _bootstrapper.Initialize();
        }
    }
}