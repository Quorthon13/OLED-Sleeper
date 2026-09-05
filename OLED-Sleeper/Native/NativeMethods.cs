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

        /// <summary>
        /// Retrieves the name of the class to which the specified window belongs.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <param name="lpClassName">The buffer that receives the class name.</param>
        /// <param name="nMaxCount">The length of the buffer, in characters.</param>
        /// <returns>The number of characters copied, excluding the terminating null; zero on failure.</returns>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getclassnamew"/>
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

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
        /// Set in <see cref="MonitorInfoEx.dwFlags"/> when the monitor is the primary display.
        /// </summary>
        public const uint MONITORINFOF_PRIMARY = 0x1;

        /// <summary>
        /// Set in <see cref="DISPLAY_DEVICE.StateFlags"/> when the adapter is part of the desktop.
        /// </summary>
        public const uint DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 0x1;

        /// <summary>
        /// Passed to <see cref="EnumDisplayDevices"/> to return the device interface path in
        /// <see cref="DISPLAY_DEVICE.DeviceID"/> instead of the monitor's hardware ID.
        /// </summary>
        public const uint EDD_GET_DEVICE_INTERFACE_NAME = 0x1;

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
        /// Places the window above all non-topmost windows.
        /// </summary>
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        /// <summary>
        /// Does not activate the window.
        /// </summary>
        public const uint SWP_NOACTIVATE = 0x0010;

        /// <summary>
        /// Retains the window's current size.
        /// </summary>
        public const uint SWP_NOSIZE = 0x0001;

        /// <summary>
        /// Retains the window's current position.
        /// </summary>
        public const uint SWP_NOMOVE = 0x0002;

        /// <summary>
        /// Use with <see cref="DwmGetWindowAttribute"/> to get the extended frame bounds rectangle.
        /// </summary>
        public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

        /// <summary>
        /// Delegate for monitor enumeration callback used by <see cref="EnumDisplayMonitors"/>.
        /// </summary>
        /// <param name="hMonitor">Handle to the display monitor.</param>
        /// <param name="hdcMonitor">Handle to a device context.</param>
        /// <param name="lprcMonitor">Pointer to a <see cref="Rect"/> structure with the display monitor rectangle.</param>
        /// <param name="dwData">Application-defined data.</param>
        /// <returns>True to continue enumeration; false to stop.</returns>
        public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref NativeMethods.Rect lprcMonitor, IntPtr dwData);

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
        /// Retrieves a handle to the display monitor that has the largest area of intersection with a specified rectangle.
        /// Used to get the correct DPI for a monitor bounds (even when X is negative).
        /// </summary>
        /// <param name="lprc">Pointer to a <see cref="Rect"/> structure that defines the rectangle.</param>
        /// <param name="dwFlags">Determines the function's return value if the rectangle does not intersect any display monitor.</param>
        /// <returns>A handle to the display monitor.</returns>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-monitorfromrect"/>
        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromRect(ref Rect lprc, uint dwFlags);

        /// <summary>
        /// Places the window at the exact physical coordinates and size (bypasses WPF DIP issues).
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        #endregion Monitor and Display Configuration

        #region DDC/CI (Monitor Brightness)

        /// <summary>
        /// The VCP code for monitor brightness.
        /// </summary>
        public const byte VCP_CODE_BRIGHTNESS = 0x10;

        /// <summary>
        /// Represents a handle to a physical monitor. The description is a wide string, so the struct is
        /// 264 bytes; marshalling it as ANSI hands dxva2 a 136-byte buffer to write 264 bytes into.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
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
        /// Retrieves the number of physical monitors behind a display monitor handle.
        /// </summary>
        /// <param name="hMonitor">Handle to the display monitor.</param>
        /// <param name="pdwNumberOfPhysicalMonitors">Receives the number of physical monitors.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/dxva2/nf-dxva2-getnumberofphysicalmonitorsfromhmonitor"/>
        [DllImport("dxva2.dll", EntryPoint = "GetNumberOfPhysicalMonitorsFromHMONITOR")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, out uint pdwNumberOfPhysicalMonitors);

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

        #region Display Configuration (CCD)

        /// <summary>
        /// Restricts a <see cref="QueryDisplayConfig"/> query to paths that are currently in use.
        /// </summary>
        public const uint QDC_ONLY_ACTIVE_PATHS = 0x2;

        /// <summary>
        /// Asks <see cref="DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DEVICE_NAME)"/> for the GDI device name of a source.
        /// </summary>
        public const uint DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1;

        /// <summary>
        /// Asks <see cref="DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_TARGET_DEVICE_NAME)"/> for the name and device path of a target.
        /// </summary>
        public const uint DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME = 2;

        /// <summary>
        /// Locally unique identifier for a display adapter.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        /// <summary>
        /// Describes the source (the desktop surface) end of a display path.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_PATH_SOURCE_INFO
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint statusFlags;
        }

        /// <summary>
        /// A ratio of two unsigned integers, used for refresh rates.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_RATIONAL
        {
            public uint Numerator;
            public uint Denominator;
        }

        /// <summary>
        /// Describes the target (the panel) end of a display path.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_PATH_TARGET_INFO
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint outputTechnology;
            public uint rotation;
            public uint scaling;
            public DISPLAYCONFIG_RATIONAL refreshRate;
            public uint scanLineOrdering;

            [MarshalAs(UnmanagedType.Bool)]
            public bool targetAvailable;

            public uint statusFlags;
        }

        /// <summary>
        /// One source-to-target display path.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_PATH_INFO
        {
            public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
            public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
            public uint flags;
        }

        /// <summary>
        /// A mode entry. The 48-byte payload is a union this application does not read; only the size matters,
        /// because <see cref="QueryDisplayConfig"/> refuses to run without a mode array to fill.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_MODE_INFO
        {
            public uint infoType;
            public uint id;
            public LUID adapterId;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
            public byte[] union;
        }

        /// <summary>
        /// Names the request and its subject for a <see cref="DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DEVICE_NAME)"/> call.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_DEVICE_INFO_HEADER
        {
            public uint type;
            public uint size;
            public LUID adapterId;
            public uint id;
        }

        /// <summary>
        /// Receives the GDI device name, such as <c>\\.\DISPLAY1</c>, of a display path's source.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string viewGdiDeviceName;
        }

        /// <summary>
        /// Receives the friendly name and device interface path of a display path's target.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DISPLAYCONFIG_TARGET_DEVICE_NAME
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public uint flags;
            public uint outputTechnology;
            public ushort edidManufactureId;
            public ushort edidProductCodeId;
            public uint connectorInstance;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string monitorFriendlyDeviceName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string monitorDevicePath;
        }

        /// <summary>
        /// Retrieves the array sizes <see cref="QueryDisplayConfig"/> needs.
        /// </summary>
        /// <param name="flags">The topology to size for, such as <see cref="QDC_ONLY_ACTIVE_PATHS"/>.</param>
        /// <param name="numPathArrayElements">Receives the number of path elements.</param>
        /// <param name="numModeInfoArrayElements">Receives the number of mode elements.</param>
        /// <returns>ERROR_SUCCESS (0) on success; otherwise, a Win32 error code.</returns>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getdisplayconfigbuffersizes"/>
        [DllImport("user32.dll", EntryPoint = "GetDisplayConfigBufferSizes")]
        public static extern int GetDisplayConfigBufferSizes(uint flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);

        /// <summary>
        /// Retrieves the display paths that make up the current desktop.
        /// </summary>
        /// <param name="flags">The topology to query, such as <see cref="QDC_ONLY_ACTIVE_PATHS"/>.</param>
        /// <param name="numPathArrayElements">On entry the array size, on return the number of paths written.</param>
        /// <param name="pathArray">Receives the paths.</param>
        /// <param name="numModeInfoArrayElements">On entry the array size, on return the number of modes written.</param>
        /// <param name="modeInfoArray">Receives the modes.</param>
        /// <param name="currentTopologyId">Reserved for database queries; pass zero.</param>
        /// <returns>ERROR_SUCCESS (0) on success; otherwise, a Win32 error code.</returns>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-querydisplayconfig"/>
        [DllImport("user32.dll", EntryPoint = "QueryDisplayConfig")]
        public static extern int QueryDisplayConfig(
            uint flags,
            ref uint numPathArrayElements,
            [Out] DISPLAYCONFIG_PATH_INFO[] pathArray,
            ref uint numModeInfoArrayElements,
            [Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
            IntPtr currentTopologyId);

        /// <summary>
        /// Retrieves the GDI device name of a display path's source.
        /// </summary>
        /// <param name="requestPacket">The request, with its header filled in.</param>
        /// <returns>ERROR_SUCCESS (0) on success; otherwise, a Win32 error code.</returns>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-displayconfiggetdeviceinfo"/>
        [DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo")]
        public static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DEVICE_NAME requestPacket);

        /// <summary>
        /// Retrieves the name and device interface path of a display path's target.
        /// </summary>
        /// <param name="requestPacket">The request, with its header filled in.</param>
        /// <returns>ERROR_SUCCESS (0) on success; otherwise, a Win32 error code.</returns>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-displayconfiggetdeviceinfo"/>
        [DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo")]
        public static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_TARGET_DEVICE_NAME requestPacket);

        #endregion Display Configuration (CCD)

        #region Process Memory Management

        /// <summary>
        /// Sets the minimum and maximum working set sizes for the specified process.
        /// </summary>
        /// <param name="process">A handle to the process whose working set size is to be set.</param>
        /// <param name="minimumWorkingSetSize">The minimum number of bytes to be in the working set of the process.</param>
        /// <param name="maximumWorkingSetSize">The maximum number of bytes to be in the working set of the process.</param>
        /// <returns>If the function succeeds, the return value is nonzero; otherwise, it is zero.</returns>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-setprocessworkingsetsize"/>
        [DllImport("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int SetProcessWorkingSetSize(IntPtr process, int minimumWorkingSetSize, int maximumWorkingSetSize);

        #endregion Process Memory Management
    }
}