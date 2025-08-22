using OLED_Sleeper.Commands;
using OLED_Sleeper.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace OLED_Sleeper.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly MonitorService _monitorService;
        private readonly MonitorLayoutService _monitorLayoutService;

        private double _containerWidth;
        private double _containerHeight;

        private MonitorViewModel? _selectedMonitor;
        public MonitorViewModel? SelectedMonitor
        {
            get => _selectedMonitor;
            set
            {
                if (_selectedMonitor != null) { _selectedMonitor.IsSelected = false; }
                _selectedMonitor = value;
                if (_selectedMonitor != null) { _selectedMonitor.IsSelected = true; }
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectionMessage));
                OnPropertyChanged(nameof(IsMonitorSelected));
            }
        }

        public string SelectionMessage => SelectedMonitor != null ? $"Monitor {SelectedMonitor.DisplayNumber} selected for configuration." : "Click a monitor above to configure its settings.";
        public bool IsMonitorSelected => SelectedMonitor != null;

        public ICommand ReloadMonitorsCommand { get; }
        public ICommand SelectMonitorCommand { get; }
        public ObservableCollection<MonitorViewModel> Monitors { get; } = new ObservableCollection<MonitorViewModel>();

        public MainViewModel()
        {
            _monitorService = new MonitorService();
            _monitorLayoutService = new MonitorLayoutService();
            ReloadMonitorsCommand = new RelayCommand(ExecuteReloadMonitors);
            SelectMonitorCommand = new RelayCommand(ExecuteSelectMonitor);
        }

        private void ExecuteSelectMonitor(object? parameter)
        {
            if (parameter is MonitorViewModel monitor)
            {
                SelectedMonitor = monitor;
            }
        }

        private void ExecuteReloadMonitors(object? parameter)
        {
            LoadMonitors(_containerWidth, _containerHeight);
        }

        public void LoadMonitors(double containerWidth, double containerHeight)
        {
            _containerWidth = containerWidth;
            _containerHeight = containerHeight;

            var monitorInfos = _monitorService.GetMonitors();
            var newMonitorViewModels = _monitorLayoutService.CreateLayout(monitorInfos, containerWidth, containerHeight);

            Monitors.Clear();
            foreach (var viewModel in newMonitorViewModels)
            {
                Monitors.Add(viewModel);
            }
            SelectedMonitor = null;
        }
    }
}