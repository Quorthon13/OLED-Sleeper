// File: Services/MonitorInfoProvider.cs
using OLED_Sleeper.Models;
using OLED_Sleeper.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using OLED_Sleeper.Helpers;
using Serilog;
using OLED_Sleeper.Services.Monitor.Interfaces;

namespace OLED_Sleeper.Services.Monitor
{
    /// <summary>
    /// Provides monitor enumeration and hardware ID enrichment services.
    /// Implements <see cref="IMonitorInfoProvider"/> for dependency injection.
    /// </summary>
    public class MonitorInfoProvider : IMonitorInfoProvider
    {
        /// <summary>
        /// Enumerates all monitors connected to the system and returns their information.
        /// </summary>
        /// <returns>A list of <see cref="MonitorInfo"/> objects representing each monitor.</returns>
        public List<MonitorInfo> GetMonitors()
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
                        DisplayNumber = DisplayNumberParser.ParseDisplayNumber(mi.szDevice)
                    });
                }
                return true;
            };

            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);

            EnrichMonitorInfo(monitors);
            return monitors;
        }

        private void EnrichMonitorInfo(List<MonitorInfo> monitors)
        {
            EnrichWithDdcCiSupportInfo(monitors);

            EnrichWithHardwareIds(monitors);
        }

        private void EnrichWithDdcCiSupportInfo(List<MonitorInfo> monitors)
        {
            foreach (var monitor in monitors)
            {
                monitor.IsDdcCiSupported = CheckDdcCiSupport(monitor.DeviceName);
                Log.Debug("DDC/CI support for monitor with DeviceName {DeviceName}: {IsSupported}", monitor.DeviceName, monitor.IsDdcCiSupported);
            }
        }

        /// <summary>
        /// Enriches the list of monitors with hardware IDs by matching device names.
        /// </summary>
        /// <param name="monitors">The list of <see cref="MonitorInfo"/> objects to enrich.</param>
        private static void EnrichWithHardwareIds(List<MonitorInfo> monitors)
        {
            var displayDevice = new NativeMethods.DISPLAY_DEVICE { cb = Marshal.SizeOf(typeof(NativeMethods.DISPLAY_DEVICE)) };
            for (uint adapterIndex = 0; NativeMethods.EnumDisplayDevices(null, adapterIndex, ref displayDevice, 0); adapterIndex++)
            {
                // Only consider active display adapters
                if ((displayDevice.StateFlags & 1) == 0) continue;
                var monitorDevice = new NativeMethods.DISPLAY_DEVICE { cb = Marshal.SizeOf(typeof(NativeMethods.DISPLAY_DEVICE)) };
                for (uint monitorIndex = 0; NativeMethods.EnumDisplayDevices(displayDevice.DeviceName, monitorIndex, ref monitorDevice, 0); monitorIndex++)
                {
                    var foundMonitor = monitors.FirstOrDefault(m => m.DeviceName == displayDevice.DeviceName);
                    if (foundMonitor != null)
                    {
                        foundMonitor.HardwareId = monitorDevice.DeviceID;
                    }
                }
            }
        }

        private bool CheckDdcCiSupport(string deviceName)
        {
            bool isSupported = false;
            // This is a simplified version of the helper from DimmerService
            // to get a physical monitor handle for testing.
            NativeMethods.MonitorEnumProc callback = (IntPtr hMonitor, IntPtr hdc, ref NativeMethods.Rect rect, IntPtr data) =>
            {
                var mi = new NativeMethods.MonitorInfoEx();
                mi.cbSize = Marshal.SizeOf(mi);
                if (NativeMethods.GetMonitorInfo(hMonitor, ref mi) && mi.szDevice == deviceName)
                {
                    var physicalMonitors = new NativeMethods.PHYSICAL_MONITOR[1];
                    if (NativeMethods.GetPhysicalMonitorsFromHMONITOR(hMonitor, 1, physicalMonitors))
                    {
                        IntPtr hPhysicalMonitor = physicalMonitors[0].hPhysicalMonitor;
                        // Try the safe, read-only command. If it succeeds, the monitor supports DDC/CI.
                        if (NativeMethods.GetCapabilitiesStringLength(hPhysicalMonitor, out _))
                        {
                            isSupported = true;
                        }
                        NativeMethods.DestroyPhysicalMonitors(1, physicalMonitors);
                    }
                }
                return !isSupported; // Stop enumerating once we've found and checked our monitor.
            };

            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
            return isSupported;
        }
    }
}