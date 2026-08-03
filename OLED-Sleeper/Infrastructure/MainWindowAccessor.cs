using OLED_Sleeper.Core.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace OLED_Sleeper.Infrastructure
{
    /// <summary>
    /// Reaches the main window through <see cref="Application.MainWindow"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class MainWindowAccessor : IMainWindowAccessor
    {
        /// <inheritdoc />
        public void SetMainWindow(Window window) => Application.Current.MainWindow = window;

        /// <inheritdoc />
        public void HideMainWindow() => Application.Current.MainWindow?.Hide();
    }
}
