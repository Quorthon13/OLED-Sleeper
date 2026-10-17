using OLED_Sleeper.Features.MonitorIdleDetection.Media.Interfaces;
using OLED_Sleeper.Native;
using Serilog;

namespace OLED_Sleeper.Features.MonitorIdleDetection.Media
{
    /// <summary>
    /// Reads the display-required flag from the system execution state.
    /// </summary>
    /// <remarks>
    /// The documentation lists <c>ES_DISPLAY_REQUIRED</c> among the flags this level reports but does not promise that it includes
    /// requests made through <c>PowerCreateRequest</c>, which Chromium-based browsers use. Whether it does has to be confirmed on a
    /// real machine; this class is the single place to replace if it does not.
    /// </remarks>
    public class ExecutionStateDisplayRequiredSignal : IDisplayRequiredSignal
    {
        private bool _failureLogged;

        /// <inheritdoc />
        public bool IsDisplayRequired()
        {
            int status = NativeMethods.CallNtPowerInformation(NativeMethods.SystemExecutionState, nint.Zero, 0, out uint state, sizeof(uint));
            if (status == 0) return (state & NativeMethods.ES_DISPLAY_REQUIRED) != 0;

            if (!_failureLogged)
            {
                _failureLogged = true;
                Log.Warning("CallNtPowerInformation(SystemExecutionState) failed with status 0x{Status:X8}. The display-required signal is treated as unset.", status);
            }
            return false;
        }
    }
}
