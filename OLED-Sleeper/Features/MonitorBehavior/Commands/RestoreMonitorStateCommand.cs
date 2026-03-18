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
        /// Gets or sets the unique hardware ID of the monitor to be restored.
        /// </summary>
        public string HardwareId { get; set; }
    }
}