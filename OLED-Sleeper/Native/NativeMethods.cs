// File: NativeMethods.cs
using System;
using System.Runtime.InteropServices;

namespace OLED_Sleeper.Native
{
    /// <summary>
    /// Contains P/Invoke (Platform Invocation Services) definitions for calling native Windows API functions.
    /// This class is internal and static, serving as a single repository for all native code interactions.
    /// </summary>
    internal static class NativeMethods
    {
        #region User Input and Window Management

        /// <summary>
        /// A structure that contains information about the last user input event.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct LASTINPUTINFO
        {
            /// <summary>
            /// The size of the structure, in bytes.
            /// </summary>
            public uint cbSize;

            /// <summary>
            /// The tick count when the last input event was received.
            /// </summary>
            public uint dwTime;
        }

        /// <summary>
        /// Retrieves the tick count of the last user input event (mouse or keyboard).
        /// </summary>
        /// <param name="plii">A reference to a LASTINPUTINFO structure that will receive the information.</param>
        /// <returns>True if the function succeeds, false otherwise.</returns>
        /// <see href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getlastinputinfo"/>
        [DllImport("user32.dll")]
        public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        /// <summary>
        /// Retrieves a handle to the foreground window (the window with which the user is currently working).
        /// </summary>
        /// <returns>A handle to the foreground window.</returns>
        /// <see href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getforegroundwindow"/>
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Retrieves the dimensions of the bounding rectangle of the specified window.
        /// The dimensions are given in screen coordinates that are relative to the upper-left corner of the screen.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <param name="lpRect">A pointer to a RECT structure that receives the screen coordinates of the window.</param>
        /// <returns>True if the function succeeds, false otherwise.</returns>
        /// <see href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowrect"/>
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

        /// <summary>
        /// Retrieves a handle to the display monitor that has the largest area of intersection with a specified window.
        /// </summary>
        /// <param name="hwnd">A handle to the window of interest.</param>
        /// <param name="dwFlags">Determines the function's return value if the window does not intersect any display monitor.</param>
        /// <returns>A handle to the display monitor.</returns>
        /// <see href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-monitorfromwindow"/>
        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        /// <summary>
        /// Determines the function's return value if the window does not intersect any display monitor.
        /// If the window is nearest to a monitor, the function returns a handle to that monitor.
        /// </summary>
        public const uint MONITOR_DEFAULTTONEAREST = 2;

        #endregion User Input and Window Management

        #region Monitor and Display Configuration

        /// <summary>
        /// Represents a rectangular area with left, top, right, and bottom coordinates.
        /// Used by various Windows API functions.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        { public int left, top, right, bottom; }

        /// <summary>
        /// A callback function that is called by EnumDisplayMonitors for each monitor found.
        /// </summary>
        /// <returns>Return true to continue enumeration, false to stop.</returns>
        public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

        /// <summary>
        /// A structure that contains information about a display monitor.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MonitorInfoEx
        {
            public int cbSize;
            public Rect rcMonitor; // The display monitor rectangle, expressed in virtual-screen coordinates.
            public Rect rcWork;    // The work area rectangle of the display monitor.
            public uint dwFlags;   // A set of flags that represent attributes of the display monitor.

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice; // A string that specifies the device name of the monitor.
        }

        /// <summary>
        /// A structure that contains information about a display device.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct DISPLAY_DEVICE
        {
            public int cb;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;

            public uint StateFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        /// <summary>
        /// Specifies the type of DPI being queried.
        /// </summary>
        public enum MonitorDpiType
        { MDT_EFFECTIVE_DPI = 0, MDT_DEFAULT = MDT_EFFECTIVE_DPI }

        /// <summary>
        /// Enumerates display monitors (including invisible pseudo-monitors associated with mirroring drivers)
        /// that intersect a region formed by the intersection of a specified clipping rectangle and the visible region of a device context.
        /// </summary>
        /// <see href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumdisplaymonitors"/>
        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        /// <summary>
        /// Retrieves information about a display monitor.
        /// </summary>
        /// <see href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getmonitorinfoa"/>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

        /// <summary>
        /// Obtains information about the display devices in the current session.
        /// </summary>
        /// <see href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumdisplaydevicesa"/>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        /// <summary>
        /// Queries the dots per inch (dpi) of a display.
        /// </summary>
        /// <see href="https://learn.microsoft.com/en-us/windows/win32/api/shellscalingapi/nf-shellscalingapi-getdpiformonitor"/>
        [DllImport("Shcore.dll")]
        public static extern int GetDpiForMonitor(IntPtr hmonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

        #endregion Monitor and Display Configuration
    }
}