using OLED_Sleeper.Commands.Monitor.State;
using OLED_Sleeper.Models;
using OLED_Sleeper.Services.Monitor.Blackout.Interfaces;
using OLED_Sleeper.Services.Monitor.Dimming.Interfaces;
using OLED_Sleeper.Services.Monitor.IdleDetection.Interfaces;
using OLED_Sleeper.Services.Monitor.Settings.Interfaces;
using Serilog;

namespace OLED_Sleeper.Handlers.Monitor.State;

/// <summary>
/// Handles the <see cref="SynchronizeMonitorStateCommand"/> to reconcile the application's monitor state.
/// <para>
/// This handler is responsible for:
/// <list type="bullet">
/// <item><description>Stopping idle detection to reset state.</description></item>
/// <item><description>Removing overlays and resetting brightness for all old and newly connected monitors.</description></item>
/// <item><description>Updating managed monitor settings based on the new set of active monitors.</description></item>
/// <item><description>Restarting idle detection with the updated settings.</description></item>
/// </list>
/// </para>
/// </summary>
public class SynchronizeMonitorStateCommandHandler(
    IMonitorIdleDetectionService idleDetectionService,
    IMonitorSettingsFileService settingsFileService,
    IMonitorDimmingService dimmingService,
    IMonitorBlackoutService blackoutService)
    : ICommandHandler<SynchronizeMonitorStateCommand>
{
    /// <summary>
    /// Handles the synchronization of monitor state by stopping idle detection, clearing overlays and brightness,
    /// updating managed monitor settings, and restarting idle detection.
    /// </summary>
    /// <param name="command">The command containing the old and new monitor lists.</param>
    public Task HandleAsync(SynchronizeMonitorStateCommand command)
    {
        idleDetectionService.Stop();

        RemoveOverlaysAndResetBrightness(command.OldMonitors);
        RemoveOverlaysAndResetBrightness(GetNewlyConnectedMonitors(command.NewMonitors, command.OldMonitors));

        var savedSettings = settingsFileService.LoadSettings();
        UpdateManagedSettings(savedSettings, command.NewMonitors);

        idleDetectionService.UpdateSettings(savedSettings);
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