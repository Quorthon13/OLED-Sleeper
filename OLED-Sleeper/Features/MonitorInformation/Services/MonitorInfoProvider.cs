using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.MonitorInformation.Services.Interfaces;
using OLED_Sleeper.Native;
using Serilog;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;

namespace OLED_Sleeper.Features.MonitorInformation.Services
{
    /// <summary>
    /// Reads the attached monitors through the Win32 display APIs and probes them over DDC/CI.
    /// Implements <see cref="IMonitorInfoProvider"/> for dependency injection.
    /// </summary>
    public class MonitorInfoProvider : IMonitorInfoProvider
    {
        /// <inheritdoc />
        public List<MonitorInfo> GetAllMonitorsBasicInfo()
        {
            var hardwareIdsByDeviceName = MapDeviceNamesToHardwareIds();
            var monitors = new List<MonitorInfo>();

            NativeMethods.MonitorEnumProc callback = (IntPtr hMonitor, IntPtr hdcMonitor, ref NativeMethods.Rect lprcMonitor, IntPtr dwData) =>
            {
                var mi = new NativeMethods.MonitorInfoEx { cbSize = Marshal.SizeOf(typeof(NativeMethods.MonitorInfoEx)) };
                if (NativeMethods.GetMonitorInfo(hMonitor, ref mi))
                {
                    NativeMethods.GetDpiForMonitor(hMonitor, NativeMethods.MonitorDpiType.MDT_EFFECTIVE_DPI, out uint dpiX, out _);
                    hardwareIdsByDeviceName.TryGetValue(mi.szDevice, out var hardwareId);

                    if (string.IsNullOrEmpty(hardwareId))
                    {
                        Log.Debug("No hardware ID resolved for {DeviceName}.", mi.szDevice);
                    }

                    monitors.Add(new MonitorInfo
                    {
                        DeviceName = mi.szDevice,
                        HardwareId = hardwareId ?? string.Empty,
                        Bounds = new Rect(
                            mi.rcMonitor.left,
                            mi.rcMonitor.top,
                            mi.rcMonitor.right - mi.rcMonitor.left,
                            mi.rcMonitor.bottom - mi.rcMonitor.top),
                        IsPrimary = (mi.dwFlags & NativeMethods.MONITORINFOF_PRIMARY) == NativeMethods.MONITORINFOF_PRIMARY,
                        Dpi = dpiX,
                        DisplayNumber = ParseDisplayNumber(mi.szDevice)
                    });
                }
                return true;
            };

            NativeMethods.EnumDisplayMonitors(nint.Zero, nint.Zero, callback, nint.Zero);
            return monitors;
        }

        /// <inheritdoc />
        public DdcCiCapabilities GetDdcCiCapabilities(MonitorInfo monitor)
        {
            bool isSupported = false;
            uint maxBrightness = 0;
            string deviceName = monitor.DeviceName;
            NativeMethods.MonitorEnumProc callback = (IntPtr hMonitor, IntPtr hdcMonitor, ref NativeMethods.Rect lprcMonitor, IntPtr dwData) =>
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

                            if (NativeMethods.GetVCPFeatureAndVCPFeatureReply(
                                    hPhysicalMonitor, NativeMethods.VCP_CODE_BRIGHTNESS, nint.Zero, out _, out var reportedMax))
                            {
                                maxBrightness = reportedMax;
                            }
                        }
                        NativeMethods.DestroyPhysicalMonitors(1, physicalMonitors);
                    }
                }
                return !isSupported; // Stop enumerating once we've found and checked our monitor.
            };

            NativeMethods.EnumDisplayMonitors(nint.Zero, nint.Zero, callback, nint.Zero);
            Log.Debug("DDC/CI probe for monitor with DeviceName {DeviceName}: supported {IsSupported}, maximum brightness {MaxBrightness}.",
                deviceName, isSupported, maxBrightness);
            return new DdcCiCapabilities(isSupported, maxBrightness);
        }

        /// <summary>
        /// Walks the attached display adapters once and maps each adapter's device name to the hardware ID
        /// of the monitor on it.
        /// </summary>
        /// <returns>The hardware ID for each adapter that reported one.</returns>
        private static Dictionary<string, string> MapDeviceNamesToHardwareIds()
        {
            var hardwareIdsByDeviceName = new Dictionary<string, string>();
            var adapter = new NativeMethods.DISPLAY_DEVICE { cb = Marshal.SizeOf(typeof(NativeMethods.DISPLAY_DEVICE)) };

            for (uint adapterIndex = 0; NativeMethods.EnumDisplayDevices(null, adapterIndex, ref adapter, 0); adapterIndex++)
            {
                if ((adapter.StateFlags & NativeMethods.DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) == 0) continue;

                var monitorDevice = new NativeMethods.DISPLAY_DEVICE { cb = Marshal.SizeOf(typeof(NativeMethods.DISPLAY_DEVICE)) };
                if (!NativeMethods.EnumDisplayDevices(adapter.DeviceName, 0, ref monitorDevice, 0)) continue;
                if (string.IsNullOrEmpty(monitorDevice.DeviceID)) continue;

                hardwareIdsByDeviceName[adapter.DeviceName] = monitorDevice.DeviceID;
                Log.Debug("HWID for monitor {DeviceName}: {HWID}", adapter.DeviceName, monitorDevice.DeviceID);
            }

            return hardwareIdsByDeviceName;
        }

        /// <summary>
        /// Parses the display number from a device name string.
        /// </summary>
        /// <param name="deviceName">The device name string.</param>
        /// <returns>The display number if found, otherwise -1.</returns>
        private static int ParseDisplayNumber(string deviceName)
        {
            var match = Regex.Match(deviceName, @"\d+$");
            if (match.Success && int.TryParse(match.Value, out int number))
            {
                return number;
            }
            return -1;
        }
    }
}
