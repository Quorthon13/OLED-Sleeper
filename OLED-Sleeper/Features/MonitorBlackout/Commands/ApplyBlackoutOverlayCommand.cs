using OLED_Sleeper.Core.Interfaces;

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
        public string? HardwareId { get; init; }
    }
}