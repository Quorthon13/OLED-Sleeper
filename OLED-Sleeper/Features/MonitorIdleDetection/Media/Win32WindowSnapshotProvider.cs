using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;
using OLED_Sleeper.Features.MonitorIdleDetection.Media.Interfaces;
using OLED_Sleeper.Features.MonitorIdleDetection.Media.Models;
using OLED_Sleeper.Native;
using Serilog;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace OLED_Sleeper.Features.MonitorIdleDetection.Media
{
    /// <summary>
    /// Walks the top-level windows with <c>EnumWindows</c>, which visits them in z-order, topmost first.
    /// </summary>
    public class Win32WindowSnapshotProvider : IWindowSnapshotProvider
    {
        /// <summary>
        /// How long a walk is reused, in seconds.
        /// </summary>
        private const double WalkIntervalSeconds = 1;

        /// <summary>
        /// Buffer size, in characters, for a window class name.
        /// </summary>
        private const int ClassNameBufferLength = 256;

        /// <summary>
        /// The window class of the frame that hosts a packaged (UWP) app. The frame's own process is not the app's.
        /// </summary>
        private const string ApplicationFrameClass = "ApplicationFrameWindow";

        /// <summary>
        /// The window class of the content window inside an application frame, which belongs to the app's process.
        /// </summary>
        private const string CoreWindowClass = "Windows.UI.Core.CoreWindow";

        /// <summary>
        /// Window classes that make up the shell, which are never a video window: the desktop and the taskbars.
        /// </summary>
        private static readonly HashSet<string> ShellWindowClasses = new(StringComparer.Ordinal)
        {
            "Progman",
            "WorkerW",
            "Shell_TrayWnd",
            "Shell_SecondaryTrayWnd"
        };

        private readonly IMonitorBlackoutService _blackoutService;
        private readonly uint _ownProcessId = (uint)Environment.ProcessId;

        private WindowSnapshotSet _snapshot = WindowSnapshotSet.Empty;
        private DateTime _lastWalkUtc = DateTime.MinValue;

        public Win32WindowSnapshotProvider(IMonitorBlackoutService blackoutService)
        {
            _blackoutService = blackoutService;
        }

        /// <inheritdoc />
        public WindowSnapshotSet GetSnapshot()
        {
            var now = DateTime.UtcNow;
            if ((now - _lastWalkUtc).TotalSeconds < WalkIntervalSeconds) return _snapshot;

            try
            {
                _snapshot = Walk();
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "The window walk failed.");
                _snapshot = WindowSnapshotSet.Empty;
            }

            _lastWalkUtc = now;
            return _snapshot;
        }

        /// <summary>
        /// Enumerates the top-level windows and keeps those that can be a video window.
        /// </summary>
        private WindowSnapshotSet Walk()
        {
            var windows = new List<WindowSnapshot>();
            var overlays = new List<Rect>();

            NativeMethods.EnumWindowsProc callback = (hwnd, _) =>
            {
                try
                {
                    Inspect(hwnd, windows, overlays);
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "A window could not be inspected.");
                }
                return true;
            };

            NativeMethods.EnumWindows(callback, nint.Zero);
            GC.KeepAlive(callback);
            return new WindowSnapshotSet(windows, overlays);
        }

        /// <summary>
        /// Classifies one window, adding it to the windows or to the overlays. Hidden, minimized, cloaked, shell, tool,
        /// click-through and own windows can never be a video window and are skipped.
        /// </summary>
        private void Inspect(nint hwnd, List<WindowSnapshot> windows, List<Rect> overlays)
        {
            if (!NativeMethods.IsWindowVisible(hwnd)) return;

            if (_blackoutService.IsOverlayWindow(hwnd))
            {
                if (TryGetBounds(hwnd, out var overlayBounds)) overlays.Add(overlayBounds);
                return;
            }

            NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);
            string className = GetClassName(hwnd);
            int exStyle = (int)NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);

            if (NativeMethods.IsIconic(hwnd) || IsCloaked(hwnd) || processId == _ownProcessId) return;
            if (ShellWindowClasses.Contains(className)) return;
            if ((exStyle & (NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_TRANSPARENT)) != 0) return;
            if (!TryGetBounds(hwnd, out var bounds) || bounds.Width <= 0 || bounds.Height <= 0) return;

            uint contentProcessId = className == ApplicationFrameClass ? FindCoreWindowProcess(hwnd) ?? processId : processId;
            int style = (int)NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_STYLE);
            bool hasCaption = (style & NativeMethods.WS_CAPTION) == NativeMethods.WS_CAPTION;
            windows.Add(new WindowSnapshot(ProcessFamily.Of(contentProcessId), bounds, hasCaption));
        }

        /// <summary>
        /// Finds the process of the app inside a packaged-app frame.
        /// </summary>
        /// <returns>The process identifier, or null when the frame has no content window of another process.</returns>
        private static uint? FindCoreWindowProcess(nint frame)
        {
            uint? found = null;

            NativeMethods.EnumWindowsProc callback = (child, _) =>
            {
                if (GetClassName(child) != CoreWindowClass) return true;

                NativeMethods.GetWindowThreadProcessId(child, out uint childProcessId);
                NativeMethods.GetWindowThreadProcessId(frame, out uint frameProcessId);
                if (childProcessId == frameProcessId) return true;

                found = childProcessId;
                return false;
            };

            NativeMethods.EnumChildWindows(frame, callback, nint.Zero);
            GC.KeepAlive(callback);
            return found;
        }

        /// <summary>
        /// Gets whether DWM hides a window, as it does for windows on another virtual desktop.
        /// </summary>
        private static bool IsCloaked(nint hwnd)
        {
            return NativeMethods.DwmGetWindowAttributeInt(hwnd, NativeMethods.DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0 && cloaked != 0;
        }

        /// <summary>
        /// Gets a window's extended frame bounds, falling back to its window rectangle.
        /// </summary>
        private static bool TryGetBounds(nint hwnd, out Rect bounds)
        {
            if (NativeMethods.DwmGetWindowAttribute(hwnd, NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS, out var native, Marshal.SizeOf<NativeMethods.Rect>()) != 0 &&
                !NativeMethods.GetWindowRect(hwnd, out native))
            {
                bounds = Rect.Empty;
                return false;
            }

            bounds = new Rect(native.left, native.top, native.right - native.left, native.bottom - native.top);
            return true;
        }

        /// <summary>
        /// Gets a window's class name.
        /// </summary>
        private static string GetClassName(nint hwnd)
        {
            var buffer = new StringBuilder(ClassNameBufferLength);
            return NativeMethods.GetClassName(hwnd, buffer, buffer.Capacity) == 0 ? string.Empty : buffer.ToString();
        }
    }
}
