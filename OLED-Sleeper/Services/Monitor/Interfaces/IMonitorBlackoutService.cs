using System.Windows;

namespace OLED_Sleeper.Services.Monitor.Interfaces
{
    /// <summary>
    /// Defines the contract for blackout overlay management on monitors.
    /// </summary>
    public interface IMonitorBlackoutService
    {
        /// <summary>
        /// Shows a blackout overlay on the specified monitor.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        /// <param name="bounds">The bounds of the monitor in screen coordinates.</param>
        void ShowBlackoutOverlay(string hardwareId, Rect bounds);

        /// <summary>
        /// Hides the blackout overlay for the specified monitor.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        void HideOverlay(string hardwareId);

        /// <summary>
        /// Determines whether the specified window handle belongs to an overlay window.
        /// </summary>
        /// <param name="windowHandle">The window handle to check.</param>
        /// <returns>True if the handle is an overlay window; otherwise, false.</returns>
        bool IsOverlayWindow(nint windowHandle);
    }
}