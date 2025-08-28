using OLED_Sleeper.Core.Interfaces;

namespace OLED_Sleeper.Commands.Monitor.Dimming
{
    /// <summary>
    /// Represents a command to dim a specific monitor to a given brightness level.
    /// This is a data-transfer object that carries the necessary information for the handler to perform the action.
    /// </summary>
    public class ApplyDimCommand : ICommand
    {
        /// <summary>
        /// The unique hardware identifier of the target monitor.
        /// </summary>
        public string? HardwareId { get; init; }

        /// <summary>
        /// The brightness level to set (0-100).
        /// </summary>
        public int DimLevel { get; init; }
    }
}