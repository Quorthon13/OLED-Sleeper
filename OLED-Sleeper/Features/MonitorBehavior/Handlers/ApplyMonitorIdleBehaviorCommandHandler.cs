using OLED_Sleeper.Core.Interfaces;
using OLED_Sleeper.Features.MonitorBehavior.Commands;
using OLED_Sleeper.Features.MonitorBehavior.Models;
using OLED_Sleeper.Features.MonitorBlackout.Commands;
using OLED_Sleeper.Features.MonitorDimming.Commands;
using Serilog;
using System.Threading.Tasks;

namespace OLED_Sleeper.Features.MonitorBehavior.Handlers
{
    /// <summary>
    /// Handles the <see cref="ApplyMonitorIdleBehaviorCommand"/> by dispatching the appropriate dim or blackout command.
    /// </summary>
    public class ApplyMonitorIdleBehaviorCommandHandler : ICommandHandler<ApplyMonitorIdleBehaviorCommand>
    {
        private readonly IMediator _mediator;

        public ApplyMonitorIdleBehaviorCommandHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Handles the <see cref="ApplyMonitorIdleBehaviorCommand"/> by dispatching the appropriate dim or blackout command for the monitor.
        /// </summary>
        /// <param name="command">The command containing the monitor idle event arguments.</param>
        public async Task HandleAsync(ApplyMonitorIdleBehaviorCommand command)
        {
            var e = command.EventArgs;
            switch (e.Settings.Behavior)
            {
                case MonitorBehaviorType.Blackout:
                    await _mediator.SendAsync(new ApplyBlackoutOverlayCommand { HardwareId = e.HardwareId });
                    break;
                case MonitorBehaviorType.Dim:
                    await _mediator.SendAsync(new ApplyDimCommand { HardwareId = e.HardwareId, DimLevel = (int)e.Settings.DimLevel });
                    break;
                default:
                    Log.Information("No idle behavior to apply for monitor {HardwareId}.", e.HardwareId);
                    break;
            }
        }
    }
}