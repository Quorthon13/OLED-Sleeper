// File: NativeMethods.cs
using System;
using System.Runtime.InteropServices;

namespace OLED_Sleeper.Native
{
    /// <summary>
    /// Provides P/Invoke (Platform Invocation Services) definitions for native Windows API functions.
    /// This static internal class centralizes all native code interactions for the application.
    /// </summary>
    internal static class NativeMethods
    {
        #region User Input and Window Management

        /// <summary>
        /// Specifies the index for retrieving or setting a window's extended styles.
        /// </summary>
        public const int GWL_EXSTYLE = -20;

        /// <summary>
        /// Specifies that a window should not be activated when shown.
        /// </summary>
        public const int WS_EX_NOACTIVATE = 0x08000000;

        /// <summary>
        /// Contains information about the last user input event.
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
        /// <param name="plii">A reference to a <see cref="LASTINPUTINFO"/> structure that receives the information.</param>
        /// <returns>True if the function succeeds; otherwise, false.</returns>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getlastinputinfo"/>
        [DllImport("user32.dll")]
        public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        /// <summary>
        /// Retrieves a handle to the foreground window (the window with which the user is currently working).
        /// </summary>
        /// <returns>A handle to the foreground window.</returns>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getforegroundwindow"/>
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Retrieves a handle to the display monitor that has the largest area of intersection with a specified window.
        /// </summary>
        /// <param name="hwnd">A handle to the window of interest.</param>
        /// <param name="dwFlags">Determines the function's return value if the window does not intersect any display monitor.</param>
        /// <returns>A handle to the display monitor.</returns>
        /// <remarks>Use <see cref="MONITOR_DEFAULTTONEAREST"/> for <c>dwFlags</c> to get the nearest monitor if none intersect.</remarks>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-monitorfromwindow"/>
        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        /// <summary>
        /// Determines the function's return value if the window does not intersect any display monitor.
        /// </summary>
        public const uint MONITOR_DEFAULTTONEAREST = 2;

        /// <summary>
        /// Retrieves the dimensions of the bounding rectangle of the specified window in screen coordinates.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <param name="lpRect">A pointer to a <see cref="Rect"/> structure that receives the screen coordinates of the window.</param>
        /// <returns>True if the function succeeds; otherwise, false.</returns>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowrect"/>
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

        /// <summary>
        /// Retrieves information about the specified window's extended styles.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <param name="nIndex">The zero-based offset to the value to be retrieved (use <see cref="GWL_EXSTYLE"/>).</param>
        /// <returns>The value of the requested offset.</returns>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowlongptra"/>
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        /// <summary>
        /// Changes an attribute of the specified window. Use to set extended window styles.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <param name="nIndex">The zero-based offset to the value to be set (use <see cref="GWL_EXSTYLE"/>).</param>
        /// <param name="dwNewLong">The replacement value.</param>
        /// <returns>The previous value of the specified offset.</returns>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowlongptra"/>
        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        #endregion User Input and Window Management

        #region Monitor and Display Configuration

        /// <summary>
        /// Represents a point in 2D space.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        /// <summary>
        /// Represents a rectangular area with left, top, right, and bottom coordinates.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int left, top, right, bottom;
        }

