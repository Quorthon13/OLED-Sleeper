using OLED_Sleeper.Features.MonitorBehavior.Commands;
using OLED_Sleeper.Features.MonitorIdleDetection.Services.Interfaces;
using OLED_Sleeper.Features.MonitorInformation.Services.Interfaces;
using OLED_Sleeper.Features.UserSettings.Commands;
using OLED_Sleeper.Messaging.Interfaces;
using Serilog;

namespace OLED_Sleeper.Features.UserSettings.Handlers
{
    /// <summary>
    /// Handles the execution of the <see cref="ApplySettingsChangeCommand"/>.
    /// Updates idle detection with the changed settings, then restores the monitors they cover.
    /// </summary>
    public class ApplySettingsChangeCommandHandler(
        IMediator mediator,
        IMonitorIdleDetectionService monitorIdleDetectionService,
        IMonitorInfoManager monitorInfoManager) : ICommandHandler<ApplySettingsChangeCommand>
    {
        /// <summary>
        /// Applies the changed settings to idle detection, then restores each affected monitor.
        /// The restore commands are issued only after the new settings are in effect. The geometry comes
        /// from the cached monitor list.
        /// </summary>
        /// <param name="command">The command containing the changed settings.</param>
        public async Task HandleAsync(ApplySettingsChangeCommand command)
        {
            try
            {
                var monitors = await monitorInfoManager.GetCurrentMonitorsAsync();
                await monitorIdleDetectionService.UpdateSettingsAsync(command.Settings, monitors);

                foreach (var setting in command.Settings)
                {
                    SendRestoreMonitorStateCommand(setting.HardwareId);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to apply changed monitor settings.");
            }
        }

        /// <summary>
        /// Sends a command to restore a monitor's state by undimming it and hiding any blackout overlay.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor to restore.</param>
        private void SendRestoreMonitorStateCommand(string hardwareId)
        {
            var restoreCommand = new RestoreMonitorStateCommand { HardwareId = hardwareId };
            mediator.SendAsync(restoreCommand);
            Log.Information("RestoreMonitorStateCommand sent for monitor {HardwareId}.", hardwareId);
        }
    }
}
