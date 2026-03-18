using OLED_Sleeper.Core.Interfaces;
using OLED_Sleeper.Features.MonitorIdleDetection.Models;

namespace OLED_Sleeper.Features.MonitorBehavior.Commands
{
    /// <summary>
    /// Command to apply the idle behavior to a specific monitor.
    /// This is a data-transfer object that carries the necessary information for the handler to perform the action.
    /// </summary>
    /// <param name="eventArgs">The event arguments for the monitor idle event, containing monitor state and context.</param>
    public class ApplyMonitorIdleBehaviorCommand(MonitorIdleStateEventArgs eventArgs) : ICommand
    {
        /// <summary>
        /// Gets the event arguments containing monitor state and context for the idle event.
        /// </summary>
        public MonitorIdleStateEventArgs EventArgs { get; init; } = eventArgs;
    }
}