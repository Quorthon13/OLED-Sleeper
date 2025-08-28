using System.Windows;
using OLED_Sleeper.Core.Interfaces;

namespace OLED_Sleeper.Commands.Monitor.Blackout
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

        /// <summary>
        /// The screen coordinates and dimensions (bounds) of the monitor
        /// where the blackout overlay should be displayed.
        /// </summary>
        public Rect Bounds { get; init; }

        /// <summary>
        /// Indicates whether the monitor supports DDC/CI for brightness control.
        /// If true, the handler will also attempt to set the brightness to zero.
        /// </summary>
        public bool IsDdcCiSupported { get; init; }
    }
}