using OLED_Sleeper.UI.Services.Interfaces;
using OLED_Sleeper.UI.ViewModels;
using System.Windows;

namespace OLED_Sleeper.UI.Services
{
    /// <summary>
    /// Provides methods to set up, show, and activate the main window.
    /// </summary>
    public class MainWindowService : IMainWindowService
    {
        private readonly MainWindow _mainWindow;
        private readonly MainViewModel _mainViewModel;

        /// <summary>
        /// Initializes a new instance of <see cref="MainWindowService"/>.
        /// </summary>
        /// <param name="mainWindow">The main application window.</param>
        /// <param name="mainViewModel">The main view model for the window.</param>
        public MainWindowService(MainWindow mainWindow, MainViewModel mainViewModel)
        {
            _mainWindow = mainWindow;
            _mainViewModel = mainViewModel;
        }

        /// <summary>
        /// Sets up the main window as the application's main window, assigns its data context, and shows it unless startHidden is true.
        /// </summary>
        public void SetupMainWindow(bool startHidden)
        {
            Application.Current.MainWindow = _mainWindow;
            _mainWindow.DataContext = _mainViewModel;

            if (!startHidden)
            {
                ShowMainWindow();
            }
            else
            {
                // Ensure the window is not visible at startup; tray icon will be used to show it.
                _mainWindow.Hide();
            }
        }

        /// <summary>
        /// Brings the main window to the foreground and restores it if minimized.
        /// </summary>
        public void ShowMainWindow()
        {
            _mainWindow.Show();
            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }
            _mainWindow.Activate();
        }
    }
}
