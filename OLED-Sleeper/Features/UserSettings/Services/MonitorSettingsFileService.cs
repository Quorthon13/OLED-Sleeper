using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Features.UserSettings.Services.Interfaces;
using OLED_Sleeper.Storage.Interfaces;
using Serilog;

namespace OLED_Sleeper.Features.UserSettings.Services
{
    /// <summary>
    /// Keeps the monitor settings in <c>settings.json</c> under the application's data directory.
    /// </summary>
    public class MonitorSettingsFileService : IMonitorSettingsFileService
    {
        /// <summary>The name of the file the settings are kept in.</summary>
        private const string SettingsFileName = "settings.json";

        /// <summary>
        /// The schema version this build writes and is willing to read. Raise it whenever a change to
        /// <see cref="MonitorSettings"/> makes an older file's contents wrong rather than merely incomplete.
        /// </summary>
        private const int CurrentSchemaVersion = 1;

        private readonly IAppDataFileStore _fileStore;

        /// <inheritdoc />
        public event Action<List<MonitorSettings>>? SettingsChanged;

        public MonitorSettingsFileService(IAppDataFileStore fileStore)
        {
            _fileStore = fileStore;
        }

        /// <inheritdoc />
        public List<MonitorSettings> LoadSettings()
        {
            var document = _fileStore.Read<MonitorSettingsDocument>(SettingsFileName);

            if (document == null)
            {
                Log.Information("No monitor settings could be read. Starting from defaults.");
                return new List<MonitorSettings>();
            }

            if (document.SchemaVersion != CurrentSchemaVersion)
            {
                Log.Warning(
                    "Discarding monitor settings written under schema version {StoredVersion}; this build reads version {CurrentVersion}. Starting from defaults.",
                    document.SchemaVersion,
                    CurrentSchemaVersion);
                return new List<MonitorSettings>();
            }

            Log.Information("Loaded {Count} monitor settings.", document.Monitors.Count);
            return document.Monitors;
        }

        /// <inheritdoc />
        public void SaveSettings(List<MonitorSettings> settings)
        {
            try
            {
                var merged = MergeWithStoredSettings(settings);
                var document = new MonitorSettingsDocument { SchemaVersion = CurrentSchemaVersion, Monitors = merged };

                if (!_fileStore.TryWrite(SettingsFileName, document)) return;

                Log.Information("Saved {Count} monitor settings.", merged.Count);
                SettingsChanged?.Invoke(settings);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save monitor settings.");
            }
        }

        /// <summary>
        /// Appends the stored settings whose hardware ID is absent from <paramref name="settings"/> to the
        /// supplied list, so the configuration of monitors that are not connected is kept.
        /// </summary>
        /// <param name="settings">The settings supplied by the caller.</param>
        /// <returns>The supplied settings followed by the stored settings that were kept.</returns>
        private List<MonitorSettings> MergeWithStoredSettings(List<MonitorSettings> settings)
        {
            var suppliedIds = settings.Select(s => s.HardwareId).ToHashSet();
            var retained = LoadSettings().Where(stored => !suppliedIds.Contains(stored.HardwareId)).ToList();

            if (retained.Count > 0)
            {
                Log.Information("Kept stored settings for {Count} monitors that were not supplied.", retained.Count);
            }

            return settings.Concat(retained).ToList();
        }
    }
}
