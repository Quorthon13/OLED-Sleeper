// File: ViewModels/MainViewModel.cs
using OLED_Sleeper.Commands;
using OLED_Sleeper.Models;
using OLED_Sleeper.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace OLED_Sleeper.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IMonitorService _monitorService;
        private readonly IMonitorLayoutService _monitorLayoutService;
        private readonly ISettingsService _settingsService;

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
                OnPropertyChanged(nameof(IsMonitorSelected));
            }
        }

        public bool IsMonitorSelected => SelectedMonitor != null;

        private bool _isDirty;

        public bool IsDirty
        {
            get => _isDirty;
            set { _isDirty = value; OnPropertyChanged(); }
        }

        public ICommand ReloadMonitorsCommand { get; }
        public ICommand SelectMonitorCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand DiscardChangesCommand { get; }

        public ObservableCollection<MonitorViewModel> Monitors { get; } = new ObservableCollection<MonitorViewModel>();

        public MainViewModel(IMonitorService monitorService, IMonitorLayoutService monitorLayoutService, ISettingsService settingsService)
        {
            _monitorService = monitorService;
            _monitorLayoutService = monitorLayoutService;
            _settingsService = settingsService;

            SelectMonitorCommand = new RelayCommand(ExecuteSelectMonitor);
            ReloadMonitorsCommand = new RelayCommand(RefreshMonitors); // Changed to call the new method
            SaveSettingsCommand = new RelayCommand(() => ExecuteSaveSettings(), () => IsDirty);
            DiscardChangesCommand = new RelayCommand(() => ExecuteDiscardChanges(), () => IsDirty);
        }

        private void ExecuteSelectMonitor(object? parameter)
        {
            if (parameter is MonitorViewModel monitor)
            {
                SelectedMonitor = monitor;
            }
        }

        private void ExecuteSaveSettings()
        {
            var allSettings = Monitors.Select(m => m.Configuration.ToSettings()).ToList();
            _settingsService.SaveSettings(allSettings);
            foreach (var monitor in Monitors)
            {
                monitor.Configuration.MarkAsSaved();
            }
            CheckDirtyState();
        }

        private void ExecuteDiscardChanges()
        {
            // A discard should behave like a full refresh, clearing selection.
            RefreshMonitors();
        }

        // --- Start of Refactoring ---

        /// <summary>
        /// Public method for the Reload button. Always clears selection and reloads from scratch.
        /// </summary>
        public void RefreshMonitors()
        {
            UpdateMonitorsInternal(_containerWidth, _containerHeight, preserveSelection: false);
        }

        /// <summary>
        /// Public method for the View's SizeChanged event. Preserves the current selection.
        /// </summary>
        public void RecalculateLayout(double width, double height)
        {
            UpdateMonitorsInternal(width, height, preserveSelection: true);
        }

        /// <summary>
        /// Private worker method that contains the core logic for updating the monitor list and layout.
        /// </summary>
        private void UpdateMonitorsInternal(double width, double height, bool preserveSelection)
        {
            if (width <= 0 || height <= 0) return;

            // Store dimensions for future use (like a manual refresh)
            _containerWidth = width;
            _containerHeight = height;

            var selectedMonitorId = preserveSelection ? SelectedMonitor?.HardwareId : null;

            var monitorInfos = _monitorService.GetMonitors();
            var savedSettings = _settingsService.LoadSettings();
            var newMonitorViewModels = _monitorLayoutService.CreateLayout(monitorInfos, width, height);

            Monitors.Clear();
            foreach (var viewModel in newMonitorViewModels)
            {
                var setting = savedSettings.FirstOrDefault(s => s.HardwareId == viewModel.HardwareId);
                if (setting != null)
                {
                    viewModel.Configuration.ApplySettings(setting);
                }
                viewModel.Configuration.OnDirtyStateChanged = CheckDirtyState;
                Monitors.Add(viewModel);
            }

            // Re-selection logic is now driven by the preserveSelection flag
            SelectedMonitor = selectedMonitorId != null
                ? Monitors.FirstOrDefault(m => m.HardwareId == selectedMonitorId)
                : null;

            CheckDirtyState();
        }

        // --- End of Refactoring ---

        private void CheckDirtyState()
        {
            IsDirty = Monitors.Any(m => m.Configuration.IsDirty);
        }
    }
}