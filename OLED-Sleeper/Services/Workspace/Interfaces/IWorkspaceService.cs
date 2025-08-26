using OLED_Sleeper.ViewModels;
using System.Collections.ObjectModel;

namespace OLED_Sleeper.Services.Workspace.Interfaces
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
        /// <returns>An observable collection of <see cref="MonitorLayoutViewModel"/> for UI binding.</returns>
        ObservableCollection<MonitorLayoutViewModel> BuildWorkspace(double containerWidth, double containerHeight);
    }
}