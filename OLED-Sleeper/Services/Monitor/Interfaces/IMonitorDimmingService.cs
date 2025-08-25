// File: Services/IMonitorDimmingService.cs
using System.Collections.Generic;

namespace OLED_Sleeper.Services.Monitor.Interfaces
{
    public interface IMonitorDimmingService
    {
        void DimMonitor(string hardwareId, int dimLevel);

        void UndimMonitor(string hardwareId);

        void RestoreBrightness(string hardwareId, uint originalBrightness);

        Dictionary<string, uint> GetDimmedMonitors();
    }
}