// File: Services/OverlayService.cs
using OLED_Sleeper.Native;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace OLED_Sleeper.Services
{
    /// <summary>
    /// Provides overlay window management for monitor blackout overlays.
    /// Implements <see cref="IOverlayService"/>.
    /// </summary>
    public class OverlayService : IOverlayService
    {
        private readonly Dictionary<string, Window> _overlayWindows = new();
        private readonly HashSet<IntPtr> _overlayHandles = new();

        /// <summary>
        /// Displays a full-screen black overlay on the specified monitor.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        /// <param name="bounds">The virtual screen coordinates of the monitor.</param>
        public void ShowBlackoutOverlay(string hardwareId, Rect bounds)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_overlayWindows.ContainsKey(hardwareId)) return;

                var overlay = CreateOverlayWindow(bounds);
                overlay.Show();

                IntPtr hwnd = new WindowInteropHelper(overlay).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    ApplyNoActivateStyle(hwnd);
                    _overlayHandles.Add(hwnd);
                }

                ApplyDpiScaling(overlay, bounds);
                _overlayWindows[hardwareId] = overlay;
            });
        }

        /// <summary>
        /// Hides the overlay for the specified monitor.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        public void HideOverlay(string hardwareId)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_overlayWindows.TryGetValue(hardwareId, out var overlay))
                {
                    RemoveOverlayHandle(overlay);
                    overlay.Close();
                    _overlayWindows.Remove(hardwareId);
                }
            });
        }

        /// <summary>
        /// Checks if a given window handle belongs to an active overlay window.
        /// </summary>
        /// <param name="windowHandle">The window handle (HWND) to check.</param>
        /// <returns>True if the handle belongs to an overlay, false otherwise.</returns>
        public bool IsOverlayWindow(IntPtr windowHandle) =>
            windowHandle != IntPtr.Zero && _overlayHandles.Contains(windowHandle);

        #region Private Helpers

        /// <summary>
        /// Creates a new overlay window with the specified bounds.
        /// </summary>
        /// <param name="bounds">The virtual screen coordinates for the overlay.</param>
        /// <returns>A configured <see cref="Window"/> instance.</returns>
        private static Window CreateOverlayWindow(Rect bounds) =>
            new()
            {
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                AllowsTransparency = true,
                Background = Brushes.Black,
                ShowInTaskbar = false,
                Topmost = true,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = bounds.Left,
                Top = bounds.Top
            };

        /// <summary>
        /// Applies the WS_EX_NOACTIVATE style to prevent the overlay from stealing focus.
        /// </summary>
        /// <param name="hwnd">The window handle.</param>
        private static void ApplyNoActivateStyle(IntPtr hwnd)
        {
            IntPtr extendedStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE,
                new IntPtr(extendedStyle.ToInt64() | NativeMethods.WS_EX_NOACTIVATE));
        }

        /// <summary>
        /// Applies DPI scaling to the overlay window to ensure correct size and position.
        /// </summary>
        /// <param name="overlay">The overlay window.</param>
        /// <param name="bounds">The virtual screen coordinates.</param>
        private static void ApplyDpiScaling(Window overlay, Rect bounds)
        {
            var source = PresentationSource.FromVisual(overlay);
            if (source != null)
            {
                double dpiX = source.CompositionTarget.TransformToDevice.M11;
                double dpiY = source.CompositionTarget.TransformToDevice.M22;
                overlay.Left = bounds.Left / dpiX;
                overlay.Top = bounds.Top / dpiY;
                overlay.Width = bounds.Width / dpiX;
                overlay.Height = bounds.Height / dpiY;
            }
        }

        /// <summary>
        /// Removes the overlay window handle from tracking before closing.
        /// </summary>
        /// <param name="overlay">The overlay window.</param>
        private void RemoveOverlayHandle(Window overlay)
        {
            IntPtr hwnd = new WindowInteropHelper(overlay).Handle;
            if (hwnd != IntPtr.Zero)
            {
                _overlayHandles.Remove(hwnd);
            }
        }

        #endregion Private Helpers
    }
}