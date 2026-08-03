using OLED_Sleeper.UI.Services.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace OLED_Sleeper.UI.Services
{
    /// <summary>
    /// Shows dialogs through <see cref="MessageBox"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class MessageBoxDialogService : IDialogService
    {
        /// <inheritdoc />
        public MessageBoxResult AskYesNoCancel(string message, string caption) =>
            MessageBox.Show(message, caption, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

        /// <inheritdoc />
        public void ShowError(string message, string caption) =>
            MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
