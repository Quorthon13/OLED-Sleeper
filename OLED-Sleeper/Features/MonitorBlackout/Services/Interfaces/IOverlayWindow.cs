using System.Windows;

namespace OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for one blackout overlay window.
    /// </summary>
    public interface IOverlayWindow
    {
        /// <summary>
        /// Gets the handle of the operating system window behind this overlay. Zero means there is no
        /// such window: before <see cref="Show"/> runs, and again once the overlay has been closed.
        /// </summary>
        nint Handle { get; }

        /// <summary>
        /// Shows the overlay covering the given bounds, without taking focus from the active window.
        /// Must be called on the UI thread.
        /// </summary>
        /// <param name="bounds">The monitor bounds in physical screen coordinates, not device-independent pixels.</param>
        void Show(Rect bounds);

        /// <summary>
        /// Closes the overlay. Must be called on the UI thread.
        /// </summary>
        void Close();
    }
}
