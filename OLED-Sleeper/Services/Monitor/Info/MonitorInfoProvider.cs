using OLED_Sleeper.Models;
using OLED_Sleeper.Native;
using System.Runtime.InteropServices;
using System.Windows;
using OLED_Sleeper.Helpers;
using Serilog;
using OLED_Sleeper.Services.Monitor.Info.Interfaces;

namespace OLED_Sleeper.Services.Monitor.Info
{
    /// <summary>
    /// Provides monitor enumeration and hardware ID / DDC/CI support services.
    /// Implements <see cref="IMonitorInfoProvider"/> for dependency injection.
    /// </summary>
    public class MonitorInfoProvider : IMonitorInfoProvider
    {
        /// <summary>
        /// Enumerates all monitors connected to the system and returns their basic information (no enrichment).
        /// </summary>
        /// <returns>A list of <see cref="MonitorInfo"/> objects representing each monitor (basic info only).</returns>
        public List<MonitorInfo> GetAllMonitorsBasicInfo()
        {
            var monitors = new List<MonitorInfo>();

            // Callback for EnumDisplayMonitors to collect monitor info
            NativeMethods.MonitorEnumProc callback = (IntPtr hMonitor, IntPtr hdcMonitor, ref NativeMethods.Rect lprcMonitor, IntPtr dwData) =>
            {
                var mi = new NativeMethods.MonitorInfoEx { cbSize = Marshal.SizeOf(typeof(NativeMethods.MonitorInfoEx)) };
                if (NativeMethods.GetMonitorInfo(hMonitor, ref mi))
                {
                    NativeMethods.GetDpiForMonitor(hMonitor, NativeMethods.MonitorDpiType.MDT_EFFECTIVE_DPI, out uint dpiX, out _);
                    monitors.Add(new MonitorInfo
                    {
                        DeviceName = mi.szDevice,
                        Bounds = new Rect(
                            mi.rcMonitor.left,
                            mi.rcMonitor.top,
                            mi.rcMonitor.right - mi.rcMonitor.left,
                            mi.rcMonitor.bottom - mi.rcMonitor.top),
                        IsPrimary = (mi.dwFlags & 1) == 1,
                        Dpi = dpiX,
                        DisplayNumber = MonitorHelper.ParseDisplayNumber(mi.szDevice)
                    });
                }
                return true;
            };

            NativeMethods.EnumDisplayMonitors(nint.Zero, nint.Zero, callback, nint.Zero);
            return monitors;
        }

        /// <summary>
        /// Returns whether the given monitor supports DDC/CI.
        /// </summary>
        public bool GetDdcCiSupport(MonitorInfo monitor)
        {
            bool isSupported = false;
            string deviceName = monitor.DeviceName;
            NativeMethods.MonitorEnumProc callback = (IntPtr hMonitor, IntPtr hdc, ref NativeMethods.Rect rect, IntPtr data) =>
            {
                var mi = new NativeMethods.MonitorInfoEx();
                mi.cbSize = Marshal.SizeOf(mi);
                if (NativeMethods.GetMonitorInfo(hMonitor, ref mi) && mi.szDevice == deviceName)
                {
                    var physicalMonitors = new NativeMethods.PHYSICAL_MONITOR[1];
                    if (NativeMethods.GetPhysicalMonitorsFromHMONITOR(hMonitor, 1, physicalMonitors))
                    {
                        nint hPhysicalMonitor = physicalMonitors[0].hPhysicalMonitor;
                        if (NativeMethods.GetCapabilitiesStringLength(hPhysicalMonitor, out _))
                        {
                            isSupported = true;
                        }
                        NativeMethods.DestroyPhysicalMonitors(1, physicalMonitors);
                    }
                }
                return !isSupported; // Stop enumerating once we've found and checked our monitor.
            };

            NativeMethods.EnumDisplayMonitors(nint.Zero, nint.Zero, callback, nint.Zero);
            Log.Debug("DDC/CI support for monitor with DeviceName {DeviceName}: {IsSupported}", deviceName, isSupported);
            return isSupported;
        }

        /// <summary>
        /// Returns the hardware ID for the given monitor.
        /// </summary>
        public string GetHardwareId(MonitorInfo monitor)
        {
            string deviceName = monitor.DeviceName;
            string hardwareId = null;
            var displayDevice = new NativeMethods.DISPLAY_DEVICE { cb = Marshal.SizeOf(typeof(NativeMethods.DISPLAY_DEVICE)) };
            for (uint adapterIndex = 0; NativeMethods.EnumDisplayDevices(null, adapterIndex, ref displayDevice, 0); adapterIndex++)
            {
                if ((displayDevice.StateFlags & 1) == 0) continue;
                var monitorDevice = new NativeMethods.DISPLAY_DEVICE { cb = Marshal.SizeOf(typeof(NativeMethods.DISPLAY_DEVICE)) };
                for (uint monitorIndex = 0; NativeMethods.EnumDisplayDevices(displayDevice.DeviceName, monitorIndex, ref monitorDevice, 0); monitorIndex++)
                {
                    if (deviceName == displayDevice.DeviceName)
                    {
                        hardwareId = monitorDevice.DeviceID;
                        Log.Debug("HWID for monitor {DeviceName}: {HWID}", deviceName, hardwareId);
                        break;
                    }
                }
                if (hardwareId != null) break;
            }
            return hardwareId;
        }
    }
}