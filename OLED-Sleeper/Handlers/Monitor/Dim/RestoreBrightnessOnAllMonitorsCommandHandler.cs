using OLED_Sleeper.Commands.Monitor.Dimming;
using OLED_Sleeper.Services.Monitor.Dimming.Interfaces;
using Serilog;

namespace OLED_Sleeper.Handlers.Monitor.Dim
{
    /// <summary>
    /// Handles the RestoreBrightnessOnStartupCommand to restore brightness for all monitors left dimmed from a previous session.
    /// </summary>
    public class RestoreBrightnessOnAllMonitorsCommandHandler(
        IMonitorBrightnessStateService monitorBrightnessStateService,
        IMonitorDimmingService monitorDimmingService)
        : ICommandHandler<RestoreBrightnessOnAllMonitorsCommand>
    {
        public async Task HandleAsync(RestoreBrightnessOnAllMonitorsCommand command)
        {
            Log.Information("Checking for monitors with unrestored brightness...");
            var state = monitorBrightnessStateService.LoadState();
            if (state.Any())
            {
                Log.Warning("Found {Count} monitors that were left dimmed from a previous session. Attempting to restore.", state.Count);
                foreach (var entry in state)
                {
                    await monitorDimmingService.RestoreBrightnessAsync(entry.Key, entry.Value);
                }
                monitorBrightnessStateService.SaveState(new Dictionary<string, uint>());
            }
        }
    }
}