// File: Services/IOverlayService.cs
using System.Windows;

namespace OLED_Sleeper.Services
{
    public interface IOverlayService
    {
        /// <summary>
        /// Displays a full-screen black overlay on the specified monitor.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        /// <param name="bounds">The virtual screen coordinates of the monitor.</param>
        void ShowBlackoutOverlay(string hardwareId, Rect bounds);

        /// <summary>
        /// Hides the overlay for the specified monitor.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        void HideOverlay(string hardwareId);

        /// <summary>
        /// Checks if a given window handle belongs to an active overlay window.
        /// </summary>
        /// <param name="windowHandle">The window handle (HWND) to check.</param>
        /// <returns>True if the handle belongs to an overlay, false otherwise.</returns>
        bool IsOverlayWindow(IntPtr windowHandle);
    }
}