using OLED_Sleeper.Core.Interfaces;
using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;
using System.Windows;

namespace OLED_Sleeper.Features.MonitorBlackout.Services
{
    /// <summary>
    /// Manages blackout overlay windows for monitors asynchronously in a WPF application.
    /// Provides creation, display, and removal of overlay windows, and tracks overlay window handles.
    /// </summary>
    public class MonitorBlackoutService : IMonitorBlackoutService
    {
        private readonly IDispatcher _dispatcher;
        private readonly IOverlayWindowFactory _overlayWindowFactory;

        /// <summary>
        /// Guards <see cref="_overlayWindows"/> and <see cref="_overlayHandles"/>. Overlays are created and
        /// closed on the dispatcher thread while <see cref="IsOverlayWindow"/> reads from the idle thread.
        /// Window creation, showing and closing all happen outside this lock.
        /// </summary>
        private readonly object _overlayLock = new();

        /// <summary>
        /// The overlay currently shown on each monitor, keyed by hardware ID. A monitor with no overlay
        /// has no entry.
        /// </summary>
        private readonly Dictionary<string, OverlayRegistration> _overlayWindows = new();

        /// <summary>
        /// The handles of every overlay currently shown, which is what <see cref="IsOverlayWindow"/>
        /// answers from. Overlays that never got a handle are absent.
        /// </summary>
        private readonly HashSet<nint> _overlayHandles = new();

        public MonitorBlackoutService(IDispatcher dispatcher, IOverlayWindowFactory overlayWindowFactory)
        {
            _dispatcher = dispatcher;
            _overlayWindowFactory = overlayWindowFactory;
        }

        /// <summary>
        /// An overlay window paired with the handle it was tracked under.
        /// </summary>
        /// <param name="Window">The overlay window.</param>
        /// <param name="Handle">The handle recorded when the overlay was shown, or zero if none was obtained.</param>
        private readonly record struct OverlayRegistration(IOverlayWindow Window, nint Handle);

        /// <inheritdoc />
        public async Task ShowBlackoutOverlayAsync(string hardwareId, Rect bounds)
        {
            await _dispatcher.InvokeAsync(() =>
            {
                lock (_overlayLock)
                {
                    if (_overlayWindows.ContainsKey(hardwareId)) return;
                }

                var overlay = _overlayWindowFactory.Create();
                overlay.Show(bounds);

                nint hwnd = overlay.Handle;

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

        /// <inheritdoc />
        public async Task HideBlackoutOverlayAsync(string hardwareId)
        {
            await _dispatcher.InvokeAsync(() =>
            {
                OverlayRegistration registration;

                lock (_overlayLock)
                {
                    if (!_overlayWindows.Remove(hardwareId, out registration)) return;

                    // An overlay reports a zero handle once it is closed, so the handle recorded when it
                    // was shown is used here.
                    if (registration.Handle != nint.Zero)
                    {
                        _overlayHandles.Remove(registration.Handle);
                    }
                }

                registration.Window.Close();
            });
        }

        /// <inheritdoc />
        public bool IsOverlayWindow(nint windowHandle)
        {
            if (windowHandle == nint.Zero) return false;

            lock (_overlayLock)
            {
                return _overlayHandles.Contains(windowHandle);
            }
        }
    }
}
