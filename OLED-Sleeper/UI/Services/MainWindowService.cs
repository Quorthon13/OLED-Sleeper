using Microsoft.Extensions.Options;
using OLED_Sleeper.Infrastructure.Hosting;
using OLED_Sleeper.UI.Services.Interfaces;
using OLED_Sleeper.UI.ViewModels;
using System.Windows;

namespace OLED_Sleeper.UI.Services
{
    /// <summary>
    /// Provides methods to set up, show, and activate the main window.
    /// </summary>
    public class MainWindowService(
        MainWindow mainWindow,
        MainViewModel mainViewModel,
        IOptions<ApplicationOptions> options,
        IMainWindowAccessor mainWindowAccessor) : IMainWindowService
    {
        private readonly ApplicationOptions _options = options.Value;

        /// <summary>
        /// Sets up the main window as the application's main window, assigns its data context, 
        /// and determines its initial visibility based on the configured application options.
        /// </summary>
        public void SetupMainWindow()
        {
            mainWindowAccessor.SetMainWindow(mainWindow);
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
