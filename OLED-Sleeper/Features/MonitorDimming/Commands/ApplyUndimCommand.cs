using OLED_Sleeper.Core.Interfaces;

namespace OLED_Sleeper.Features.MonitorDimming.Commands
{
    /// <summary>
    /// Represents a command to restore a monitor's brightness to its original value (undim).
    /// This is a data-transfer object that carries the necessary information for the handler to perform the action.
    /// </summary>
    public class ApplyUndimCommand : ICommand
    {
        /// <summary>
        /// The unique hardware identifier of the target monitor.
        /// </summary>
        public string? HardwareId { get; init; }
    }
}