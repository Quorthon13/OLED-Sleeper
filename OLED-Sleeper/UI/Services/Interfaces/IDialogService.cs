using System.Windows;

namespace OLED_Sleeper.UI.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for showing a modal dialog to the user.
    /// Every member blocks until the user answers and must be called on the UI thread.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Asks a question with Yes, No and Cancel buttons.
        /// </summary>
        /// <param name="message">The question to show.</param>
        /// <param name="caption">The dialog's title.</param>
        /// <returns>The button the user chose.</returns>
        MessageBoxResult AskYesNoCancel(string message, string caption);

        /// <summary>
        /// Shows an error the user can only acknowledge.
        /// </summary>
        /// <param name="message">The error to show.</param>
        /// <param name="caption">The dialog's title.</param>
        void ShowError(string message, string caption);
    }
}
