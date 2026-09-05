using OLED_Sleeper.Features.MonitorDimming.Services.Interfaces;
using OLED_Sleeper.Native;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace OLED_Sleeper.Features.MonitorDimming.Services
{
    /// <summary>
    /// Opens DDC/CI channels by matching a display device name against the enumerated monitors.
    /// Holds every native call the dimming feature makes.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DdcCiAccess : IDdcCiAccess
    {
        /// <inheritdoc />
        public IDdcCiSession? OpenSession(string deviceName)
        {
            var hMonitor = FindMonitorHandle(deviceName);
            if (hMonitor == nint.Zero) return null;

            if (!NativeMethods.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out uint count) || count == 0)
            {
                count = 1;
            }

            var physicalMonitors = new NativeMethods.PHYSICAL_MONITOR[count];
            if (!NativeMethods.GetPhysicalMonitorsFromHMONITOR(hMonitor, count, physicalMonitors)) return null;

            int index = count == 1 ? 0 : FindRespondingMonitor(physicalMonitors);
            if (index < 0)
            {
                NativeMethods.DestroyPhysicalMonitors(count, physicalMonitors);
                return null;
            }

            return new DdcCiSession(physicalMonitors, index);
        }

        /// <summary>
        /// Finds the physical monitor that answers DDC/CI. A duplicated desktop surface hands back one physical
        /// monitor per panel in no useful order, and a virtual display can be the one that comes first.
        /// </summary>
        /// <param name="physicalMonitors">The physical monitors behind one display monitor handle.</param>
        /// <returns>The index of the first monitor that answered, or -1 when none did.</returns>
        private static int FindRespondingMonitor(NativeMethods.PHYSICAL_MONITOR[] physicalMonitors)
        {
            for (int index = 0; index < physicalMonitors.Length; index++)
            {
                if (NativeMethods.GetVCPFeatureAndVCPFeatureReply(
                        physicalMonitors[index].hPhysicalMonitor, NativeMethods.VCP_CODE_BRIGHTNESS, nint.Zero, out _, out _))
                {
                    return index;
                }
            }

            return -1;
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
