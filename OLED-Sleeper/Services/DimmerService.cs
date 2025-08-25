using OLED_Sleeper.Models;
using OLED_Sleeper.Native;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace OLED_Sleeper.Services
{
    public class DimmerService : IDimmerService
    {
        private readonly IMonitorManagerService _monitorManager;
        private readonly IBrightnessStateService _brightnessStateService;
        private readonly Dictionary<string, uint> _originalBrightnessLevels;

        public DimmerService(IMonitorManagerService monitorManager, IBrightnessStateService brightnessStateService)
        {
            _monitorManager = monitorManager;
            _brightnessStateService = brightnessStateService;
            // --- Load the previous state on startup ---
            _originalBrightnessLevels = _brightnessStateService.LoadState();
        }

        public void DimMonitor(string hardwareId, int dimLevel)
        {
            WithPhysicalMonitor(hardwareId, hPhysicalMonitor =>
            {
                if (NativeMethods.GetVCPFeatureAndVCPFeatureReply(hPhysicalMonitor, NativeMethods.VCP_CODE_BRIGHTNESS, IntPtr.Zero, out uint currentBrightness, out _))
                {
                    _originalBrightnessLevels[hardwareId] = currentBrightness;
                    // --- Save state to disk immediately after changing it ---
                    _brightnessStateService.SaveState(_originalBrightnessLevels);

                    if (NativeMethods.SetVCPFeature(hPhysicalMonitor, NativeMethods.VCP_CODE_BRIGHTNESS, (uint)dimLevel))
                    {
                        Log.Information("Successfully dimmed monitor {HardwareId} to {DimLevel}%.", hardwareId, dimLevel);
                    }
                }
            });
        }

        public void UndimMonitor(string hardwareId)
        {
            if (_originalBrightnessLevels.TryGetValue(hardwareId, out uint originalBrightness))
            {
                RestoreBrightness(hardwareId, originalBrightness);
                _originalBrightnessLevels.Remove(hardwareId);
                // --- Save state to disk immediately after changing it ---
                _brightnessStateService.SaveState(_originalBrightnessLevels);
            }
        }

        /// <summary>
        /// Helper method to safely get and destroy physical monitor handles.
        /// </summary>
        private void WithPhysicalMonitor(string hardwareId, Action<IntPtr> action)
        {
            var allMonitors = _monitorManager.GetCurrentMonitors();
            var targetMonitor = allMonitors.FirstOrDefault(m => m.HardwareId == hardwareId);
            if (targetMonitor == null) return;

            IntPtr hMonitor = NativeMethods.MonitorFromWindow(IntPtr.Zero, NativeMethods.MONITOR_DEFAULTTONEAREST); // A bit of a hack to get an HWND on the right monitor
            // A more robust solution would find a window on the target monitor or use EnumDisplayMonitors.
            // For now, we find the monitor by iterating through them.

            var monitors = new List<IntPtr>();
            NativeMethods.MonitorEnumProc callback = (IntPtr monitor, IntPtr hdc, ref NativeMethods.Rect rect, IntPtr data) =>
            {
                var mi = new NativeMethods.MonitorInfoEx();
                mi.cbSize = Marshal.SizeOf(mi);
                if (NativeMethods.GetMonitorInfo(monitor, ref mi) && mi.szDevice == targetMonitor.DeviceName)
                {
                    monitors.Add(monitor);
                }
                return true;
            };
            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
            hMonitor = monitors.FirstOrDefault();

            if (hMonitor == IntPtr.Zero) return;

            var physicalMonitors = new NativeMethods.PHYSICAL_MONITOR[1];
            if (NativeMethods.GetPhysicalMonitorsFromHMONITOR(hMonitor, 1, physicalMonitors))
            {
                IntPtr hPhysicalMonitor = physicalMonitors[0].hPhysicalMonitor;
                try
                {
                    action(hPhysicalMonitor);
                }
                finally
                {
                    NativeMethods.DestroyPhysicalMonitors(1, physicalMonitors);
                }
            }
        }

        public void RestoreBrightness(string hardwareId, uint originalBrightness)
        {
            WithPhysicalMonitor(hardwareId, hPhysicalMonitor =>
            {
                if (NativeMethods.SetVCPFeature(hPhysicalMonitor, NativeMethods.VCP_CODE_BRIGHTNESS, originalBrightness))
                {
                    Log.Information("Restored original brightness {OriginalBrightness} for monitor {HardwareId}.", originalBrightness, hardwareId);
                }
            });
        }

        public Dictionary<string, uint> GetDimmedMonitors()
        {
            // Return a copy to prevent external modification
            return new Dictionary<string, uint>(_originalBrightnessLevels);
        }
    }
}