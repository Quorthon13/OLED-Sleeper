using Serilog;
using System.IO;
using System.Text.Json;

namespace OLED_Sleeper.Services
{
    public class BrightnessStateService : IBrightnessStateService
    {
        private readonly string _stateFilePath;

        public BrightnessStateService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var settingsDir = Path.Combine(appDataPath, "OLED-Sleeper");
            Directory.CreateDirectory(settingsDir);
            _stateFilePath = Path.Combine(settingsDir, "brightness_state.json");
        }

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
    }
}