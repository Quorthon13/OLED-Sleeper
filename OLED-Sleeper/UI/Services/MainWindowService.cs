using Microsoft.Extensions.Options;
using OLED_Sleeper.Infrastructure;
using OLED_Sleeper.UI.Services.Interfaces;
using OLED_Sleeper.UI.ViewModels;
using System.Windows;

namespace OLED_Sleeper.UI.Services
{
    /// <summary>
    /// Provides methods to set up, show, and activate the main window.
    /// </summary>
    /// <param name="mainWindow">The main application window.</param>
    /// <param name="mainViewModel">The main view model for the window.</param>
    /// <param name="options">The application configuration options.</param>
    public class MainWindowService(MainWindow mainWindow, MainViewModel mainViewModel, IOptions<ApplicationOptions> options) : IMainWindowService
    {
        private readonly ApplicationOptions _options = options.Value;

        /// <summary>
        /// Sets up the main window as the application's main window, assigns its data context, 
        /// and determines its initial visibility based on the configured application options.
        /// </summary>
        public void SetupMainWindow()
        {
            Application.Current.MainWindow = mainWindow;
            mainWindow.DataContext = mainViewModel;

            if (_options.StartHidden)
            {
                mainWindow.Hide();
            }
            else
            {
                ShowMainWindow();
            }
        }

        /// <summary>
        /// Brings the main window to the foreground and restores it if minimized.
        /// </summary>
        public void ShowMainWindow()
        {
            mainWindow.Show();
            if (mainWindow.WindowState == WindowState.Minimized)
            {
                mainWindow.WindowState = WindowState.Normal;
            }
            mainWindow.Activate();
        }
    }
}
