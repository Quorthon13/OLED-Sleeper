using OLED_Sleeper.Commands.Monitor.Dimming;
using OLED_Sleeper.Services.Monitor.Dimming.Interfaces;
using Serilog;

namespace OLED_Sleeper.Handlers.Monitor.Dim
{
    /// <summary>
    /// Handles the execution of the <see cref="ApplyDimCommand"/>.
    /// This class contains the business logic for dimming a monitor to a specified brightness level.
    /// </summary>
    public class ApplyDimCommandHandler : ICommandHandler<ApplyDimCommand>
    {
        private readonly IMonitorDimmingService _monitorDimmingService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplyDimCommandHandler"/> class.
        /// </summary>
        /// <param name="monitorDimmingService">The service responsible for controlling monitor brightness.</param>
        public ApplyDimCommandHandler(
            IMonitorDimmingService monitorDimmingService)
        {
            _monitorDimmingService = monitorDimmingService;
        }

        /// <summary>
        /// Executes the dimming logic asynchronously based on the command's data.
        /// Exceptions are caught and logged to avoid silent failures.
        /// </summary>
        /// <param name="command">The command containing the details of the monitor to dim and the target brightness level.</param>
        public async Task Handle(ApplyDimCommand command)
        {
            try
            {
                Log.Information("Executing ApplyDimCommand for monitor {HardwareId} to level {DimLevel}.", command.HardwareId, command.DimLevel);
                await _monitorDimmingService.DimMonitorAsync(command.HardwareId, command.DimLevel);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to dim monitor {HardwareId} to level {DimLevel}.", command.HardwareId, command.DimLevel);
            }
        }
    }
}