using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.MonitorInformation.Services.Interfaces;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Features.UserSettings.Services.Interfaces;
using OLED_Sleeper.UI.Services.Interfaces;
using OLED_Sleeper.UI.ViewModels;
using System.Collections.ObjectModel;

namespace OLED_Sleeper.UI.Services
{
    /// <summary>
    /// Provides workspace management functionality, including monitor discovery, settings loading,
    /// and layout ViewModel construction for the main application UI.
    /// </summary>
    public class WorkspaceService : IWorkspaceService
    {
        private readonly IMonitorInfoManager _monitorManager;
        private readonly IMonitorSettingsFileService _settingsService;
        private readonly IMonitorLayoutService _monitorLayoutService;

        public event EventHandler<ObservableCollection<MonitorLayoutViewModel>> WorkspaceReady;

        public WorkspaceService(
            IMonitorInfoManager monitorManager,
            IMonitorSettingsFileService settingsService,
            IMonitorLayoutService monitorLayoutService)
        {
            _monitorManager = monitorManager;
            _settingsService = settingsService;
            _monitorLayoutService = monitorLayoutService;
        }

        /// <summary>
        /// Builds the workspace asynchronously.
        /// </summary>
        /// <param name="containerWidth">The width of the container.</param>
        /// <param name="containerHeight">The height of the container.</param>
        public async Task BuildWorkspaceAsync(double containerWidth, double containerHeight)
        {
            var monitorInfos = await _monitorManager.GetCurrentMonitorsAsync();

            var savedSettings = _settingsService.LoadSettings();
            var monitorLayoutViewModels = _monitorLayoutService.CreateLayout(monitorInfos.ToList(), containerWidth, containerHeight);
            ApplySettingsToViewModels(monitorLayoutViewModels, savedSettings);
            WorkspaceReady?.Invoke(this, monitorLayoutViewModels);
        }

        /// <summary>
        /// Performs a full refresh of the workspace by re-scanning the monitor list and then rebuilding the workspace.
        /// </summary>
        /// <param name="containerWidth">The width of the container for layout scaling.</param>
        /// <param name="containerHeight">The height of the container for layout scaling.</param>
        public async Task RefreshWorkspaceAsync(double containerWidth, double containerHeight)
        {
            await _monitorManager.RefreshMonitorsAsync();
            await BuildWorkspaceAsync(containerWidth, containerHeight);
        }

        /// <summary>
        /// Applies saved settings to the corresponding monitor layout view models.
        /// </summary>
        /// <param name="viewModels">The collection of monitor layout view models.</param>
        /// <param name="savedSettings">The list of saved monitor settings.</param>
        private static void ApplySettingsToViewModels(ObservableCollection<MonitorLayoutViewModel> viewModels, System.Collections.Generic.List<MonitorSettings> savedSettings)
        {
            foreach (var viewModel in viewModels)
            {
                var setting = savedSettings.FirstOrDefault(s => s.HardwareId == viewModel.HardwareId);
                if (setting != null)
                {
                    viewModel.Configuration.ApplySettings(setting);
                    viewModel.Configuration.MarkAsSaved();
                }
            }
        }
    }
}