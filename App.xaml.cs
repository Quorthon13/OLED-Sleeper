using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using Serilog;

// Note: C# namespaces can't use hyphens, so we use an underscore.
namespace OLED_Sleeper
{
    public partial class App : Application
    {
        private TaskbarIcon? notifyIcon;
        private MainWindow? mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            var logPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OLED-Sleeper", "Logs", "log-.txt");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("--- Application Starting ---");

            base.OnStartup(e);

            mainWindow = new MainWindow();

            notifyIcon = new TaskbarIcon();
            notifyIcon.ToolTipText = "OLED Sleeper";
            notifyIcon.TrayMouseDoubleClick += (sender, args) => ShowMainWindow();

            notifyIcon.ContextMenu = new ContextMenu();
            var showMenuItem = new MenuItem { Header = "Show Settings" };
            showMenuItem.Click += (sender, args) => ShowMainWindow();
            notifyIcon.ContextMenu.Items.Add(showMenuItem);

            var exitMenuItem = new MenuItem { Header = "Exit" };
            exitMenuItem.Click += (sender, args) => ExitApplication();
            notifyIcon.ContextMenu.Items.Add(exitMenuItem);

            // Load the icon from project resources
            try
            {
                var iconUri = new Uri("pack://application:,,,/Assets/icon.ico", UriKind.RelativeOrAbsolute);
                notifyIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(iconUri).Stream);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load tray icon from resources.");
                notifyIcon.Icon = System.Drawing.SystemIcons.Application; // Fallback
            }
        }

        private void ShowMainWindow()
        {
            if (mainWindow == null)
            {
                mainWindow = new MainWindow();
            }

            if (mainWindow.IsVisible)
            {
                if (mainWindow.WindowState == WindowState.Minimized)
                {
                    mainWindow.WindowState = WindowState.Normal;
                }
                mainWindow.Activate();
            }
            else
            {
                mainWindow.Show();
            }
        }

        private void ExitApplication()
        {
            Log.Information("--- Application Exiting ---");
            Log.CloseAndFlush();
            notifyIcon?.Dispose();
            Current.Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("--- Application Exiting ---");
            Log.CloseAndFlush();
            notifyIcon?.Dispose();
            base.OnExit(e);
        }
    }
}