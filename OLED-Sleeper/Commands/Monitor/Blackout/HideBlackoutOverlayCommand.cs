using OLED_Sleeper.Core.Interfaces;

namespace OLED_Sleeper.Commands.Monitor.Blackout
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
        public string? HardwareId { get; init; }
    }
}