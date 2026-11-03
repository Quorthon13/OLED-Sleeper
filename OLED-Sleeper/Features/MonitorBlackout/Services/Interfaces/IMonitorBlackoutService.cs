using System.Windows;

namespace OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for asynchronous blackout overlay management on monitors.
    /// </summary>
    public interface IMonitorBlackoutService
    {
        /// <summary>
        /// Asynchronously shows a blackout overlay on the specified monitor.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        /// <param name="bounds">The bounds of the monitor in screen coordinates.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ShowBlackoutOverlayAsync(string hardwareId, Rect bounds);

        /// <summary>
        /// Asynchronously hides the blackout overlay for the specified monitor.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task HideBlackoutOverlayAsync(string hardwareId);

        /// <summary>
        /// Determines whether the specified window handle belongs to an overlay window.
        /// </summary>
        /// <param name="windowHandle">The window handle to check.</param>
        /// <returns>True if the handle is an overlay window; otherwise, false.</returns>
        bool IsOverlayWindow(nint windowHandle);

        /// <summary>Returns whether a blackout overlay is currently displayed for the monitor.</summary>
        bool IsBlackoutShown(string hardwareId);

        /// <summary>Raised on the UI thread whenever an overlay is shown or closed.</summary>
        event Action<string>? BlackoutStateChanged;
    }
}
