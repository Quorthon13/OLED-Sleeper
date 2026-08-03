using OLED_Sleeper.Features.MonitorDimming.Services.Interfaces;
using OLED_Sleeper.Native;
using System.Runtime.InteropServices;

namespace OLED_Sleeper.Features.MonitorDimming.Services
{
    /// <summary>
    /// Opens DDC/CI channels by matching a display device name against the enumerated monitors.
    /// Holds every native call the dimming feature makes.
    /// </summary>
    public class DdcCiAccess : IDdcCiAccess
    {
        /// <inheritdoc />
        public IDdcCiSession? OpenSession(string deviceName)
        {
            var hMonitor = FindMonitorHandle(deviceName);
            if (hMonitor == nint.Zero) return null;

            var physicalMonitors = new NativeMethods.PHYSICAL_MONITOR[1];
            if (!NativeMethods.GetPhysicalMonitorsFromHMONITOR(hMonitor, 1, physicalMonitors)) return null;

            return new DdcCiSession(physicalMonitors);
        }

        /// <summary>
        /// Finds the monitor handle (HMONITOR) for the given device name.
        /// </summary>
        /// <param name="deviceName">The display device name to match.</param>
        /// <returns>The HMONITOR handle, or zero when no monitor matched.</returns>
        private static nint FindMonitorHandle(string deviceName)
        {
            nint foundMonitor = nint.Zero;
            NativeMethods.MonitorEnumProc callback = (IntPtr hMonitor, IntPtr hdcMonitor, ref NativeMethods.Rect lprcMonitor, IntPtr dwData) =>
            {
                var mi = new NativeMethods.MonitorInfoEx { cbSize = Marshal.SizeOf(typeof(NativeMethods.MonitorInfoEx)) };
                if (NativeMethods.GetMonitorInfo(hMonitor, ref mi) && mi.szDevice == deviceName)
                {
                    foundMonitor = hMonitor;
                    return false; // Stop enumerating once we've found it
                }
                return true; // Continue enumerating
            };

            NativeMethods.EnumDisplayMonitors(nint.Zero, nint.Zero, callback, nint.Zero);
            return foundMonitor;
        }
    }
}
