using OLED_Sleeper.Commands.Monitor.Dimming;
using OLED_Sleeper.Services.Monitor.Dimming.Interfaces;
using Serilog;

namespace OLED_Sleeper.Handlers.Monitor.Dim
{
    /// <summary>
    /// Handles the execution of the <see cref="ApplyUndimCommand"/>.
    /// This class contains the business logic for restoring a monitor's brightness to its original value (undimming).
    /// </summary>
    public class ApplyUndimCommandHandler : ICommandHandler<ApplyUndimCommand>
    {
        private readonly IMonitorDimmingService _monitorDimmingService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplyUndimCommandHandler"/> class.
        /// </summary>
        /// <param name="monitorDimmingService">The service responsible for controlling monitor brightness.</param>
        public ApplyUndimCommandHandler(
            IMonitorDimmingService monitorDimmingService)
        {
            _monitorDimmingService = monitorDimmingService;
        }

        /// <summary>
        /// Executes the undimming logic asynchronously based on the command's data.
        /// Exceptions are caught and logged to avoid silent failures.
        /// </summary>
        /// <param name="command">The command containing the details of the monitor to undim.</param>
        public async Task HandleAsync(ApplyUndimCommand command)
        {
            try
            {
                Log.Information("Executing UndimMonitorCommand for monitor {HardwareId}.", command.HardwareId);
                await _monitorDimmingService.UndimMonitorAsync(command.HardwareId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to undim monitor {HardwareId}.", command.HardwareId);
            }
        }
    }
}