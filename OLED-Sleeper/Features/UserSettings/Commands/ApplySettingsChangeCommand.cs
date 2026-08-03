using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.Messaging.Interfaces;

namespace OLED_Sleeper.Features.UserSettings.Commands
{
    /// <summary>
    /// Represents a command to apply a changed set of monitor settings to idle detection
    /// and restore the monitors those settings cover.
    /// </summary>
    public class ApplySettingsChangeCommand : ICommand
    {
        /// <summary>
        /// The settings that changed. One restore is issued per entry, so this carries the settings the
        /// caller supplied rather than the full stored list.
        /// </summary>
        public required List<MonitorSettings> Settings { get; init; }
    }
}
