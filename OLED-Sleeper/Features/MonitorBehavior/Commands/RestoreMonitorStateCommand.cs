using OLED_Sleeper.Core.Interfaces;

namespace OLED_Sleeper.Features.MonitorBehavior.Commands
{
    /// <summary>
    /// Represents a command to restore a monitor to its default state,
    /// which includes hiding any blackout overlay and restoring its original brightness (undimming).
    /// </summary>
    public class RestoreMonitorStateCommand : ICommand
    {
        /// <summary>
        /// The unique hardware identifier of the target monitor.
        /// </summary>
        public required string HardwareId { get; init; }
    }
}