using System.Windows;

namespace OLED_Sleeper.UI.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for reaching the window the application treats as its main window.
    /// </summary>
    public interface IMainWindowAccessor
    {
        /// <summary>
        /// Registers the window as the application's main window. Must be called on the UI thread.
        /// </summary>
        /// <param name="window">The window to register.</param>
        void SetMainWindow(Window window);

        /// <summary>
        /// Hides the main window. Does nothing when no main window has been registered.
        /// Must be called on the UI thread.
        /// </summary>
        void HideMainWindow();
    }
}
