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
            var settings = _fileStore.Read<List<MonitorSettings>>(SettingsFileName) ?? new List<MonitorSettings>();
            Log.Information("Loaded {Count} monitor settings.", settings.Count);
            return settings;
        }

        /// <inheritdoc />
        public void SaveSettings(List<MonitorSettings> settings)
        {
            var merged = MergeWithStoredSettings(settings);

            if (!_fileStore.TryWrite(SettingsFileName, merged)) return;

            Log.Information("Saved {Count} monitor settings.", merged.Count);
            SettingsChanged?.Invoke(settings);
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
