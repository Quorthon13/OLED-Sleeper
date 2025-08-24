// File: Services/ISaveValidationService.cs
using OLED_Sleeper.ViewModels;
using System.Collections.Generic;

namespace OLED_Sleeper.Services
{
    public interface ISaveValidationService
    {
        /// <summary>
        /// Validates the monitors and notifies the user if any are invalid.
        /// </summary>
        /// <param name="monitors">The collection of monitors to validate.</param>
        /// <returns>True if all monitors are valid, otherwise false.</returns>
        bool ValidateAndNotify(IEnumerable<MonitorLayoutViewModel> monitors);
    }
}