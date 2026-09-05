using OLED_Sleeper.UI.Services.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace OLED_Sleeper.UI.Services
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
        public Window? MainWindow => Application.Current?.MainWindow;
    }
}
