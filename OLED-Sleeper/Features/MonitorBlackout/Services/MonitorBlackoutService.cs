using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;
using OLED_Sleeper.Native;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace OLED_Sleeper.Features.MonitorBlackout.Services
{
    /// <summary>
    /// Provides asynchronous overlay window management for monitor blackout overlays.
    /// Implements <see cref="IMonitorBlackoutService"/>.
    /// </summary>
    public class MonitorBlackoutService : IMonitorBlackoutService
    {
        private readonly Dictionary<string, Window> _overlayWindows = new();
        private readonly HashSet<nint> _overlayHandles = new();

        /// <inheritdoc cref="IMonitorBlackoutService.ShowBlackoutOverlayAsync"/>
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

                ApplyDpiScaling(overlay, bounds);
                _overlayWindows[hardwareId] = overlay;
            });
        }

        /// <inheritdoc/>
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

        /// <inheritdoc cref="IMonitorBlackoutService.IsOverlayWindow"/>
        public bool IsOverlayWindow(nint windowHandle) =>
            windowHandle != nint.Zero && _overlayHandles.Contains(windowHandle);

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
        private static void ApplyNoActivateStyle(nint hwnd)
        {
            nint extendedStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE,
                new nint(extendedStyle.ToInt64() | NativeMethods.WS_EX_NOACTIVATE));
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
            nint hwnd = new WindowInteropHelper(overlay).Handle;
            if (hwnd != nint.Zero)
            {
                _overlayHandles.Remove(hwnd);
            }
        }

        #endregion Private Helpers
    }
}