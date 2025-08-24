using OLED_Sleeper.ViewModels;
using System.Collections.ObjectModel;

namespace OLED_Sleeper.Services
{
    /// <summary>
    /// Provides workspace management, including monitor discovery, settings loading,
    /// and layout ViewModel construction for the main application UI.
    /// </summary>
    public class WorkspaceService : IWorkspaceService
    {
        private readonly IMonitorService _monitorService;
        private readonly ISettingsService _settingsService;
        private readonly IMonitorLayoutService _monitorLayoutService;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkspaceService"/> class.
        /// </summary>
        /// <param name="monitorService">Service for monitor enumeration.</param>
        /// <param name="settingsService">Service for loading and saving monitor settings.</param>
        /// <param name="monitorLayoutService">Service for creating monitor layout view models.</param>
        public WorkspaceService(
            IMonitorService monitorService,
            ISettingsService settingsService,
            IMonitorLayoutService monitorLayoutService)
        {
            _monitorService = monitorService;
            _settingsService = settingsService;
            _monitorLayoutService = monitorLayoutService;
        }

        /// <summary>
        /// Builds the workspace by discovering monitors, loading settings, and constructing layout view models.
        /// </summary>
        /// <param name="containerWidth">The width of the container for layout scaling.</param>
        /// <param name="containerHeight">The height of the container for layout scaling.</param>
        /// <returns>An observable collection of <see cref="MonitorLayoutViewModel"/> for UI binding.</returns>
        public ObservableCollection<MonitorLayoutViewModel> BuildWorkspace(double containerWidth, double containerHeight)
        {
            var monitorInfos = _monitorService.GetMonitors();
            var savedSettings = _settingsService.LoadSettings();
            var monitorLayoutViewModels = _monitorLayoutService.CreateLayout(monitorInfos, containerWidth, containerHeight);

            foreach (var viewModel in monitorLayoutViewModels)
            {
                // Apply saved settings to each monitor if available
                var setting = savedSettings.FirstOrDefault(s => s.HardwareId == viewModel.HardwareId);
                if (setting != null)
                {
                    viewModel.Configuration.ApplySettings(setting);
                    // Establish the loaded settings as the new "saved" state for dirty tracking
                    viewModel.Configuration.MarkAsSaved();
                }
            }

            return monitorLayoutViewModels;
        }
    }
}