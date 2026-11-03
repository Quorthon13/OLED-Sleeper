using OLED_Sleeper.Features.MonitorBehavior.Commands;
using OLED_Sleeper.Features.MonitorBlackout.Commands;
using OLED_Sleeper.Features.MonitorDimming.Commands;
using OLED_Sleeper.Messaging.Interfaces;
using Serilog;

namespace OLED_Sleeper.Features.MonitorBehavior.Handlers
{
    /// <summary>
    /// Handles the execution of the <see cref="RestoreMonitorStateCommand"/>.
    /// Restores a monitor to its default state by hiding overlays and undimming.
    /// </summary>
    public class RestoreMonitorStateCommandHandler : ICommandHandler<RestoreMonitorStateCommand>
    {
        private readonly IMediator _mediator;

        public RestoreMonitorStateCommandHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Handles the command to restore the monitor's state.
        /// </summary>
        /// <param name="command">The command containing the monitor's hardware ID.</param>
        public async Task HandleAsync(RestoreMonitorStateCommand command)
        {
            Log.Information("Restoring state for monitor {HardwareId}.", command.HardwareId);
            var hideTask = _mediator.SendAsync(new HideBlackoutOverlayCommand { HardwareId = command.HardwareId });
            var undimTask = _mediator.SendAsync(new ApplyUndimCommand { HardwareId = command.HardwareId });
            try
            {
                await Task.WhenAll(hideTask, undimTask);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not fully restore monitor {HardwareId}.", command.HardwareId);
            }
        }
    }
}
