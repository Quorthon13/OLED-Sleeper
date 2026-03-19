using Hardcodet.Wpf.TaskbarNotification;
using OLED_Sleeper.UI.Helpers;
using OLED_Sleeper.UI.Services.Interfaces;
using Serilog;
using System.Windows;
using System.Windows.Controls;

namespace OLED_Sleeper.UI.Services
{
    /// <summary>
    /// Manages the tray icon, its context menu, and related events for the application.
    /// </summary>
    public class TrayIconService : ITrayIconService
    {
        private TaskbarIcon? _notifyIcon;
        private Action? _showMainWindowAction;
        private Action? _exitApplicationAction;

        /// <summary>
        /// Initializes and displays the tray icon and its context menu.
        /// Also wires up left-click and double-click to show the main window.
        /// </summary>
        /// <param name="showMainWindowAction">Action to show the main window.</param>
        /// <param name="exitApplicationAction">Action to exit the application.</param>
        public void Initialize(Action showMainWindowAction, Action exitApplicationAction)
        {
            _showMainWindowAction = showMainWindowAction ?? throw new ArgumentNullException(nameof(showMainWindowAction));
            _exitApplicationAction = exitApplicationAction ?? throw new ArgumentNullException(nameof(exitApplicationAction));

            _notifyIcon = new TaskbarIcon { ToolTipText = "OLED Sleeper" };

            WireTrayIconEvents();
            SetupContextMenu();
            SetTrayIcon();
        }

        /// <summary>
        /// Wires up tray icon mouse events for left-click and double-click actions.
        /// </summary>
        private void WireTrayIconEvents()
        {
            if (_notifyIcon == null) return;

            _notifyIcon.TrayMouseDoubleClick += (_, _) => _showMainWindowAction?.Invoke();
            _notifyIcon.TrayLeftMouseUp += (_, _) => _showMainWindowAction?.Invoke();
        }

        /// <summary>
        /// Sets up the tray icon's context menu.
        /// </summary>
        private void SetupContextMenu()
        {
            if (_notifyIcon == null) return;

            var contextMenu = new ContextMenu();

            contextMenu.Items.Add(CreateShowSettingsMenuItem());
            contextMenu.Items.Add(CreateStartupMenuItem());
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(CreateExitMenuItem());

            _notifyIcon.ContextMenu = contextMenu;
        }

        /// <summary>
        /// Creates the menu item used to show the main application settings window.
        /// </summary>
        /// <returns>A configured MenuItem for showing settings.</returns>
        private MenuItem CreateShowSettingsMenuItem()
        {
            var item = new MenuItem { Header = "Show Settings" };
            item.Click += (_, _) => _showMainWindowAction?.Invoke();
            return item;
        }

        /// <summary>
        /// Creates the menu item used to toggle the run-at-startup registry setting.
        /// </summary>
        /// <returns>A configured MenuItem for the startup toggle.</returns>
        private MenuItem CreateStartupMenuItem()
        {
            var item = new MenuItem
            {
                Header = "Run at Startup",
                IsCheckable = true,
                IsChecked = StartupHelper.IsRunAtStartupEnabled()
            };

            item.Click += HandleStartupMenuItemClick;
            return item;
        }

        /// <summary>
        /// Handles the click event for the startup menu item, updating the registry and reverting UI state on failure.
        /// </summary>
        private void HandleStartupMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item) return;

            try
            {
                StartupHelper.SetRunAtStartup(item.IsChecked);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to toggle startup registry key.");
                item.IsChecked = !item.IsChecked; // Revert UI state on failure
            }
        }

        /// <summary>
        /// Creates the menu item used to exit the application.
        /// </summary>
        /// <returns>A configured MenuItem for exiting.</returns>
        private MenuItem CreateExitMenuItem()
        {
            var item = new MenuItem { Header = "Exit" };
            item.Click += (_, _) => _exitApplicationAction?.Invoke();
            return item;
        }

        /// <summary>
        /// Loads and sets the tray icon from resources.
        /// </summary>
        private void SetTrayIcon()
        {
            if (_notifyIcon == null) return;

            try
            {
                var iconUri = new Uri("pack://application:,,,/Assets/icon.ico", UriKind.RelativeOrAbsolute);
                _notifyIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(iconUri)!.Stream);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load tray icon from resources.");
            }
        }

        /// <summary>
        /// Disposes the tray icon and releases resources.
        /// </summary>
        public void Dispose()
        {
            _notifyIcon?.Dispose();
        }
    }
}