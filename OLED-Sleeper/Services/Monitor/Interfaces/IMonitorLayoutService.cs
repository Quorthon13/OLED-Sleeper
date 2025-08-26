using System.Collections.ObjectModel;
using OLED_Sleeper.Models;
using OLED_Sleeper.ViewModels;

namespace OLED_Sleeper.Services.Monitor.Interfaces
{
    /// <summary>
    /// Defines the contract for creating monitor layout view models for the UI.
    /// </summary>
    public interface IMonitorLayoutService
    {
        /// <summary>
        /// Creates a collection of <see cref="MonitorLayoutViewModel"/> objects for the given monitors and container size.
        /// </summary>
        /// <param name="monitorInfos">The list of monitor information models.</param>
        /// <param name="containerWidth">The width of the container for layout.</param>
        /// <param name="containerHeight">The height of the container for layout.</param>
        /// <returns>An observable collection of monitor layout view models.</returns>
        ObservableCollection<MonitorLayoutViewModel> CreateLayout(List<MonitorInfo> monitorInfos, double containerWidth, double containerHeight);
    }
}