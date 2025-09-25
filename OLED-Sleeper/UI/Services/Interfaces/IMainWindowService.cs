namespace OLED_Sleeper.UI.Services.Interfaces
{
    /// <summary>
    /// Provides methods to set up, show, and activate the main application window.
    /// </summary>
    public interface IMainWindowService
    {
        /// <summary>
        /// Sets up the main window as the application's main window, assigns its data context, and shows it.
        /// </summary>
        void SetupMainWindow();

        /// <summary>
        /// Brings the main window to the foreground and restores it if minimized.
        /// </summary>
        void ShowMainWindow();
    }
}