using System.Collections.ObjectModel;
using OLED_Sleeper.ViewModels;

namespace OLED_Sleeper.Services.UI.Interfaces
{
    /// <summary>
    /// Provides workspace management functionality, including monitor discovery, settings loading,
    /// and layout ViewModel construction for the main application UI.
    /// </summary>
    public interface IWorkspaceService
    {
        /// <summary>
        /// Raised when the workspace has finished building and monitor layout view models are ready.
        /// </summary>
        event EventHandler<ObservableCollection<MonitorLayoutViewModel>> WorkspaceReady;

        /// <summary>
        /// Begins building the workspace asynchronously by discovering monitors, loading settings, and constructing layout view models.
        /// </summary>
        /// <param name="containerWidth">The width of the container for layout scaling.</param>
        /// <param name="containerHeight">The height of the container for layout scaling.</param>
        void BuildWorkspaceAsync(double containerWidth, double containerHeight);

        /// <summary>
        /// Begins a full refresh of the workspace asynchronously by refreshing the monitor list and then rebuilding the workspace.
        /// </summary>
        /// <param name="containerWidth">The width of the container for layout scaling.</param>
        /// <param name="containerHeight">The height of the container for layout scaling.</param>
        void RefreshWorkspaceAsync(double containerWidth, double containerHeight);
    }
}