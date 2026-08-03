using OLED_Sleeper.Features.UserSettings.Models;

namespace OLED_Sleeper.Features.UserSettings.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for loading and saving user settings to persistent storage.
    /// </summary>
    public interface IMonitorSettingsFileService
    {
        /// <summary>
        /// Event raised after settings have been saved. It carries the settings the caller supplied rather than
        /// the merged list that was written.
        /// </summary>
        event Action<List<MonitorSettings>>? SettingsChanged;

        /// <summary>
        /// Loads all monitor settings from persistent storage.
        /// </summary>
        /// <returns>A list of <see cref="MonitorSettings"/> objects, empty when no settings could be read.</returns>
        List<MonitorSettings> LoadSettings();

        /// <summary>
        /// Saves the provided monitor settings to persistent storage. Stored settings for monitors that are
        /// not in <paramref name="settings"/> are kept. <see cref="SettingsChanged"/> is raised only when the
        /// save succeeded.
        /// </summary>
        /// <param name="settings">The list of monitor settings to save.</param>
        void SaveSettings(List<MonitorSettings> settings);
    }
}