using OLED_Sleeper.Core.Interfaces;
using OLED_Sleeper.Features.MonitorDimming.Commands;
using OLED_Sleeper.Features.MonitorDimming.Services.Interfaces;
using Serilog;

namespace OLED_Sleeper.Features.MonitorDimming.Handlers
{
    /// <summary>
    /// Handles the RestoreBrightnessOnAllMonitorsCommand to restore brightness for all monitors left dimmed.
    /// </summary>
    public class RestoreBrightnessOnAllMonitorsCommandHandler(IMonitorDimmingService monitorDimmingService)
        : ICommandHandler<RestoreBrightnessOnAllMonitorsCommand>
    {
        public async Task HandleAsync(RestoreBrightnessOnAllMonitorsCommand command)
        {
            Log.Information("Checking for monitors with unrestored brightness...");

            // The dimming service owns the dimmed-monitor list and writes the state file. Going through it here
            // keeps both in step; clearing the file directly leaves its list populated and the next save rewrites it.
            var dimmedMonitors = monitorDimmingService.GetDimmedMonitors();
            if (dimmedMonitors.Count == 0) return;

            Log.Warning("Found {Count} monitors that were left dimmed. Attempting to restore.", dimmedMonitors.Count);
            foreach (var hardwareId in dimmedMonitors.Keys)
            {
                await monitorDimmingService.UndimMonitorAsync(hardwareId);
            }
        }
    }
}
