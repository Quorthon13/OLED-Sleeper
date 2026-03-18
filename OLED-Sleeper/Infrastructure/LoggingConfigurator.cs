using Serilog;

namespace OLED_Sleeper.Infrastructure
{
    /// <summary>
    /// Configures Serilog logging for the application and performs log file cleanup.
    /// </summary>
    public static class LoggingConfigurator
    {
        /// <summary>
        /// Sets up Serilog logging and deletes log files older than 7 days.
        /// </summary>
        public static void Configure()
        {
            var logDirectory = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OLED-Sleeper", "Logs");
            var logPath = System.IO.Path.Combine(logDirectory, "log-.txt");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("--- Application Starting ---");

            CleanupOldLogs(logDirectory, TimeSpan.FromDays(7));
        }

        /// <summary>
        /// Deletes log files in the specified directory that are older than the specified retention period.
        /// </summary>
        /// <param name="logDirectory">The directory containing log files.</param>
        /// <param name="retention">The maximum age of log files to keep.</param>
        private static void CleanupOldLogs(string logDirectory, TimeSpan retention)
        {
            try
            {
                if (!System.IO.Directory.Exists(logDirectory))
                    return;

                var now = DateTime.UtcNow;
                var files = System.IO.Directory.GetFiles(logDirectory, "log-*.txt");
                foreach (var file in files)
                {
                    var info = new System.IO.FileInfo(file);
                    if (now - info.CreationTimeUtc > retention)
                    {
                        try { info.Delete(); } catch { /* Ignore errors */ }
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}