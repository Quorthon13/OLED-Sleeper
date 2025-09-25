using Hardcodet.Wpf.TaskbarNotification;
using Serilog;
using System;
using System.Windows;
using System.Windows.Controls;
using OLED_Sleeper.UI.Services.Interfaces;

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
            _showMainWindowAction = showMainWindowAction;
            _exitApplicationAction = exitApplicationAction;
            _notifyIcon = new TaskbarIcon
            {
                ToolTipText = "OLED Sleeper"
            };
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
            // Show main window on double-click
            _notifyIcon.TrayMouseDoubleClick += (_, _) => _showMainWindowAction?.Invoke();
            // Show main window on left mouse up (single left click)
            _notifyIcon.TrayLeftMouseUp += (_, _) => _showMainWindowAction?.Invoke();
        }

        /// <summary>
        /// Sets up the tray icon's context menu.
        /// </summary>
        private void SetupContextMenu()
        {
            if (_notifyIcon == null) return;
            var contextMenu = new ContextMenu();
            var showMenuItem = new MenuItem { Header = "Show Settings" };
            showMenuItem.Click += (_, _) => _showMainWindowAction?.Invoke();
            contextMenu.Items.Add(showMenuItem);

            var exitMenuItem = new MenuItem { Header = "Exit" };
            exitMenuItem.Click += (_, _) => _exitApplicationAction?.Invoke();
            contextMenu.Items.Add(exitMenuItem);

            _notifyIcon.ContextMenu = contextMenu;
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
                _notifyIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(iconUri).Stream);
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