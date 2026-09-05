namespace OLED_Sleeper.UI.Services.Interfaces
{
    /// <summary>
    /// Asks the user what to do about unsaved monitor settings before an action that would lose them.
    /// Both members block on a modal dialog and must be called on the UI thread.
    /// </summary>
    public interface IUnsavedSettingsService
    {
        /// <summary>
        /// Asks before the settings window is hidden.
        /// </summary>
        /// <returns>True to go ahead and hide the window, false to leave it as it is.</returns>
        bool ConfirmHide();

        /// <summary>
        /// Asks before the application exits.
        /// </summary>
        /// <returns>True to go ahead and exit, false to keep the application running.</returns>
        bool ConfirmExit();
    }
}
