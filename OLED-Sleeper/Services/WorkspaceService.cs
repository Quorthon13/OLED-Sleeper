using OLED_Sleeper.ViewModels;
using System.Collections.ObjectModel;

namespace OLED_Sleeper.Services
{
    public class WorkspaceService : IWorkspaceService
    {
        private readonly IMonitorService _monitorService;
        private readonly ISettingsService _settingsService;
        private readonly IMonitorLayoutService _monitorLayoutService;

        public WorkspaceService(IMonitorService monitorService, ISettingsService settingsService, IMonitorLayoutService monitorLayoutService)
        {
            _monitorService = monitorService;
            _settingsService = settingsService;
            _monitorLayoutService = monitorLayoutService;
        }

        public ObservableCollection<MonitorLayoutViewModel> BuildWorkspace(double containerWidth, double containerHeight)
        {
            var monitorInfos = _monitorService.GetMonitors();
            var savedSettings = _settingsService.LoadSettings();

            var monitorLayoutViewModels = _monitorLayoutService.CreateLayout(monitorInfos, containerWidth, containerHeight);

            foreach (var viewModel in monitorLayoutViewModels)
            {
                var setting = savedSettings.FirstOrDefault(s => s.HardwareId == viewModel.HardwareId);
                if (setting != null)
                {
                    viewModel.Configuration.ApplySettings(setting);
                }
            }

            return monitorLayoutViewModels;
        }
    }
}