using OLED_Sleeper.UI.ViewModels;
using System.Text;
using System.Windows;

namespace OLED_Sleeper.UI.Helpers
{
    /// <summary>
    /// Provides validation logic for monitor settings and displays errors to the user.
    /// This is a static helper class and does not hold any state.
    /// </summary>
    public static class MonitorSettingsValidator
    {
        /// <summary>
        /// Validates the provided monitor settings and displays a message box if there are invalid monitors.
        /// </summary>
        /// <param name="monitors">The collection of monitor layout view models to validate.</param>
        /// <returns>True if all monitors are valid; otherwise, false.</returns>
        public static bool ValidateAndNotify(IEnumerable<MonitorLayoutViewModel> monitors)
        {
            var invalidMonitors = GetInvalidMonitors(monitors);

            if (invalidMonitors.Any())
            {
                ShowValidationError(invalidMonitors);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns a list of monitors that are managed but have invalid configuration.
        /// </summary>
        /// <param name="monitors">The collection of monitor layout view models to check.</param>
        /// <returns>A list of invalid monitor view models.</returns>
        private static List<MonitorLayoutViewModel> GetInvalidMonitors(IEnumerable<MonitorLayoutViewModel> monitors)
        {
            return monitors
                .Where(m => m.Configuration.IsManaged && !m.Configuration.IsValid)
                .ToList();
        }

        /// <summary>
        /// Displays a validation error message for the provided invalid monitors.
        /// </summary>
        /// <param name="invalidMonitors">The list of invalid monitor view models.</param>
        private static void ShowValidationError(List<MonitorLayoutViewModel> invalidMonitors)
        {
            var errorBuilder = new StringBuilder();
            errorBuilder.AppendLine("Cannot save due to invalid settings on the following monitors:");
            foreach (var monitor in invalidMonitors)
            {
                errorBuilder.AppendLine($" - {monitor.MonitorTitle}");
            }
            errorBuilder.AppendLine("\nPlease correct the required fields before saving.");
            MessageBox.Show(errorBuilder.ToString(), "Invalid Settings", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}