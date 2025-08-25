// File: Services/IWorkspaceService.cs
using OLED_Sleeper.ViewModels;
using System.Collections.ObjectModel;

namespace OLED_Sleeper.Services.Workspace.Interfaces
{
    public interface IWorkspaceService
    {
        ObservableCollection<MonitorLayoutViewModel> BuildWorkspace(double containerWidth, double containerHeight);
    }
}