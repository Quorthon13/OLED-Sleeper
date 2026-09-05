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

        public WorkspaceService(
            IMonitorInfoManager monitorManager,
            IMonitorSettingsFileService settingsService,
            IMonitorLayoutService monitorLayoutService)
        {
            _monitorManager = monitorManager;
            _settingsService = settingsService;
            _monitorLayoutService = monitorLayoutService;
        }

        /// <inheritdoc />
        public async Task<ObservableCollection<MonitorLayoutViewModel>> BuildWorkspaceAsync(double containerWidth, double containerHeight)
        {
            var monitorInfos = await _monitorManager.GetCurrentMonitorsAsync();

            var savedSettings = _settingsService.LoadSettings();
            var monitorLayoutViewModels = _monitorLayoutService.CreateLayout(monitorInfos.ToList(), containerWidth, containerHeight);
            ApplySettingsToViewModels(monitorLayoutViewModels, savedSettings);

            return monitorLayoutViewModels;
        }

        /// <inheritdoc />
        public async Task<ObservableCollection<MonitorLayoutViewModel>> RefreshWorkspaceAsync(double containerWidth, double containerHeight)
        {
            await _monitorManager.RefreshMonitorsAsync();

            return await BuildWorkspaceAsync(containerWidth, containerHeight);
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