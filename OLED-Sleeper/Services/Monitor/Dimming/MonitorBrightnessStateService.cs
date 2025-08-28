using OLED_Sleeper.Services.Monitor.Dimming.Interfaces;
using Serilog;
using System.IO;
using System.Text.Json;

namespace OLED_Sleeper.Services.Monitor.Dimming
{
    /// <summary>
    /// Service for loading and saving the brightness state of monitors to disk.
    /// </summary>
    public class MonitorBrightnessStateService : IMonitorBrightnessStateService
    {
        private readonly string _stateFilePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorBrightnessStateService"/> class.
        /// Sets up the file path for storing brightness state in the user's AppData directory.
        /// </summary>
        public MonitorBrightnessStateService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var settingsDir = Path.Combine(appDataPath, "OLED-Sleeper");
            Directory.CreateDirectory(settingsDir);
            _stateFilePath = Path.Combine(settingsDir, "brightness_state.json");
        }

        #region IMonitorBrightnessStateService Implementation

        /// <summary>
        /// Loads the brightness state for all monitors from persistent storage.
        /// </summary>
        /// <returns>A dictionary mapping monitor hardware IDs to their brightness values.</returns>
        public Dictionary<string, uint> LoadState()
        {
            if (!File.Exists(_stateFilePath))
            {
                return new Dictionary<string, uint>();
            }

            try
            {
                var json = File.ReadAllText(_stateFilePath);
                var state = JsonSerializer.Deserialize<Dictionary<string, uint>>(json);
                return state ?? new Dictionary<string, uint>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load brightness state from {FilePath}.", _stateFilePath);
                return new Dictionary<string, uint>();
            }
        }

        /// <summary>
        /// Saves the brightness state for all monitors to persistent storage.
        /// </summary>
        /// <param name="state">A dictionary mapping monitor hardware IDs to their brightness values.</param>
        public void SaveState(Dictionary<string, uint> state)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(state, options);
                File.WriteAllText(_stateFilePath, json);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save brightness state to {FilePath}.", _stateFilePath);
            }
        }

        #endregion IMonitorBrightnessStateService Implementation
    }
}