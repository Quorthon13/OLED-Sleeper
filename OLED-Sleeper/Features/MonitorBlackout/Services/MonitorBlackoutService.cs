using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;
using OLED_Sleeper.Native;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace OLED_Sleeper.Features.MonitorBlackout.Services
{
    /// <summary>
    /// Manages blackout overlay windows for monitors asynchronously in a WPF application.
    /// Provides creation, display, and removal of overlay windows, and tracks overlay window handles.
    /// </summary>
    public class MonitorBlackoutService : IMonitorBlackoutService
    {
        /// <summary>
        /// Guards <see cref="_overlayWindows"/> and <see cref="_overlayHandles"/>. Overlays are created and
        /// closed on the dispatcher thread while <see cref="IsOverlayWindow"/> reads from the idle thread.
        /// Window creation, showing and closing all happen outside this lock.
        /// </summary>
        private readonly object _overlayLock = new();

        private readonly Dictionary<string, OverlayRegistration> _overlayWindows = new();
        private readonly HashSet<nint> _overlayHandles = new();

        /// <summary>
        /// An overlay window paired with the handle it was tracked under.
        /// </summary>
        /// <param name="Window">The overlay window.</param>
        /// <param name="Handle">The handle recorded when the overlay was shown, or zero if none was obtained.</param>
        private readonly record struct OverlayRegistration(Window Window, nint Handle);

        /// <summary>
        /// Asynchronously shows a blackout overlay on the specified monitor.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        /// <param name="bounds">The bounds of the monitor in screen coordinates.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ShowBlackoutOverlayAsync(string hardwareId, Rect bounds)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                lock (_overlayLock)
                {
                    if (_overlayWindows.ContainsKey(hardwareId)) return;
                }

                var overlay = CreateOverlayWindow();
                overlay.Show();

                nint hwnd = new WindowInteropHelper(overlay).Handle;
                if (hwnd != nint.Zero)
                {
                    ApplyNoActivateStyle(hwnd);
                    PositionOverlayToMonitor(hwnd, bounds);
                }

                lock (_overlayLock)
                {
                    if (hwnd != nint.Zero)
                    {
                        _overlayHandles.Add(hwnd);
                    }

                    _overlayWindows[hardwareId] = new OverlayRegistration(overlay, hwnd);
                }
            });
        }

        /// <summary>
        /// Asynchronously hides the blackout overlay for the specified monitor.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task HideBlackoutOverlayAsync(string hardwareId)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                OverlayRegistration registration;

                lock (_overlayLock)
                {
                    if (!_overlayWindows.Remove(hardwareId, out registration)) return;

                    // The stored handle is used rather than reading it back from the window, which
                    // returns zero once the window is closed and would leave the handle in the set.
                    if (registration.Handle != nint.Zero)
                    {
                        _overlayHandles.Remove(registration.Handle);
                    }
                }

                registration.Window.Close();
            });
        }

        /// <summary>
        /// Determines whether the specified window handle belongs to an overlay window.
        /// </summary>
        /// <param name="windowHandle">The window handle to check.</param>
        /// <returns>True if the handle is an overlay window; otherwise, false.</returns>
        public bool IsOverlayWindow(nint windowHandle)
        {
            if (windowHandle == nint.Zero) return false;

            lock (_overlayLock)
            {
                return _overlayHandles.Contains(windowHandle);
            }
        }

        #region Private Helpers

        /// <summary>
        /// Creates a new overlay window.
        /// </summary>
        /// <returns>A configured <see cref="Window"/> instance.</returns>
        private static Window CreateOverlayWindow() =>
            new()
            {
                Cursor = System.Windows.Input.Cursors.None,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                AllowsTransparency = true,
                Background = Brushes.Black,
                ShowInTaskbar = false,
                Topmost = true,
                WindowStartupLocation = WindowStartupLocation.Manual
            };

        /// <summary>
        /// Positions the overlay window to exactly cover the monitor using physical screen coordinates.
        /// </summary>
        /// <param name="hwnd">The window handle.</param>
        /// <param name="bounds">The monitor bounds in physical screen coordinates.</param>
        private static void PositionOverlayToMonitor(nint hwnd, Rect bounds)
        {
            NativeMethods.SetWindowPos(
                hwnd,
                NativeMethods.HWND_TOPMOST,
                (int)bounds.Left,
                (int)bounds.Top,
                (int)bounds.Width,
                (int)bounds.Height,
                NativeMethods.SWP_NOACTIVATE);
        }

        /// <summary>
        /// Applies the WS_EX_NOACTIVATE style to prevent the overlay from stealing focus.
        /// </summary>
        /// <param name="hwnd">The window handle.</param>
        private static void ApplyNoActivateStyle(nint hwnd)
        {
            nint extendedStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE,
                new nint(extendedStyle.ToInt64() | NativeMethods.WS_EX_NOACTIVATE));
        }

        #endregion Private Helpers
    }
}