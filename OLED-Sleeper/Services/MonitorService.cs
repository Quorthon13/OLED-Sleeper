// File: Services/MonitorService.cs
using OLED_Sleeper.Models;
using OLED_Sleeper.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using OLED_Sleeper.Helpers;

namespace OLED_Sleeper.Services
{
    /// <summary>
    /// Provides monitor enumeration and hardware ID enrichment services.
    /// Implements <see cref="IMonitorService"/> for dependency injection.
    /// </summary>
    public class MonitorService : IMonitorService
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
            EnrichWithHardwareIds(monitors);
            return monitors;
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
    }
}