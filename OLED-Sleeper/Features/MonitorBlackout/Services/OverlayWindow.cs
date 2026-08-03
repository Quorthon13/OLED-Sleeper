using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;
using OLED_Sleeper.Native;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace OLED_Sleeper.Features.MonitorBlackout.Services
{
    /// <summary>
    /// A blackout overlay backed by a borderless, topmost WPF window.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class OverlayWindow : IOverlayWindow
    {
        /// <summary>
        /// The black, borderless, topmost window shown over the monitor. Its startup location is manual
        /// so that <see cref="Show"/> can place it in physical pixels.
        /// </summary>
        private readonly Window _window = new()
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

        /// <inheritdoc />
        public nint Handle => new WindowInteropHelper(_window).Handle;

        /// <inheritdoc />
        public void Show(Rect bounds)
        {
            _window.Show();

            nint hwnd = Handle;
            if (hwnd == nint.Zero) return;

            ApplyNoActivateStyle(hwnd);
            PositionToMonitor(hwnd, bounds);
        }

        /// <inheritdoc />
        public void Close() => _window.Close();

        #region Private Helpers

        /// <summary>
        /// Positions the overlay to exactly cover the monitor using physical screen coordinates.
        /// </summary>
        /// <param name="hwnd">The window handle.</param>
        /// <param name="bounds">The monitor bounds in physical screen coordinates.</param>
        private static void PositionToMonitor(nint hwnd, Rect bounds)
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
