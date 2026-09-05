using OLED_Sleeper.UI.Services.Interfaces;
using System.Windows;

namespace OLED_Sleeper.UI.Services
{
    /// <summary>
    /// Prompts through <see cref="IDialogService"/>, saving the settings on Yes and discarding them on No.
    /// </summary>
    public class UnsavedSettingsService(
        IUnsavedSettings unsavedSettings,
        IDialogService dialogService) : IUnsavedSettingsService
    {
        private const string Caption = "Unsaved Changes";

        /// <inheritdoc />
        public bool ConfirmHide() =>
            Confirm("You have unsaved changes. Would you like to save them before hiding the window?");

        /// <inheritdoc />
        public bool ConfirmExit() =>
            Confirm("You have unsaved changes. Would you like to save them before exiting?");

        /// <summary>
        /// Prompts only when there is something to lose. Yes answers with whether the save succeeded;
        /// No discards the changes.
        /// </summary>
        private bool Confirm(string question)
        {
            if (!unsavedSettings.IsDirty) return true;

            var result = dialogService.AskYesNoCancel(question, Caption);

            if (result == MessageBoxResult.Cancel) return false;

            if (result == MessageBoxResult.Yes) return unsavedSettings.TrySaveChanges();

            unsavedSettings.DiscardChanges();
            return true;
        }
    }
}
