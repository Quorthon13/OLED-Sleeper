using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;
using OLED_Sleeper.Native;
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
        private readonly Dictionary<string, Window> _overlayWindows = new();
        private readonly HashSet<nint> _overlayHandles = new();

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
                if (_overlayWindows.ContainsKey(hardwareId)) return;

                var overlay = CreateOverlayWindow(bounds);
                overlay.Show();

                nint hwnd = new WindowInteropHelper(overlay).Handle;
                if (hwnd != nint.Zero)
                {
                    ApplyNoActivateStyle(hwnd);
                    _overlayHandles.Add(hwnd);
                }

                _overlayWindows[hardwareId] = overlay;
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
                if (_overlayWindows.TryGetValue(hardwareId, out var overlay))
                {
                    RemoveOverlayHandle(overlay);
                    overlay.Close();
                    _overlayWindows.Remove(hardwareId);
                }
            });
        }

        /// <summary>
        /// Determines whether the specified window handle belongs to an overlay window.
        /// </summary>
        /// <param name="windowHandle">The window handle to check.</param>
        /// <returns>True if the handle is an overlay window; otherwise, false.</returns>
        public bool IsOverlayWindow(nint windowHandle) =>
            windowHandle != nint.Zero && _overlayHandles.Contains(windowHandle);

        #region Private Helpers

        /// <summary>
        /// Creates a new overlay window positioned and sized for the target monitor.
        /// </summary>
        /// <param name="bounds">The monitor bounds in physical screen coordinates.</param>
        /// <returns>A configured <see cref="Window"/> instance.</returns>
        private static Window CreateOverlayWindow(Rect bounds)
        {
            var (scaleX, scaleY) = GetMonitorDpiScale(bounds);

            return new Window
            {
                Cursor = System.Windows.Input.Cursors.None,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                AllowsTransparency = true,
                Background = Brushes.Black,
                ShowInTaskbar = false,
                Topmost = true,
                WindowStartupLocation = WindowStartupLocation.Manual,

                Left = bounds.Left / scaleX,
                Top = bounds.Top / scaleY,
                Width = bounds.Width / scaleX,
                Height = bounds.Height / scaleY
            };
        }

        /// <summary>
        /// Retrieves the DPI scale factor for the monitor that contains the specified bounds.
        /// </summary>
        /// <param name="physicalBounds">The monitor bounds in physical screen coordinates.</param>
        /// <returns>The scale factors for the X and Y axes.</returns>
        private static (double scaleX, double scaleY) GetMonitorDpiScale(Rect physicalBounds)
        {
            NativeMethods.Rect rect = new NativeMethods.Rect
            {
                left = (int)Math.Floor(physicalBounds.Left),
                top = (int)Math.Floor(physicalBounds.Top),
                right = (int)Math.Ceiling(physicalBounds.Right),
                bottom = (int)Math.Ceiling(physicalBounds.Bottom)
            };

            IntPtr hMonitor = NativeMethods.MonitorFromRect(ref rect, NativeMethods.MONITOR_DEFAULTTONEAREST);
            if (hMonitor == IntPtr.Zero)
                return (1.0, 1.0);

            uint dpiX = 96, dpiY = 96;
            int hr = NativeMethods.GetDpiForMonitor(hMonitor, NativeMethods.MonitorDpiType.MDT_EFFECTIVE_DPI, out dpiX, out dpiY);

            return hr == 0
                ? (dpiX / 96.0, dpiY / 96.0)
                : (1.0, 1.0);
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

        /// <summary>
        /// Removes the overlay window handle from tracking before closing.
        /// </summary>
        /// <param name="overlay">The overlay window.</param>
        private void RemoveOverlayHandle(Window overlay)
        {
            nint hwnd = new WindowInteropHelper(overlay).Handle;
            if (hwnd != nint.Zero)
            {
                _overlayHandles.Remove(hwnd);
            }
        }

        #endregion Private Helpers
    }
}