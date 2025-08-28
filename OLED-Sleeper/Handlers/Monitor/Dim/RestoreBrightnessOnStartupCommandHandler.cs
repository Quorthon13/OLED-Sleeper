using OLED_Sleeper.Commands.Monitor.Dimming;
using OLED_Sleeper.Services.Monitor.Dimming.Interfaces;
using Serilog;

namespace OLED_Sleeper.Handlers.Monitor.Dim
{
    /// <summary>
    /// Handles the RestoreBrightnessOnStartupCommand to restore brightness for all monitors left dimmed from a previous session.
    /// </summary>
    public class RestoreBrightnessOnStartupCommandHandler : ICommandHandler<RestoreBrightnessOnStartupCommand>
    {
        private readonly IMonitorBrightnessStateService _monitorBrightnessStateService;
        private readonly IMonitorDimmingService _monitorDimmingService;

        public RestoreBrightnessOnStartupCommandHandler(
            IMonitorBrightnessStateService monitorBrightnessStateService,
            IMonitorDimmingService monitorDimmingService)
        {
            _monitorBrightnessStateService = monitorBrightnessStateService;
            _monitorDimmingService = monitorDimmingService;
        }

        public async Task Handle(RestoreBrightnessOnStartupCommand command)
        {
            Log.Information("Checking for monitors with unrestored brightness...");
            var state = _monitorBrightnessStateService.LoadState();
            if (state.Any())
            {
                Log.Warning("Found {Count} monitors that were left dimmed from a previous session. Attempting to restore.", state.Count);
                foreach (var entry in state)
                {
                    await _monitorDimmingService.RestoreBrightnessAsync(entry.Key, entry.Value);
                }
                _monitorBrightnessStateService.SaveState(new Dictionary<string, uint>());
            }
        }
    }
}