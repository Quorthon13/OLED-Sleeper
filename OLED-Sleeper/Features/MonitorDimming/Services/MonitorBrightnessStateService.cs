using OLED_Sleeper.Features.MonitorDimming.Services.Interfaces;
using Serilog;
using System.IO;
using System.Text.Json;

namespace OLED_Sleeper.Features.MonitorDimming.Services
{
    /// <summary>
    /// Service for loading and saving the brightness state of monitors to disk.
    /// </summary>
    public class MonitorBrightnessStateService : IMonitorBrightnessStateService
    {
        private readonly string _stateFilePath;
        private readonly string _tempFilePath;
        private readonly string _backupFilePath;

        /// <summary>
        /// Sets up the file path for storing brightness state in the user's AppData directory.
        /// </summary>
        public MonitorBrightnessStateService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var settingsDir = Path.Combine(appDataPath, "OLED-Sleeper");
            Directory.CreateDirectory(settingsDir);
            _stateFilePath = Path.Combine(settingsDir, "brightness_state.json");
            _tempFilePath = _stateFilePath + ".tmp";
            _backupFilePath = _stateFilePath + ".bak";
        }

        #region IMonitorBrightnessStateService Implementation

        /// <summary>
        /// Loads the brightness state for all monitors from persistent storage.
        /// Falls back to the backup file when the state file is missing or unreadable.
        /// </summary>
        /// <returns>A dictionary mapping monitor hardware IDs to their brightness values.</returns>
        public Dictionary<string, uint> LoadState()
        {
            if (TryLoadFrom(_stateFilePath, out var state))
            {
                return state;
            }

            if (TryLoadFrom(_backupFilePath, out var backupState))
            {
                Log.Warning("Loaded brightness state from the backup file {FilePath}.", _backupFilePath);
                return backupState;
            }

            return new Dictionary<string, uint>();
        }

        /// <summary>
        /// Saves the brightness state for all monitors to persistent storage.
        /// Writes to a temporary file and replaces the state file with it, keeping the previous contents as a backup.
        /// </summary>
        /// <param name="state">A dictionary mapping monitor hardware IDs to their brightness values.</param>
        public void SaveState(Dictionary<string, uint> state)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(state, options);
                File.WriteAllText(_tempFilePath, json);

                if (File.Exists(_stateFilePath))
                {
                    File.Replace(_tempFilePath, _stateFilePath, _backupFilePath, ignoreMetadataErrors: true);
                }
                else
                {
                    File.Move(_tempFilePath, _stateFilePath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save brightness state to {FilePath}.", _stateFilePath);
            }
        }

        #endregion IMonitorBrightnessStateService Implementation

        #region Private Methods

        /// <summary>
        /// Reads and deserializes a brightness state file.
        /// </summary>
        /// <param name="filePath">The path of the file to read.</param>
        /// <param name="state">The deserialized state, or an empty dictionary when the file could not be read.</param>
        /// <returns><c>true</c> if the file was read and deserialized; otherwise, <c>false</c>.</returns>
        private static bool TryLoadFrom(string filePath, out Dictionary<string, uint> state)
        {
            state = new Dictionary<string, uint>();

            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                var json = File.ReadAllText(filePath);
                state = JsonSerializer.Deserialize<Dictionary<string, uint>>(json) ?? new Dictionary<string, uint>();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load brightness state from {FilePath}.", filePath);
                return false;
            }
        }

        #endregion Private Methods
    }
}