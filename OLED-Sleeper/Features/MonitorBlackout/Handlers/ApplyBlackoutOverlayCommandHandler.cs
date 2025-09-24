using OLED_Sleeper.Core.Interfaces;
using OLED_Sleeper.Features.MonitorBlackout.Commands;
using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;
using OLED_Sleeper.Features.MonitorDimming.Services.Interfaces;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.MonitorInformation.Services.Interfaces;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplyBlackoutOverlayCommandHandler"/> class.
        /// </summary>
        /// <param name="monitorInfoManager"></param>
        /// <param name="monitorBlackoutService">The service responsible for showing/hiding blackout overlays.</param>
        /// <param name="monitorDimmingService">The service responsible for controlling monitor brightness.</param>
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

                var monitorInfo = await GetMonitorInfoAsync(command.HardwareId);

                // Task 1: Show the software blackout overlay.
                // We start this task but don't await it immediately.
                var showOverlayTask = _monitorBlackoutService.ShowBlackoutOverlayAsync(monitorInfo.HardwareId, monitorInfo.Bounds);

                // Task 2: If supported, also set the hardware brightness to 0 via DDC/CI.
                if (monitorInfo.IsDdcCiSupported)
                {
                    Log.Information("Monitor {HardwareId} supports DDC/CI. Setting brightness to 0 for blackout.", monitorInfo.HardwareId);
                    var dimTask = _monitorDimmingService.DimMonitorAsync(monitorInfo.HardwareId, 0);

                    // Await both the overlay and dimming tasks to complete concurrently.
                    await Task.WhenAll(showOverlayTask, dimTask);
                }
                else
                {
                    // If DDC/CI is not supported, just wait for the overlay task to complete.
                    await showOverlayTask;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to apply blackout for monitor {HardwareId}.", command.HardwareId);
            }
        }

        /// <summary>
        /// Asynchronously retrieves the <see cref="MonitorInfo"/> for the specified hardware ID by awaiting the MonitorListReady event.
        /// This method bridges the event-based monitor info retrieval to an awaitable Task, ensuring the handler can work with up-to-date monitor data.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor to retrieve.</param>
        /// <returns>The <see cref="MonitorInfo"/> for the specified hardware ID, or null if not found.</returns>
        private async Task<MonitorInfo?> GetMonitorInfoAsync(string? hardwareId)
        {
            var tcs = new TaskCompletionSource<MonitorInfo?>();

            void Handler(object? sender, IReadOnlyList<MonitorInfo> monitors)
            {
                _monitorInfoManager.MonitorListReady -= Handler;
                var monitor = monitors.FirstOrDefault(m => m.HardwareId == hardwareId);
                tcs.SetResult(monitor);
            }

            _monitorInfoManager.MonitorListReady += Handler;
            _monitorInfoManager.GetCurrentMonitorsAsync();

            return await tcs.Task;
        }
    }
}