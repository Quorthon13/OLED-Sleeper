// File: Services/IMonitorSettingsFileService.cs
using OLED_Sleeper.Models;
using System.Collections.Generic;

namespace OLED_Sleeper.Services.Monitor.Interfaces
{
    public interface IMonitorSettingsFileService
    {
        List<MonitorSettings> LoadSettings();

        void SaveSettings(List<MonitorSettings> settings);
    }
}