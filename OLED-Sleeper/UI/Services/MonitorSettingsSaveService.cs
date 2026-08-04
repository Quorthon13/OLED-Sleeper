using OLED_Sleeper.Features.UserSettings.Services.Interfaces;
using OLED_Sleeper.UI.Helpers;
using OLED_Sleeper.UI.Services.Interfaces;
using OLED_Sleeper.UI.ViewModels;

namespace OLED_Sleeper.UI.Services
{
    /// <summary>
    /// Saves monitor settings through <see cref="IMonitorSettingsFileService"/>, reporting an invalid
    /// configuration with a modal dialog rather than writing it.
    /// </summary>
    public class MonitorSettingsSaveService : IMonitorSettingsSaveService
    {
        private readonly IMonitorSettingsFileService _settingsService;
        private readonly IDialogService _dialogService;

        public MonitorSettingsSaveService(
            IMonitorSettingsFileService settingsService,
            IDialogService dialogService)
        {
            _settingsService = settingsService;
            _dialogService = dialogService;
        }

        /// <inheritdoc />
        public bool TrySave(IReadOnlyList<MonitorLayoutViewModel> monitors)
        {
            var error = MonitorSettingsValidator.BuildValidationError(monitors);
            if (error != null)
            {
                _dialogService.ShowError(error, "Monitor Configuration Error");
                return false;
            }

            _settingsService.SaveSettings(monitors.Select(m => m.Configuration.ToSettings()).ToList());

            foreach (var monitor in monitors)
            {
                monitor.Configuration.MarkAsSaved();
            }

            return true;
        }
    }
}
