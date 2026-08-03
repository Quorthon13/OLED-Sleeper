using OLED_Sleeper.Messaging.Interfaces;

namespace OLED_Sleeper.Features.MonitorBlackout.Commands
{
    /// <summary>
    /// Represents a command to hide the blackout overlay for a specific monitor.
    /// This is a data-transfer object that carries the necessary information
    /// for the handler to perform the action.
    /// </summary>
    public class HideBlackoutOverlayCommand : ICommand
    {
        /// <summary>
        /// The unique hardware identifier of the target monitor.
        /// </summary>
        public required string HardwareId { get; init; }
    }
}