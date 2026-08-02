using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Features.UserSettings.Services.Interfaces;
using Serilog;
using System.IO;
using System.Text.Json;

namespace OLED_Sleeper.Features.UserSettings.Services
{
    /// <summary>
    /// Service for loading and saving user settings to a JSON file in the user's AppData directory.
    /// </summary>
    public class MonitorSettingsFileService : IMonitorSettingsFileService
    {
        // Fields
        private readonly string _settingsFilePath;

        /// <summary>
        /// Occurs when monitor settings are changed and saved.
        /// </summary>
        public event Action<List<MonitorSettings>>? SettingsChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorSettingsFileService"/> class.
        /// Ensures the settings directory exists and sets the file path.
        /// </summary>
        public MonitorSettingsFileService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var settingsDir = Path.Combine(appDataPath, "OLED-Sleeper");
            Directory.CreateDirectory(settingsDir);
            _settingsFilePath = Path.Combine(settingsDir, "settings.json");
        }

        /// <summary>
        /// Loads monitor settings from the settings file.
        /// Returns an empty list if the file does not exist or cannot be read.
        /// </summary>
        /// <returns>A list of <see cref="MonitorSettings"/> objects.</returns>
        public List<MonitorSettings> LoadSettings()
        {
            if (!File.Exists(_settingsFilePath))
            {
                Log.Information("Settings file not found. Returning default settings.");
                return new List<MonitorSettings>();
            }

            try
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<List<MonitorSettings>>(json);
                Log.Information("Successfully loaded {Count} monitor settings.", settings?.Count ?? 0);
                return settings ?? new List<MonitorSettings>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load settings from {FilePath}.", _settingsFilePath);
                return new List<MonitorSettings>();
            }
        }

        /// <summary>
        /// Saves the specified monitor settings to the settings file, keeping stored entries for monitors
        /// the caller did not supply.
        /// Invokes the <see cref="SettingsChanged"/> event with the supplied settings after a successful save.
        /// </summary>
        /// <param name="settings">The list of <see cref="MonitorSettings"/> to save.</param>
        public void SaveSettings(List<MonitorSettings> settings)
        {
            try
            {
                var merged = MergeWithStoredSettings(settings);
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(merged, options);
                File.WriteAllText(_settingsFilePath, json);
                Log.Information("Successfully saved {Count} monitor settings to {FilePath}.", merged.Count, _settingsFilePath);
                SettingsChanged?.Invoke(settings);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save settings to {FilePath}.", _settingsFilePath);
            }
        }

        /// <summary>
        /// Appends the stored settings whose hardware ID is absent from <paramref name="settings"/> to the
        /// supplied list. The caller only ever builds settings for connected monitors, so without this the
        /// write would drop the configuration of every disconnected one.
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