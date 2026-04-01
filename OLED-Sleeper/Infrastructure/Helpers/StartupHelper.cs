using Microsoft.Win32;

namespace OLED_Sleeper.Infrastructure.Helpers
{
    /// <summary>
    /// Provides static helper methods to manage the application's startup behavior via the Windows registry.
    /// </summary>
    public static class StartupHelper
    {
        private const string AppName = "OLED Sleeper";
        private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>
        /// Checks if the app is currently set to run at startup.
        /// </summary>
        public static bool IsRunAtStartupEnabled()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(AppName) != null;
        }

        /// <summary>
        /// Toggles the startup registry key on or off.
        /// </summary>
        public static void SetRunAtStartup(bool enable)
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key == null) return;

            if (enable)
            {
                string? exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\" -h");
                }
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
    }
}