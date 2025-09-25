using System;

namespace OLED_Sleeper.UI.Services.Interfaces
{
    /// <summary>
    /// Provides functionality for managing the tray icon and its context menu.
    /// </summary>
    public interface ITrayIconService : IDisposable
    {
        /// <summary>
        /// Initializes and displays the tray icon and its context menu.
        /// </summary>
        /// <param name="showMainWindowAction">Action to show the main window.</param>
        /// <param name="exitApplicationAction">Action to exit the application.</param>
        void Initialize(Action showMainWindowAction, Action exitApplicationAction);
    }
}
