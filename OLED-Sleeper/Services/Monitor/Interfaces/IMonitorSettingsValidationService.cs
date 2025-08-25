// File: Services/IMonitorSettingsValidationService.cs
using OLED_Sleeper.ViewModels;
using System.Collections.Generic;

namespace OLED_Sleeper.Services.Monitor.Interfaces
{
    public interface IMonitorSettingsValidationService
    {
        bool ValidateAndNotify(IEnumerable<MonitorLayoutViewModel> monitors);
    }
}