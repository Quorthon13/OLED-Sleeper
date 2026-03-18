using OLED_Sleeper.Core.Interfaces;
using OLED_Sleeper.Features.MonitorBehavior.Commands;
using OLED_Sleeper.Features.MonitorBlackout.Commands;
using OLED_Sleeper.Features.MonitorDimming.Commands;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="RestoreMonitorStateCommandHandler"/> class.
        /// </summary>
        /// <param name="mediator">The mediator for dispatching further commands.</param>
        public RestoreMonitorStateCommandHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Handles the command to restore the monitor's state.
        /// </summary>
        /// <param name="command">The command containing the monitor's hardware ID.</param>
        public Task HandleAsync(RestoreMonitorStateCommand command)
        {
            Log.Information("Restoring state for monitor {HardwareId}.", command.HardwareId);
            _mediator.SendAsync(new HideBlackoutOverlayCommand { HardwareId = command.HardwareId });
            _mediator.SendAsync(new ApplyUndimCommand { HardwareId = command.HardwareId });
            return Task.CompletedTask;
        }
    }
}
