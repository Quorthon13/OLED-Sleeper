using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;
using OLED_Sleeper.Native;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace OLED_Sleeper.Features.MonitorBlackout.Services
{
    /// <summary>
    /// A blackout overlay backed by a borderless, topmost WPF window.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class OverlayWindow : IOverlayWindow
    {
        /// <summary>
        /// How often the overlay returns itself to the top of the topmost band while it is shown.
        /// </summary>
        private static readonly TimeSpan TopmostReassertInterval = TimeSpan.FromSeconds(1);

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

        /// <summary>
        /// Runs on the UI thread between <see cref="Show"/> and <see cref="Close"/>. Windows orders
        /// topmost windows among themselves, so the taskbar and other topmost windows sit above the
        /// overlay from the moment they are shown or activated until it re-asserts its z-order.
        /// </summary>
        private readonly DispatcherTimer _topmostTimer;

        public OverlayWindow()
        {
            _topmostTimer = new DispatcherTimer { Interval = TopmostReassertInterval };
            _topmostTimer.Tick += (_, _) => ReassertTopmost();
        }

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
            _topmostTimer.Start();
        }

        /// <inheritdoc />
        public void Close()
        {
            // A running DispatcherTimer is held by the dispatcher, which keeps this overlay alive.
            _topmostTimer.Stop();
            _window.Close();
        }

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

        /// <summary>
        /// Returns the overlay to the top of the topmost band, leaving its position, size and the
        /// active window untouched. Does nothing once the overlay has been closed.
        /// </summary>
        private void ReassertTopmost()
        {
            nint hwnd = Handle;
            if (hwnd == nint.Zero) return;

            NativeMethods.SetWindowPos(
                hwnd,
                NativeMethods.HWND_TOPMOST,
                0, 0, 0, 0,
                NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE);
        }

        #endregion Private Helpers
    }
}
