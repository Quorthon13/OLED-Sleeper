using OLED_Sleeper.Features.MonitorDimming.Services.Interfaces;
using OLED_Sleeper.Native;
using System.Diagnostics.CodeAnalysis;

namespace OLED_Sleeper.Features.MonitorDimming.Services
{
    /// <summary>
    /// A DDC/CI channel backed by a physical monitor handle from dxva2.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal sealed class DdcCiSession : IDdcCiSession
    {
        private readonly NativeMethods.PHYSICAL_MONITOR[] _physicalMonitors;
        private readonly nint _hPhysicalMonitor;
        private bool _disposed;

        internal DdcCiSession(NativeMethods.PHYSICAL_MONITOR[] physicalMonitors)
        {
            _physicalMonitors = physicalMonitors;
            _hPhysicalMonitor = physicalMonitors[0].hPhysicalMonitor;
        }

        /// <inheritdoc />
        public uint? GetBrightness()
        {
            if (!NativeMethods.GetVCPFeatureAndVCPFeatureReply(
                    _hPhysicalMonitor, NativeMethods.VCP_CODE_BRIGHTNESS, nint.Zero, out var currentBrightness, out _))
            {
                return null;
            }

            return currentBrightness;
        }

        /// <inheritdoc />
        public bool SetBrightness(uint brightness)
        {
            return NativeMethods.SetVCPFeature(_hPhysicalMonitor, NativeMethods.VCP_CODE_BRIGHTNESS, brightness);
        }

        /// <summary>
        /// Releases the physical monitor handle. Calls after the first do nothing.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            NativeMethods.DestroyPhysicalMonitors(1, _physicalMonitors);
        }
    }
}
