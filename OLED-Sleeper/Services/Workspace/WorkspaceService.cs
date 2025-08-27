using OLED_Sleeper.Services.Monitor.Interfaces;
using OLED_Sleeper.Services.Workspace.Interfaces;
using OLED_Sleeper.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace OLED_Sleeper.Services.Workspace
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

        /// <summary>
        /// Builds the workspace asynchronously.
        /// </summary>
        /// <param name="containerWidth">The width of the container.</param>
        /// <param name="containerHeight">The height of the container.</param>
        public void BuildWorkspaceAsync(double containerWidth, double containerHeight)
        {
            void Handler(object sender, System.Collections.Generic.IReadOnlyList<Models.MonitorInfo> monitorInfos)
            {
                _monitorManager.MonitorListReady -= Handler;
                var savedSettings = _settingsService.LoadSettings();
                var monitorLayoutViewModels = _monitorLayoutService.CreateLayout(monitorInfos.ToList(), containerWidth, containerHeight);
                ApplySettingsToViewModels(monitorLayoutViewModels, savedSettings);
                WorkspaceReady?.Invoke(this, monitorLayoutViewModels);
            }
            _monitorManager.MonitorListReady += Handler;
            _monitorManager.GetCurrentMonitorsAsync();
        }

        /// <summary>
        /// Begins a full refresh of the workspace asynchronously by refreshing the monitor list and then rebuilding the workspace.
        /// </summary>
        /// <param name="containerWidth">The width of the container for layout scaling.</param>
        /// <param name="containerHeight">The height of the container for layout scaling.</param>
        public void RefreshWorkspaceAsync(double containerWidth, double containerHeight)
        {
            void Handler(object sender, System.Collections.Generic.IReadOnlyList<Models.MonitorInfo> monitorInfos)
            {
                _monitorManager.MonitorListReady -= Handler;
                BuildWorkspaceAsync(containerWidth, containerHeight);
            }
            _monitorManager.MonitorListReady += Handler;
            _monitorManager.RefreshMonitorsAsync();
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