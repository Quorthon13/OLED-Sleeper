using OLED_Sleeper.Commands.Monitor.Blackout;
using OLED_Sleeper.Services.Monitor.Blackout.Interfaces;
using Serilog;

namespace OLED_Sleeper.Handlers.Monitor.Blackout
{
    /// <summary>
    /// Handles the execution of the <see cref="HideBlackoutOverlayCommand"/>.
    /// This class contains the business logic for hiding the blackout overlay on a monitor.
    /// </summary>
    public class HideBlackoutOverlayCommandHandler : ICommandHandler<HideBlackoutOverlayCommand>
    {
        private readonly IMonitorBlackoutService _monitorBlackoutService;

        /// <summary>
        /// Initializes a new instance of the <see cref="HideBlackoutOverlayCommandHandler"/> class.
        /// </summary>
        /// <param name="monitorBlackoutService">The service responsible for showing/hiding blackout overlays.</param>
        public HideBlackoutOverlayCommandHandler(
            IMonitorBlackoutService monitorBlackoutService)
        {
            _monitorBlackoutService = monitorBlackoutService;
        }

        /// <summary>
        /// Executes the logic to hide the blackout overlay asynchronously based on the command's data.
        /// Exceptions are caught and logged to avoid silent failures.
        /// </summary>
        /// <param name="command">The command containing the details of the monitor whose overlay should be hidden.</param>
        public async Task HandleAsync(HideBlackoutOverlayCommand command)
        {
            try
            {
                Log.Information("Executing HideBlackoutOverlayCommand for monitor {HardwareId}.", command.HardwareId);
                await _monitorBlackoutService.HideBlackoutOverlayAsync(command.HardwareId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to hide blackout overlay for monitor {HardwareId}.", command.HardwareId);
            }
        }
    }
}
