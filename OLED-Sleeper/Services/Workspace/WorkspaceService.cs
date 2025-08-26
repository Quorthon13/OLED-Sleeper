using OLED_Sleeper.Services.Monitor.Interfaces;
using OLED_Sleeper.Services.Workspace.Interfaces;
using OLED_Sleeper.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;

namespace OLED_Sleeper.Services.Workspace
{
    /// <inheritdoc/>
    public class WorkspaceService : IWorkspaceService
    {
        private readonly IMonitorInfoManager _monitorManager;
        private readonly IMonitorSettingsFileService _settingsService;
        private readonly IMonitorLayoutService _monitorLayoutService;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkspaceService"/> class.
        /// </summary>
        /// <param name="monitorManager">Service for monitor enumeration.</param>
        /// <param name="settingsService">Service for loading and saving monitor settings.</param>
        /// <param name="monitorLayoutService">Service for creating monitor layout view models.</param>
        public WorkspaceService(
            IMonitorInfoManager monitorManager,
            IMonitorSettingsFileService settingsService,
            IMonitorLayoutService monitorLayoutService)
        {
            _monitorManager = monitorManager;
            _settingsService = settingsService;
            _monitorLayoutService = monitorLayoutService;
        }

        /// <inheritdoc/>
        public ObservableCollection<MonitorLayoutViewModel> BuildWorkspace(double containerWidth, double containerHeight)
        {
            var monitorInfos = _monitorManager.GetCurrentMonitors();
            var savedSettings = _settingsService.LoadSettings();
            var monitorLayoutViewModels = _monitorLayoutService.CreateLayout(monitorInfos, containerWidth, containerHeight);

            ApplySettingsToViewModels(monitorLayoutViewModels, savedSettings);

            return monitorLayoutViewModels;
        }

        /// <summary>
        /// Applies saved settings to the corresponding monitor layout view models.
        /// </summary>
        /// <param name="viewModels">The collection of monitor layout view models.</param>
        /// <param name="savedSettings">The list of saved monitor settings.</param>
        private static void ApplySettingsToViewModels(ObservableCollection<MonitorLayoutViewModel> viewModels, System.Collections.Generic.List<Models.MonitorSettings> savedSettings)
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