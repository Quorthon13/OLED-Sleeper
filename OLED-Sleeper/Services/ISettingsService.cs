// File: Services/ISettingsService.cs
using OLED_Sleeper.Models;
using System.Collections.Generic;

namespace OLED_Sleeper.Services
{
    // Refactored: New interface for handling application settings persistence.
    public interface ISettingsService
    {
        List<MonitorSettings> LoadSettings();

        void SaveSettings(List<MonitorSettings> settings);
    }
}