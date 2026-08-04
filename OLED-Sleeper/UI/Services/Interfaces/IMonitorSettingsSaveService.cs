using OLED_Sleeper.UI.ViewModels;

namespace OLED_Sleeper.UI.Services.Interfaces
{
    /// <summary>
    /// Validates the configured monitors and writes them to the settings file.
    /// </summary>
    public interface IMonitorSettingsSaveService
    {
        /// <summary>
        /// Validates the supplied monitors and saves them. An invalid configuration is reported to the
        /// user and nothing is written. Saved monitors are marked as no longer holding unsaved changes.
        /// </summary>
        /// <param name="monitors">The monitors whose configuration should be saved.</param>
        /// <returns>True when the settings were written; false when validation rejected them.</returns>
        bool TrySave(IReadOnlyList<MonitorLayoutViewModel> monitors);
    }
}
