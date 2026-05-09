using OLED_Sleeper.Core.Interfaces;
using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;
using OLED_Sleeper.Features.MonitorDimming.Services.Interfaces;
using OLED_Sleeper.Features.MonitorIdleDetection.Services.Interfaces;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.MonitorInformation.Services.Interfaces;
using OLED_Sleeper.Features.MonitorState.Commands;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Features.UserSettings.Services.Interfaces;
using Serilog;

namespace OLED_Sleeper.Features.MonitorState.Handlers;

/// <summary>
/// Handles the <see cref="SynchronizeMonitorStateCommand"/> to reconcile the application's monitor state.
/// <para>
/// This handler is responsible for:
/// <list type="bullet">
/// <item><description>Removing overlays and resetting brightness for all old and newly connected monitors.</description></item>
/// <item><description>Updating managed monitor settings based on the new set of active monitors.</description></item>
/// <item><description>Aligning the monitor info cache and idle detection with the new topology without stopping the idle loop.</description></item>
/// <item><description>Ensuring the idle detection loop is running.</description></item>
/// </list>
/// </para>
/// </summary>
public class SynchronizeMonitorStateCommandHandler(
    IMonitorIdleDetectionService idleDetectionService,
    IMonitorSettingsFileService settingsFileService,
    IMonitorDimmingService dimmingService,
    IMonitorBlackoutService blackoutService,
    IMonitorInfoManager monitorInfoManager)
    : ICommandHandler<SynchronizeMonitorStateCommand>
{
    /// <summary>
    /// Handles the synchronization of monitor state by clearing overlays and brightness where needed,
    /// updating managed monitor settings, refreshing the monitor cache, and applying topology to idle detection.
    /// </summary>
    /// <param name="command">The command containing the old and new monitor lists.</param>
    public Task HandleAsync(SynchronizeMonitorStateCommand command)
    {
        RemoveOverlaysAndResetBrightness(command.OldMonitors);
        RemoveOverlaysAndResetBrightness(GetNewlyConnectedMonitors(command.NewMonitors, command.OldMonitors));

        var savedSettings = settingsFileService.LoadSettings();
        UpdateManagedSettings(savedSettings, command.NewMonitors);

        monitorInfoManager.UpdateCachedMonitorsFromSnapshot(command.NewMonitors);
        idleDetectionService.ApplyTopologyAndSettings(savedSettings, command.NewMonitors);
        idleDetectionService.Start();

        Log.Information("Monitor state synchronized. Active monitors: {Count}", command.NewMonitors.Count);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes blackout overlays and resets brightness for the specified monitors.
    /// </summary>
    /// <param name="monitors">The collection of monitors to process.</param>
    private void RemoveOverlaysAndResetBrightness(IEnumerable<MonitorInfo> monitors)
    {
        foreach (var monitor in monitors)
        {
            if (string.IsNullOrEmpty(monitor.HardwareId))
                continue;
            blackoutService.HideBlackoutOverlayAsync(monitor.HardwareId);
            dimmingService.UndimMonitorAsync(monitor.HardwareId);
        }
    }

    /// <summary>
    /// Gets the monitors that are newly connected (present in newMonitors but not in oldMonitors).
    /// </summary>
    /// <param name="newMonitors">The current list of monitors.</param>
    /// <param name="oldMonitors">The previous list of monitors.</param>
    /// <returns>A collection of monitors that are newly connected.</returns>
    private IEnumerable<MonitorInfo> GetNewlyConnectedMonitors(IReadOnlyList<MonitorInfo> newMonitors, IReadOnlyList<MonitorInfo> oldMonitors)
    {
        var oldIds = oldMonitors.Select(b => b.HardwareId);
        return newMonitors.ExceptBy(oldIds, a => a.HardwareId).ToList();
    }

    /// <summary>
    /// Updates the IsManaged property of each monitor setting based on whether the monitor is currently active.
    /// </summary>
    /// <param name="settings">The list of monitor settings to update.</param>
    /// <param name="activeMonitors">The list of currently active monitors.</param>
    private void UpdateManagedSettings(List<MonitorSettings> settings, IReadOnlyList<MonitorInfo> activeMonitors)
    {
        foreach (var setting in settings)
        {
            var isActiveMonitor = activeMonitors.Any(m => m.HardwareId == setting.HardwareId);
            setting.IsManaged = setting.IsManaged && isActiveMonitor;
        }
    }
}