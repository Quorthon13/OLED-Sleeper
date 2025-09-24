using OLED_Sleeper.Core.Interfaces;
using Serilog;
using OLED_Sleeper.Features.MonitorBlackout.Commands;
using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;
using OLED_Sleeper.Features.MonitorDimming.Commands;
using OLED_Sleeper.Features.MonitorBehavior.Commands;
using OLED_Sleeper.Features.MonitorIdleDetection.Models;

namespace OLED_Sleeper.Features.MonitorBehavior.Handlers
{
    /// <summary>
    /// Handles the execution of the <see cref="ApplyMonitorActiveBehaviorCommand"/>.
    /// Applies the correct behavior when a monitor becomes active, including filtering out overlay-initiated activations and restoring the monitor's normal state.
    /// </summary>
    public class ApplyMonitorActiveBehaviorCommandHandler : ICommandHandler<ApplyMonitorActiveBehaviorCommand>
    {
        private readonly IMonitorBlackoutService _monitorBlackoutService;
        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplyMonitorActiveBehaviorCommandHandler"/> class.
        /// </summary>
        /// <param name="monitorBlackoutService">The service responsible for blackout overlays.</param>
        /// <param name="mediator">The mediator for dispatching further commands.</param>
        public ApplyMonitorActiveBehaviorCommandHandler(
            IMonitorBlackoutService monitorBlackoutService,
            IMediator mediator)
        {
            _monitorBlackoutService = monitorBlackoutService;
            _mediator = mediator;
        }

        /// <summary>
        /// Handles the monitor activation event, restoring monitor state if not triggered by overlay window.
        /// </summary>
        /// <param name="command">The command containing the event arguments for the monitor activation event.</param>
        public Task HandleAsync(ApplyMonitorActiveBehaviorCommand command)
        {
            var e = command.EventArgs;
            if (e.Reason == ActivityReason.ActiveWindow && _monitorBlackoutService.IsOverlayWindow(e.ForegroundWindowHandle))
            {
                Log.Debug("Monitor #{DisplayNumber} became active due to an overlay window. Flagging event as ignored.", e.DisplayNumber);
                e.IsIgnored = true;
                return Task.CompletedTask;
            }
            Log.Information("Monitor became active. Restoring state for monitor #{DisplayNumber}.", e.DisplayNumber);
            _mediator.SendAsync(new HideBlackoutOverlayCommand { HardwareId = e.HardwareId });
            _mediator.SendAsync(new ApplyUndimCommand { HardwareId = e.HardwareId });
            return Task.CompletedTask;
        }
    }
}