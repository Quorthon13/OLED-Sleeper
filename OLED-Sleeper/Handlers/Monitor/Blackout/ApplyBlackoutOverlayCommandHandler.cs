using OLED_Sleeper.Commands.Monitor.Blackout;
using OLED_Sleeper.Services.Monitor.Blackout.Interfaces;
using OLED_Sleeper.Services.Monitor.Dimming.Interfaces;
using Serilog;

namespace OLED_Sleeper.Handlers.Monitor.Blackout
{
    /// <summary>
    /// Handles the execution of the <see cref="ApplyBlackoutOverlayCommand"/>.
    /// This class contains the business logic for applying the blackout effect to a monitor,
    /// which includes showing a software overlay and setting the hardware brightness to zero if supported.
    /// </summary>
    public class ApplyBlackoutOverlayCommandHandler : ICommandHandler<ApplyBlackoutOverlayCommand>
    {
        private readonly IMonitorBlackoutService _monitorBlackoutService;
        private readonly IMonitorDimmingService _monitorDimmingService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplyBlackoutOverlayCommandHandler"/> class.
        /// </summary>
        /// <param name="monitorBlackoutService">The service responsible for showing/hiding blackout overlays.</param>
        /// <param name="monitorDimmingService">The service responsible for controlling monitor brightness.</param>
        public ApplyBlackoutOverlayCommandHandler(
            IMonitorBlackoutService monitorBlackoutService,
            IMonitorDimmingService monitorDimmingService)
        {
            _monitorBlackoutService = monitorBlackoutService;
            _monitorDimmingService = monitorDimmingService;
        }

        /// <summary>
        /// Executes the blackout logic asynchronously based on the command's data.
        /// It shows a blackout overlay and, if the monitor supports DDC/CI,
        /// it simultaneously dims the monitor's brightness to 0.
        /// Exceptions are caught and logged to avoid silent failures.
        /// </summary>
        /// <param name="command">The command containing the details of the monitor to black out.</param>
        public async Task Handle(ApplyBlackoutOverlayCommand command)
        {
            try
            {
                Log.Information("Executing ApplyBlackoutCommand for monitor {HardwareId}.", command.HardwareId);

                // Task 1: Show the software blackout overlay.
                // We start this task but don't await it immediately.
                var showOverlayTask = _monitorBlackoutService.ShowBlackoutOverlayAsync(command.HardwareId, command.Bounds);

                // Task 2: If supported, also set the hardware brightness to 0 via DDC/CI.
                if (command.IsDdcCiSupported)
                {
                    Log.Information("Monitor {HardwareId} supports DDC/CI. Setting brightness to 0 for blackout.", command.HardwareId);
                    var dimTask = _monitorDimmingService.DimMonitorAsync(command.HardwareId, 0);

                    // Await both the overlay and dimming tasks to complete concurrently.
                    await Task.WhenAll(showOverlayTask, dimTask);
                }
                else
                {
                    // If DDC/CI is not supported, just wait for the overlay task to complete.
                    await showOverlayTask;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to apply blackout for monitor {HardwareId}.", command.HardwareId);
            }
        }
    }
}