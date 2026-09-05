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
            var identitiesByDeviceName = MapDeviceNamesToIdentities();
            var monitors = new List<MonitorInfo>();

            NativeMethods.MonitorEnumProc callback = (IntPtr hMonitor, IntPtr hdcMonitor, ref NativeMethods.Rect lprcMonitor, IntPtr dwData) =>
            {
                var mi = new NativeMethods.MonitorInfoEx { cbSize = Marshal.SizeOf(typeof(NativeMethods.MonitorInfoEx)) };
                if (NativeMethods.GetMonitorInfo(hMonitor, ref mi))
                {
                    NativeMethods.GetDpiForMonitor(hMonitor, NativeMethods.MonitorDpiType.MDT_EFFECTIVE_DPI, out uint dpiX, out _);
                    identitiesByDeviceName.TryGetValue(mi.szDevice, out var identity);

                    if (string.IsNullOrEmpty(identity.HardwareId))
                    {
                        Log.Debug("No hardware ID resolved for {DeviceName}.", mi.szDevice);
                    }

                    monitors.Add(new MonitorInfo
                    {
                        DeviceName = mi.szDevice,
                        HardwareId = identity.HardwareId ?? string.Empty,
                        TopologyLabel = identity.TopologyLabel ?? string.Empty,
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
                    uint count = CountPhysicalMonitors(hMonitor);
                    var physicalMonitors = new NativeMethods.PHYSICAL_MONITOR[count];
                    if (NativeMethods.GetPhysicalMonitorsFromHMONITOR(hMonitor, count, physicalMonitors))
                    {
                        foreach (var physicalMonitor in physicalMonitors)
                        {
                            if (!NativeMethods.GetCapabilitiesStringLength(physicalMonitor.hPhysicalMonitor, out _)) continue;

                            isSupported = true;

                            if (NativeMethods.GetVCPFeatureAndVCPFeatureReply(
                                    physicalMonitor.hPhysicalMonitor, NativeMethods.VCP_CODE_BRIGHTNESS, nint.Zero, out _, out var reportedMax))
                            {
                                maxBrightness = reportedMax;
                            }
                            break;
                        }
                        NativeMethods.DestroyPhysicalMonitors(count, physicalMonitors);
                    }
                }
                return !isSupported; // Stop enumerating once we've found and checked our monitor.
            };

            NativeMethods.EnumDisplayMonitors(nint.Zero, nint.Zero, callback, nint.Zero);
            Log.Debug("DDC/CI probe for {HardwareId} on {DeviceName}: supported {IsSupported}, maximum brightness {MaxBrightness}.",
                monitor.HardwareId, deviceName, isSupported, maxBrightness);
            return new DdcCiCapabilities(isSupported, maxBrightness);
        }

        /// <summary>
        /// Counts the physical monitors behind a display monitor handle. A duplicated desktop surface has one
        /// per panel, and <see cref="NativeMethods.GetPhysicalMonitorsFromHMONITOR"/> fails outright unless the
        /// array it is handed is exactly that size.
        /// </summary>
        /// <param name="hMonitor">Handle to the display monitor.</param>
        /// <returns>The number of physical monitors, or one when the count could not be read.</returns>
        private static uint CountPhysicalMonitors(nint hMonitor)
        {
            if (!NativeMethods.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out uint count) || count == 0)
            {
                return 1;
            }

            return count;
        }

        /// <summary>
        /// Walks the attached display adapters once and works out which monitor each adapter's desktop
        /// surface belongs to.
        /// </summary>
        /// <returns>The identity of each adapter that resolved one.</returns>
        private static Dictionary<string, MonitorIdentity> MapDeviceNamesToIdentities()
        {
            var identitiesByDeviceName = new Dictionary<string, MonitorIdentity>();
            var adapter = new NativeMethods.DISPLAY_DEVICE { cb = Marshal.SizeOf(typeof(NativeMethods.DISPLAY_DEVICE)) };
            List<DisplayPath>? displayPaths = null;

            for (uint adapterIndex = 0; NativeMethods.EnumDisplayDevices(null, adapterIndex, ref adapter, 0); adapterIndex++)
            {
                if ((adapter.StateFlags & NativeMethods.DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) == 0) continue;

                var hardwareIds = ReadMonitorHardwareIds(adapter.DeviceName);
                if (hardwareIds.Count == 0) continue;

                var identity = hardwareIds.Count == 1
                    ? new MonitorIdentity(hardwareIds[0], string.Empty)
                    : ResolveDuplicatedIdentity(adapter.DeviceName, hardwareIds, displayPaths ??= ReadActiveDisplayPaths());

                if (string.IsNullOrEmpty(identity.HardwareId)) continue;

                identitiesByDeviceName[adapter.DeviceName] = identity;
            }

            return identitiesByDeviceName;
        }

        /// <summary>
        /// Reads the hardware ID of every monitor an adapter reports, by child index.
        /// </summary>
        /// <param name="adapterDeviceName">The adapter's device name, such as <c>\\.\DISPLAY1</c>.</param>
        /// <returns>The hardware IDs, with an empty entry for any child that reported none.</returns>
        private static List<string> ReadMonitorHardwareIds(string adapterDeviceName)
        {
            var hardwareIds = new List<string>();
            var monitorDevice = new NativeMethods.DISPLAY_DEVICE { cb = Marshal.SizeOf(typeof(NativeMethods.DISPLAY_DEVICE)) };

            for (uint childIndex = 0; NativeMethods.EnumDisplayDevices(adapterDeviceName, childIndex, ref monitorDevice, 0); childIndex++)
            {
                hardwareIds.Add(monitorDevice.DeviceID ?? string.Empty);
            }

            return hardwareIds;
        }

        /// <summary>
        /// Works out the identity of an adapter reporting more than one monitor, which is what a duplicated
        /// desktop surface looks like. The children come in no useful order — a virtual display can sit at
        /// index zero while the panel the surface belongs to sits below it — so the active display paths
        /// decide: the panel this adapter's own path drives is the monitor, and every child holding a path of
        /// its own contributes its display number to the label Windows shows, such as <c>1|2</c>.
        /// </summary>
        /// <param name="adapterDeviceName">The adapter's device name, such as <c>\\.\DISPLAY1</c>.</param>
        /// <param name="hardwareIds">The adapter's hardware IDs, by child index.</param>
        /// <param name="displayPaths">The active display paths, in the order Windows numbers them.</param>
        /// <returns>The identity, falling back to the first child when no path named the surface's panel.</returns>
        private static MonitorIdentity ResolveDuplicatedIdentity(
            string adapterDeviceName, List<string> hardwareIds, List<DisplayPath> displayPaths)
        {
            string surfaceDevicePath = displayPaths
                .FirstOrDefault(path => string.Equals(path.GdiDeviceName, adapterDeviceName, StringComparison.OrdinalIgnoreCase))
                .DevicePath ?? string.Empty;

            var monitorDevice = new NativeMethods.DISPLAY_DEVICE { cb = Marshal.SizeOf(typeof(NativeMethods.DISPLAY_DEVICE)) };
            string hardwareId = string.Empty;
            var displayNumbers = new List<int>();

            for (uint childIndex = 0; childIndex < hardwareIds.Count; childIndex++)
            {
                if (!NativeMethods.EnumDisplayDevices(
                        adapterDeviceName, childIndex, ref monitorDevice, NativeMethods.EDD_GET_DEVICE_INTERFACE_NAME))
                {
                    break;
                }

                string devicePath = monitorDevice.DeviceID ?? string.Empty;
                if (devicePath.Length == 0) continue;

                if (string.Equals(devicePath, surfaceDevicePath, StringComparison.OrdinalIgnoreCase))
                {
                    hardwareId = hardwareIds[(int)childIndex];
                }

                int pathIndex = displayPaths.FindIndex(
                    path => string.Equals(path.DevicePath, devicePath, StringComparison.OrdinalIgnoreCase));

                if (pathIndex >= 0) displayNumbers.Add(pathIndex + 1);
            }

            if (hardwareId.Length == 0)
            {
                Log.Debug("{DeviceName} reports {Count} monitors and no active display path named one of them. Falling back to {HardwareId}.",
                    adapterDeviceName, hardwareIds.Count, hardwareIds[0]);
                hardwareId = hardwareIds[0];
            }

            displayNumbers.Sort();
            string topologyLabel = displayNumbers.Count > 1 ? string.Join('|', displayNumbers) : string.Empty;
            return new MonitorIdentity(hardwareId, topologyLabel);
        }

        /// <summary>
        /// Reads the display paths that make up the current desktop, in the order Windows numbers them:
        /// the first path is display 1.
        /// </summary>
        /// <returns>The active paths, empty when the display configuration could not be read.</returns>
        private static List<DisplayPath> ReadActiveDisplayPaths()
        {
            var displayPaths = new List<DisplayPath>();

            if (NativeMethods.GetDisplayConfigBufferSizes(
                    NativeMethods.QDC_ONLY_ACTIVE_PATHS, out uint pathCount, out uint modeCount) != 0)
            {
                return displayPaths;
            }

            var paths = new NativeMethods.DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new NativeMethods.DISPLAYCONFIG_MODE_INFO[modeCount];

            if (NativeMethods.QueryDisplayConfig(
                    NativeMethods.QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, nint.Zero) != 0)
            {
                return displayPaths;
            }

            for (uint pathIndex = 0; pathIndex < pathCount; pathIndex++)
            {
                var path = paths[pathIndex];

                var sourceName = new NativeMethods.DISPLAYCONFIG_SOURCE_DEVICE_NAME
                {
                    header = new NativeMethods.DISPLAYCONFIG_DEVICE_INFO_HEADER
                    {
                        type = NativeMethods.DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME,
                        size = (uint)Marshal.SizeOf(typeof(NativeMethods.DISPLAYCONFIG_SOURCE_DEVICE_NAME)),
                        adapterId = path.sourceInfo.adapterId,
                        id = path.sourceInfo.id
                    }
                };

                var targetName = new NativeMethods.DISPLAYCONFIG_TARGET_DEVICE_NAME
                {
                    header = new NativeMethods.DISPLAYCONFIG_DEVICE_INFO_HEADER
                    {
                        type = NativeMethods.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME,
                        size = (uint)Marshal.SizeOf(typeof(NativeMethods.DISPLAYCONFIG_TARGET_DEVICE_NAME)),
                        adapterId = path.targetInfo.adapterId,
                        id = path.targetInfo.id
                    }
                };

                bool sourceRead = NativeMethods.DisplayConfigGetDeviceInfo(ref sourceName) == 0;
                bool targetRead = NativeMethods.DisplayConfigGetDeviceInfo(ref targetName) == 0;

                displayPaths.Add(new DisplayPath(
                    sourceRead ? sourceName.viewGdiDeviceName : string.Empty,
                    targetRead ? targetName.monitorDevicePath : string.Empty));
            }

            return displayPaths;
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

        /// <summary>
        /// What a desktop surface is: the monitor it belongs to, and how Windows numbers the panels showing it.
        /// </summary>
        /// <param name="HardwareId">The hardware ID of the monitor the surface belongs to.</param>
        /// <param name="TopologyLabel">The display numbers sharing the surface, such as <c>1|2</c>. Empty when it is not duplicated.</param>
        private readonly record struct MonitorIdentity(string HardwareId, string TopologyLabel);

        /// <summary>
        /// One active display path: a desktop surface and the panel it drives.
        /// </summary>
        /// <param name="GdiDeviceName">The source's GDI device name, such as <c>\\.\DISPLAY1</c>.</param>
        /// <param name="DevicePath">The target's device interface path.</param>
        private readonly record struct DisplayPath(string GdiDeviceName, string DevicePath);
    }
}
