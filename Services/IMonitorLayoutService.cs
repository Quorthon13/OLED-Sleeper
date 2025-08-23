using OLED_Sleeper.Models;
using OLED_Sleeper.ViewModels;
using System.Collections.ObjectModel;

namespace OLED_Sleeper.Services
{
    // Refactored: Interface for MonitorLayoutService to support dependency injection.
    public interface IMonitorLayoutService
    {
        ObservableCollection<MonitorViewModel> CreateLayout(List<MonitorInfo> monitorInfos, double containerWidth, double containerHeight);
    }
}