// File: ViewModels/MainViewModel.cs
using OLED_Sleeper.Commands;
using OLED_Sleeper.Models;
using OLED_Sleeper.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace OLED_Sleeper.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IMonitorService _monitorService;
        private readonly IMonitorLayoutService _monitorLayoutService;
        private readonly ISettingsService _settingsService;
        private readonly IIdleActivityService _idleActivityService;

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
            set
            {
                if (_isDirty == value) return;
                _isDirty = value;
                OnPropertyChanged();
                // Update window title when dirty state changes
                WindowTitle = "OLED Sleeper Settings" + (_isDirty ? "*" : "");
            }
        }

        private string _windowTitle = "OLED Sleeper Settings";

        public string WindowTitle
        {
            get => _windowTitle;
            set { _windowTitle = value; OnPropertyChanged(); }
        }

        private string _saveButtonText = "Save Settings";

        public string SaveButtonText
        {
            get => _saveButtonText;
            set { _saveButtonText = value; OnPropertyChanged(); }
        }

        public ICommand ReloadMonitorsCommand { get; }
        public ICommand SelectMonitorCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand DiscardChangesCommand { get; }

        public ObservableCollection<MonitorViewModel> Monitors { get; } = new ObservableCollection<MonitorViewModel>();

        public MainViewModel(IMonitorService monitorService, IMonitorLayoutService monitorLayoutService, ISettingsService settingsService, IIdleActivityService idleActivityService)
        {
            _monitorService = monitorService;
            _monitorLayoutService = monitorLayoutService;
            _settingsService = settingsService;
            _idleActivityService = idleActivityService; // Store service

            SelectMonitorCommand = new RelayCommand(ExecuteSelectMonitor);
            ReloadMonitorsCommand = new RelayCommand(RefreshMonitors);
            SaveSettingsCommand = new AsyncRelayCommand(ExecuteSaveSettings, () => IsDirty);
            DiscardChangesCommand = new RelayCommand(ExecuteDiscardChanges, () => IsDirty);
        }

        private void ExecuteSelectMonitor(object? parameter)
        {
            if (parameter is MonitorViewModel monitor)
            {
                SelectedMonitor = monitor;
            }
        }

        private async Task ExecuteSaveSettings()
        {
            var invalidMonitors = Monitors
                .Where(m => m.Configuration.IsManaged && !m.Configuration.IsValid)
                .ToList();

            if (invalidMonitors.Any())
            {
                var errorBuilder = new StringBuilder();
                errorBuilder.AppendLine("Cannot save due to invalid settings on the following monitors:");
                foreach (var monitor in invalidMonitors)
                {
                    errorBuilder.AppendLine($" - {monitor.MonitorTitle}");
                }
                errorBuilder.AppendLine("\nPlease correct the highlighted errors before saving.");
                MessageBox.Show(errorBuilder.ToString(), "Invalid Settings", MessageBoxButton.OK, MessageBoxImage.Error);
                return; // Stop the save operation
            }

            var allSettings = Monitors.Select(m => m.Configuration.ToSettings()).ToList();
            _settingsService.SaveSettings(allSettings);

            // Notify the idle service about the new settings
            _idleActivityService.UpdateSettings(allSettings);

            foreach (var monitorVM in Monitors)
            {
                monitorVM.Configuration.MarkAsSaved();
            }

            CheckDirtyState();

            SaveButtonText = "Saved!";
            await Task.Delay(2000);
            SaveButtonText = "Save Settings";
        }

        private void ExecuteDiscardChanges()
        {
            RefreshMonitors();
        }

        public void RefreshMonitors()
        {
            RecalculateLayout(_containerWidth, _containerHeight, preserveSelection: false);
        }

        public void RecalculateLayout(double width, double height, bool preserveSelection = true)
        {
            UpdateMonitorsInternal(width, height, preserveSelection);
        }

        private void UpdateMonitorsInternal(double width, double height, bool preserveSelection)
        {
            if (width <= 0 || height <= 0) return;

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
                // Subscribe to each monitor's dirty state change
                viewModel.OnMonitorDirtyStateChanged = CheckDirtyState;
                Monitors.Add(viewModel);
            }

            SelectedMonitor = selectedMonitorId != null
                ? Monitors.FirstOrDefault(m => m.HardwareId == selectedMonitorId)
                : null;

            CheckDirtyState();
        }

        private void CheckDirtyState()
        {
            // The master dirty state is true if any monitor has unsaved changes.
            IsDirty = Monitors.Any(m => m.IsDirty);
        }
    }
}