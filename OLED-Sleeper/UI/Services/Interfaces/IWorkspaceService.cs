using OLED_Sleeper.UI.ViewModels;
using System.Collections.ObjectModel;

namespace OLED_Sleeper.UI.Services.Interfaces
{
    /// <summary>
    /// Provides workspace management functionality, including monitor discovery, settings loading,
    /// and layout ViewModel construction for the main application UI.
    /// </summary>
    public interface IWorkspaceService
    {
        /// <summary>
        /// Builds the workspace by discovering monitors, loading settings, and constructing layout view models.
        /// </summary>
        /// <param name="containerWidth">The width of the container for layout scaling.</param>
        /// <param name="containerHeight">The height of the container for layout scaling.</param>
        /// <returns>The layout view models for the monitors that were found.</returns>
        Task<ObservableCollection<MonitorLayoutViewModel>> BuildWorkspaceAsync(double containerWidth, double containerHeight);

        /// <summary>
        /// Performs a full refresh of the workspace by re-scanning the monitor list and then rebuilding the workspace.
        /// </summary>
        /// <param name="containerWidth">The width of the container for layout scaling.</param>
        /// <param name="containerHeight">The height of the container for layout scaling.</param>
        /// <returns>The layout view models for the monitors that were found.</returns>
        Task<ObservableCollection<MonitorLayoutViewModel>> RefreshWorkspaceAsync(double containerWidth, double containerHeight);
    }
}