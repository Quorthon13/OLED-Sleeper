using OLED_Sleeper.Messaging.Interfaces;

namespace OLED_Sleeper.Features.MonitorBlackout.Commands
{
    /// <summary>
    /// Represents a command to apply the blackout behavior to a specific monitor.
    /// This is a data-transfer object that carries the necessary information
    /// for the handler to perform the action.
    /// </summary>
    public class ApplyBlackoutOverlayCommand : ICommand
    {
        /// <summary>
        /// The unique hardware identifier of the target monitor.
        /// </summary>
        public required string HardwareId { get; init; }

        /// <summary>
        /// Whether to also set the monitor's hardware brightness to zero. True when unset.
        /// </summary>
        public bool LowerBrightness { get; init; } = true;
    }
}