namespace OLED_Sleeper.UI.Services.Interfaces
{
    /// <summary>
    /// Provides methods to set up, show, and activate the main application window.
    /// </summary>
    public interface IMainWindowService
    {
        /// <summary>
        /// Sets up the main window as the application's main window, assigns its data context,
        /// and optionally shows it.
        /// </summary>
        /// <param name="startHidden">If true, the main window will not be shown on startup (useful for tray-first startup).</param>
        void SetupMainWindow(bool startHidden);

        /// <summary>
        /// Brings the main window to the foreground and restores it if minimized.
        /// </summary>
        void ShowMainWindow();
    }
}
