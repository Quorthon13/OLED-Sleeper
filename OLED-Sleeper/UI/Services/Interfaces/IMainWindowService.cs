namespace OLED_Sleeper.UI.Services.Interfaces
{
    /// <summary>
    /// Provides methods to set up, show, and activate the main application window, and decides what
    /// closing it does.
    /// </summary>
    public interface IMainWindowService
    {
        /// <summary>
        /// Sets up the main window as the application's main window, assigns its data context, 
        /// and determines its initial visibility based on the configured application options.
        /// </summary>
        void SetupMainWindow();

        /// <summary>
        /// Brings the main window to the foreground and restores it if minimized.
        /// </summary>
        void ShowMainWindow();

        /// <summary>
        /// Stops the window being hidden on close, for the closes WPF performs as the application exits.
        /// </summary>
        void PrepareForShutdown();
    }
}
