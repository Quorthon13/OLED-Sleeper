using OLED_Sleeper.Features.MonitorBlackout.Commands;
using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;
using OLED_Sleeper.Features.MonitorDimming.Services.Interfaces;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.MonitorInformation.Services.Interfaces;
using OLED_Sleeper.Messaging.Interfaces;
using Serilog;

namespace OLED_Sleeper.Features.MonitorBlackout.Handlers
{
    /// <summary>
    /// Handles the execution of the <see cref="ApplyBlackoutOverlayCommand"/>.
    /// This class contains the business logic for applying the blackout effect to a monitor,
    /// which includes showing a software overlay and setting the hardware brightness to zero if supported.
    /// </summary>
    public class ApplyBlackoutOverlayCommandHandler : ICommandHandler<ApplyBlackoutOverlayCommand>
    {
        private readonly IMonitorInfoManager _monitorInfoManager;
        private readonly IMonitorBlackoutService _monitorBlackoutService;
        private readonly IMonitorDimmingService _monitorDimmingService;

        public ApplyBlackoutOverlayCommandHandler(
            IMonitorInfoManager monitorInfoManager,
            IMonitorBlackoutService monitorBlackoutService,
            IMonitorDimmingService monitorDimmingService)
        {
            _monitorInfoManager = monitorInfoManager;
            _monitorBlackoutService = monitorBlackoutService;
            _monitorDimmingService = monitorDimmingService;
        }

        /// <summary>
        /// Executes the blackout logic asynchronously based on the command's data.
        /// It shows a blackout overlay and, if the monitor supports DDC/CI,
        /// it simultaneously dims the monitor's brightness to 0.
        /// Exceptions are caught and logged to avoid silent failures.
        /// </summary>
        /// <param name="command">The command containing the details of the monitor to black out.</param>
        public async Task HandleAsync(ApplyBlackoutOverlayCommand command)
        {
            try
            {
                Log.Information("Executing ApplyBlackoutCommand for monitor {HardwareId}.", command.HardwareId);

                var monitors = await _monitorInfoManager.GetCurrentMonitorsAsync();
                var monitorInfo = monitors.FirstOrDefault(m => m.HardwareId == command.HardwareId);
                if (monitorInfo?.HardwareId == null)
                {
                    Log.Warning("Cannot apply blackout: no monitor found with HardwareId {HardwareId}.", command.HardwareId);
                    return;
                }

                var showOverlayTask = _monitorBlackoutService.ShowBlackoutOverlayAsync(monitorInfo.HardwareId, monitorInfo.Bounds);

                if (monitorInfo.IsDdcCiSupported)
                {
                    Log.Information("Monitor {HardwareId} supports DDC/CI. Setting brightness to 0 for blackout.", monitorInfo.HardwareId);
                    var dimTask = _monitorDimmingService.DimMonitorAsync(monitorInfo.HardwareId, 0);

                    await Task.WhenAll(showOverlayTask, dimTask);
                }
                else
                {
                    await showOverlayTask;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to apply blackout for monitor {HardwareId}.", command.HardwareId);
            }
        }
    }
}