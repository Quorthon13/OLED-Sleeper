using OLED_Sleeper.Models;
using OLED_Sleeper.Core.Interfaces;

namespace OLED_Sleeper.Commands.Monitor.Behavior
{
    /// <summary>
    /// Command to apply the active behavior to a specific monitor.
    /// This is a data-transfer object that carries the necessary information for the handler to perform the action.
    /// </summary>
    public class ApplyMonitorActiveBehaviorCommand : ICommand
    {
        /// <summary>
        /// The event arguments containing monitor state and context for the activation event.
        /// </summary>cfx
        public MonitorIdleStateEventArgs EventArgs { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplyMonitorActiveBehaviorCommand"/> class.
        /// </summary>
        /// <param name="eventArgs">The event arguments for the monitor activation event.</param>
        public ApplyMonitorActiveBehaviorCommand(MonitorIdleStateEventArgs eventArgs)
        {
            EventArgs = eventArgs;
        }
    }
}