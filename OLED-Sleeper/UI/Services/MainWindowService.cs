using Microsoft.Extensions.Options;
using OLED_Sleeper.Infrastructure.Hosting;
using OLED_Sleeper.UI.Services.Interfaces;
using OLED_Sleeper.UI.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace OLED_Sleeper.UI.Services
{
    /// <summary>
    /// Provides methods to set up, show, and activate the main window, and hides it rather than
    /// letting it close.
    /// </summary>
    public class MainWindowService(
        MainWindow mainWindow,
        MainViewModel mainViewModel,
        IOptions<ApplicationOptions> options,
        IMainWindowAccessor mainWindowAccessor,
        IUnsavedSettingsService unsavedSettingsService) : IMainWindowService
    {
        private readonly ApplicationOptions _options = options.Value;

        /// <summary>
        /// True once the application has started exiting.
        /// </summary>
        private bool _isShuttingDown;

        /// <inheritdoc />
        public void SetupMainWindow()
        {
            mainWindowAccessor.SetMainWindow(mainWindow);
            mainWindow.DataContext = mainViewModel;
            mainWindow.Closing += OnMainWindowClosing;

            if (_options.StartHidden)
            {
                mainWindow.Hide();
            }
            else
            {
                ShowMainWindow();
            }
        }

        /// <inheritdoc />
        public void ShowMainWindow()
        {
            mainWindow.Show();
            if (mainWindow.WindowState == WindowState.Minimized)
            {
                mainWindow.WindowState = WindowState.Normal;
            }
            mainWindow.Activate();
        }

        /// <inheritdoc />
        public void PrepareForShutdown() => _isShuttingDown = true;

        /// <summary>
        /// Hides the window instead of closing it, once the user has answered for any unsaved settings.
        /// The close is left alone during shutdown.
        /// </summary>
        private void OnMainWindowClosing(object? sender, CancelEventArgs e)
        {
            if (_isShuttingDown) return;

            e.Cancel = true;

            if (unsavedSettingsService.ConfirmHide())
            {
                mainWindow.Hide();
            }
        }
    }
}