        /// <summary>
        /// Contains information about a display device.
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
        /// Contains information about a display monitor.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MonitorInfoEx
        {
            public int cbSize;
            public Rect rcMonitor;
            public Rect rcWork;
            public uint dwFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        /// <summary>
        /// Specifies the type of DPI being queried.
        /// </summary>
        public enum MonitorDpiType
        {
            /// <summary>Effective DPI that incorporates user settings and scaling.</summary>
            MDT_EFFECTIVE_DPI = 0,

            /// <summary>Default DPI type (same as <see cref="MDT_EFFECTIVE_DPI"/>).</summary>
            MDT_DEFAULT = MDT_EFFECTIVE_DPI
        }

        /// <summary>
        /// Delegate for monitor enumeration callback used by <see cref="EnumDisplayMonitors"/>.
        /// </summary>
        /// <param name="hMonitor">Handle to the display monitor.</param>
        /// <param name="hdcMonitor">Handle to a device context.</param>
        /// <param name="lprcMonitor">Pointer to a <see cref="Rect"/> structure with the display monitor rectangle.</param>
        /// <param name="dwData">Application-defined data.</param>
        /// <returns>True to continue enumeration; false to stop.</returns>
        public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

        /// <summary>
        /// Enumerates display monitors that intersect a region formed by the intersection of a specified clipping rectangle and the visible region of a device context.
        /// </summary>
        /// <param name="hdc">Handle to a display device context.</param>
        /// <param name="lprcClip">Pointer to a clipping rectangle.</param>
        /// <param name="lpfnEnum">Pointer to a callback function.</param>
        /// <param name="dwData">Application-defined data.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumdisplaymonitors"/>
        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        /// <summary>
        /// Retrieves information about a display device.
        /// </summary>
        /// <param name="lpDevice">Device name or null for the display adapter.</param>
        /// <param name="iDevNum">Device index.</param>
        /// <param name="lpDisplayDevice">Reference to a <see cref="DISPLAY_DEVICE"/> structure.</param>
        /// <param name="dwFlags">Flags.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumdisplaydevicesa"/>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        /// <summary>
        /// Retrieves information about a display monitor.
        /// </summary>
        /// <param name="hMonitor">Handle to the display monitor.</param>
        /// <param name="lpmi">Reference to a <see cref="MonitorInfoEx"/> structure.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getmonitorinfoa"/>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

        /// <summary>
        /// Retrieves the position of the cursor in screen coordinates.
        /// </summary>
        /// <param name="lpPoint">When this method returns, contains the cursor position.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getcursorpos"/>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out POINT lpPoint);

        /// <summary>
        /// Retrieves the dots per inch (DPI) for a display monitor.
        /// </summary>
        /// <param name="hmonitor">Handle to the monitor.</param>
        /// <param name="dpiType">The type of DPI to query.</param>
        /// <param name="dpiX">Receives the DPI value for the X axis.</param>
        /// <param name="dpiY">Receives the DPI value for the Y axis.</param>
        /// <returns>Status code (0 for success).</returns>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/shellscalingapi/nf-shellscalingapi-getdpiformonitor"/>
        [DllImport("Shcore.dll")]
        public static extern int GetDpiForMonitor(IntPtr hmonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

        /// <summary>
        /// Retrieves the value of a specified Desktop Window Manager (DWM) attribute for a window.
        /// </summary>
        /// <param name="hwnd">Handle to the window.</param>
        /// <param name="dwAttribute">The attribute to retrieve (use <see cref="DWMWA_EXTENDED_FRAME_BOUNDS"/> for frame bounds).</param>
        /// <param name="pvAttribute">Receives the attribute value.</param>
        /// <param name="cbAttribute">The size of the attribute value.</param>
        /// <returns>Status code (0 for success).</returns>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmgetwindowattribute"/>
        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out Rect pvAttribute, int cbAttribute);

        /// <summary>
        /// Use with <see cref="DwmGetWindowAttribute"/> to get the extended frame bounds rectangle.
        /// </summary>
        public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

        #endregion Monitor and Display Configuration

        #region DDC/CI (Monitor Brightness)

        /// <summary>
        /// The VCP code for monitor brightness.
        /// </summary>
        public const byte VCP_CODE_BRIGHTNESS = 0x10;

        /// <summary>
        /// Represents a handle to a physical monitor.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct PHYSICAL_MONITOR
        {
            public IntPtr hPhysicalMonitor;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szPhysicalMonitorDescription;
        }

        /// <summary>
        /// Destroys a list of physical monitor handles.
        /// </summary>
        /// <param name="dwPhysicalMonitorArraySize">The number of elements in the array.</param>
        /// <param name="pPhysicalMonitorArray">Array of <see cref="PHYSICAL_MONITOR"/> structures.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/dxva2/nf-dxva2-destroyphysicalmonitors"/>
        [DllImport("dxva2.dll", EntryPoint = "DestroyPhysicalMonitors")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, [In] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        /// <summary>
        /// Retrieves the physical monitors associated with a display monitor handle.
        /// </summary>
        /// <param name="hMonitor">Handle to the display monitor.</param>
        /// <param name="dwPhysicalMonitorArraySize">The number of physical monitors.</param>
        /// <param name="pPhysicalMonitorArray">Array to receive <see cref="PHYSICAL_MONITOR"/> structures.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/dxva2/nf-dxva2-getphysicalmonitorsfromhmonitor"/>
        [DllImport("dxva2.dll", EntryPoint = "GetPhysicalMonitorsFromHMONITOR")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetPhysicalMonitorsFromHMONITOR(
            IntPtr hMonitor, uint dwPhysicalMonitorArraySize, [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        /// <summary>
        /// Retrieves the current value of a VCP control for a monitor.
        /// </summary>
        /// <param name="hPhysicalMonitor">Handle to the physical monitor.</param>
        /// <param name="bVCPCode">VCP code to query (use <see cref="VCP_CODE_BRIGHTNESS"/> for brightness).</param>
        /// <param name="pvct">Reserved; set to IntPtr.Zero.</param>
        /// <param name="pdwCurrentValue">Receives the current value.</param>
        /// <param name="pdwMaximumValue">Receives the maximum value.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/dxva2/nf-dxva2-getvcpfeatureandvcpfeaturereply"/>
        [DllImport("dxva2.dll", EntryPoint = "GetVCPFeatureAndVCPFeatureReply")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetVCPFeatureAndVCPFeatureReply(
            IntPtr hPhysicalMonitor, byte bVCPCode, IntPtr pvct, out uint pdwCurrentValue, out uint pdwMaximumValue);

        /// <summary>
        /// Sets the value of a VCP control for a monitor.
        /// </summary>
        /// <param name="hPhysicalMonitor">Handle to the physical monitor.</param>
        /// <param name="bVCPCode">VCP code to set (use <see cref="VCP_CODE_BRIGHTNESS"/> for brightness).</param>
        /// <param name="dwNewValue">The new value to set.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/dxva2/nf-dxva2-setvcpfeature"/>
        [DllImport("dxva2.dll", EntryPoint = "SetVCPFeature")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetVCPFeature(IntPtr hPhysicalMonitor, byte bVCPCode, uint dwNewValue);

        /// <summary>
        /// Retrieves the length, in characters, of the DDC/CI capabilities string for a physical monitor.
        /// </summary>
        /// <param name="hPhysicalMonitor">A handle to the physical monitor.</param>
        /// <param name="pdwCapabilitiesStringLengthInCharacters">When this method returns, contains the length of the capabilities string, in characters.</param>
        /// <returns>True if the function succeeds; otherwise, false.</returns>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/dxva2/nf-dxva2-getcapabilitiesstringlength"/>
        [DllImport("dxva2.dll", EntryPoint = "GetCapabilitiesStringLength")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCapabilitiesStringLength(IntPtr hPhysicalMonitor, out uint pdwCapabilitiesStringLengthInCharacters);

        #endregion DDC/CI (Monitor Brightness)
    }
}