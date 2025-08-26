using OLED_Sleeper.Models;
using System;
using System.Collections.Generic;

namespace OLED_Sleeper.Services.Monitor.Interfaces
{
    /// <summary>
    /// Defines the contract for loading and saving monitor settings to persistent storage.
    /// </summary>
    public interface IMonitorSettingsFileService
    {
        /// <summary>
        /// Event raised when monitor settings are changed and saved.
        /// </summary>
        event Action<List<MonitorSettings>>? SettingsChanged;

        /// <summary>
        /// Loads all monitor settings from persistent storage.
        /// </summary>
        /// <returns>A list of <see cref="MonitorSettings"/> objects.</returns>
        List<MonitorSettings> LoadSettings();

        /// <summary>
        /// Saves the provided monitor settings to persistent storage.
        /// </summary>
        /// <param name="settings">The list of monitor settings to save.</param>
        void SaveSettings(List<MonitorSettings> settings);
    }
}