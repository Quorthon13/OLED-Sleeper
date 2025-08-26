using System.Collections.Generic;
using OLED_Sleeper.ViewModels;

namespace OLED_Sleeper.Services.Monitor.Interfaces
{
    /// <summary>
    /// Defines the contract for validating monitor settings and notifying the user of validation results.
    /// </summary>
    public interface IMonitorSettingsValidationService
    {
        /// <summary>
        /// Validates the provided monitor settings and notifies the user of any validation errors.
        /// </summary>
        /// <param name="monitors">The collection of monitor layout view models to validate.</param>
        /// <returns>True if all monitors are valid; otherwise, false.</returns>
        bool ValidateAndNotify(IEnumerable<MonitorLayoutViewModel> monitors);
    }
}