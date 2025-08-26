using OLED_Sleeper.Models;
using OLED_Sleeper.Services.Monitor.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace OLED_Sleeper.Services.Monitor
{
    // Refactored: New service to manage loading and saving of monitor settings.
    public class MonitorSettingsFileService : IMonitorSettingsFileService
    {
        private readonly string _settingsFilePath;
        public event Action<List<MonitorSettings>>? SettingsChanged;

        public MonitorSettingsFileService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var settingsDir = Path.Combine(appDataPath, "OLED-Sleeper");
            Directory.CreateDirectory(settingsDir);
            _settingsFilePath = Path.Combine(settingsDir, "settings.json");
        }

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

        public void SaveSettings(List<MonitorSettings> settings)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(_settingsFilePath, json);
                Log.Information("Successfully saved {Count} monitor settings to {FilePath}.", settings.Count, _settingsFilePath);
                SettingsChanged?.Invoke(settings);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save settings to {FilePath}.", _settingsFilePath);
            }
        }
    }
}