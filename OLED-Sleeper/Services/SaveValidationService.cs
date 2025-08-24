// File: Services/SaveValidationService.cs
using OLED_Sleeper.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace OLED_Sleeper.Services
{
    public class SaveValidationService : ISaveValidationService
    {
        public bool ValidateAndNotify(IEnumerable<MonitorLayoutViewModel> monitors)
        {
            var invalidMonitors = monitors
                .Where(m => m.Configuration.IsManaged && !m.Configuration.IsValid)
                .ToList();

            if (invalidMonitors.Any())
            {
                var errorBuilder = new StringBuilder();
                errorBuilder.AppendLine("Cannot save due to invalid settings on the following monitors:");
                foreach (var monitor in invalidMonitors)
                {
                    errorBuilder.AppendLine($" - {monitor.MonitorTitle}");
                }
                errorBuilder.AppendLine("\nPlease correct the required fields before saving.");
                MessageBox.Show(errorBuilder.ToString(), "Invalid Settings", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }
    }
}