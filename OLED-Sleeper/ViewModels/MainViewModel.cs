using OLED_Sleeper.Commands;
using OLED_Sleeper.Events;
using OLED_Sleeper.Services.Monitor.Interfaces;
using OLED_Sleeper.Services.Workspace.Interfaces;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace OLED_Sleeper.ViewModels
{
    /// <summary>
    /// The main ViewModel for the application's main window.
    /// It orchestrates the various services and manages the overall state of the UI.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        #region Private Fields

        /// <summary>
        /// Service for building and managing the monitor workspace layout.
        /// </summary>
        private readonly IWorkspaceService _workspaceService;

        /// <summary>
        /// Service for loading and saving monitor settings.
        /// </summary>
        private readonly IMonitorSettingsFileService _settingsService;

        /// <summary>
        /// Service for handling idle activity and updating monitor states.
        /// </summary>
        private readonly IMonitorIdleDetectionService _idleActivityService;

        /// <summary>
        /// Service for validating monitor settings before saving.
        /// </summary>
        private readonly IMonitorSettingsValidationService _saveValidationService;

        /// <summary>
        /// Service for managing and refreshing monitor information from the system.
        /// </summary>
        private readonly IMonitorInfoManager _monitorInfoManager;

        /// <summary>
        /// The width of the container used for monitor layout calculations.
        /// </summary>
        private double _containerWidth;

        /// <summary>
        /// The height of the container used for monitor layout calculations.
        /// </summary>
        private double _containerHeight;

        /// <summary>
        /// The currently selected monitor in the UI.
        /// </summary>
        private MonitorLayoutViewModel? _selectedMonitor;

        /// <summary>
        /// Indicates whether any monitor settings have unsaved changes.
        /// </summary>
        private bool _isDirty;

        /// <summary>
        /// The text displayed in the main window's title bar.
        /// </summary>
        private string _windowTitle = "OLED Sleeper Settings";

        /// <summary>
        /// The text displayed on the save button.
        /// </summary>
        private string _saveButtonText = "Save Settings";

        #endregion Private Fields

        #region Public Properties

        /// <summary>
        /// Gets or sets the currently selected monitor in the layout view. Updates selection state and notifies property changes.
        /// </summary>
        public MonitorLayoutViewModel? SelectedMonitor
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

        /// <summary>
        /// Returns true if a monitor is currently selected in the UI.
        /// </summary>
        public bool IsMonitorSelected => SelectedMonitor != null;

        /// <summary>
        /// Gets or sets a value indicating whether any monitor settings have been changed and not saved.
        /// Updates the window title to reflect unsaved changes.
        /// </summary>
        public bool IsDirty
        {
            get => _isDirty;
            set
            {
                if (_isDirty == value) return;
                _isDirty = value;
                OnPropertyChanged();
                WindowTitle = "OLED Sleeper Settings" + (_isDirty ? "*" : "");
            }
        }

        /// <summary>
        /// Gets or sets the text for the main window's title bar. Includes a '*' when changes are unsaved.
        /// </summary>
        public string WindowTitle
        {
            get => _windowTitle;
            set { _windowTitle = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets the text for the save button (e.g., "Save Settings" or "Saved!").
        /// </summary>
        public string SaveButtonText
        {
            get => _saveButtonText;
            set { _saveButtonText = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// The collection of monitor view models to be displayed in the layout view.
        /// </summary>
        public ObservableCollection<MonitorLayoutViewModel> Monitors { get; } = new ObservableCollection<MonitorLayoutViewModel>();

        #endregion Public Properties

        #region Commands

        /// <summary>
        /// Command to refresh the list of monitors from the system and update the UI.
        /// </summary>
        public ICommand ReloadMonitorsCommand { get; }

        /// <summary>
        /// Command to select a specific monitor from the layout view.
        /// </summary>
        public ICommand SelectMonitorCommand { get; }

        /// <summary>
        /// Command to validate and save all current monitor settings.
        /// </summary>
        public ICommand SaveSettingsCommand { get; }

        /// <summary>
        /// Command to discard any unsaved changes and reload monitor settings.
        /// </summary>
        public ICommand DiscardChangesCommand { get; }

        #endregion Commands

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MainViewModel class, wiring up services and commands.
        /// </summary>
        /// <param name="workspaceService">Service for monitor workspace management.</param>
        /// <param name="settingsService">Service for settings persistence.</param>
        /// <param name="monitorIdleDetectionService">Service for idle activity monitoring.</param>
        /// <param name="monitorSettingsValidationService">Service for validating settings before save.</param>
        /// <param name="monitorInfoManager">Service for refreshing monitor information from the system.</param>
        public MainViewModel(IWorkspaceService workspaceService, IMonitorSettingsFileService settingsService,
                             IMonitorIdleDetectionService monitorIdleDetectionService, IMonitorSettingsValidationService monitorSettingsValidationService,
                             IMonitorInfoManager monitorInfoManager)
        {
            _workspaceService = workspaceService;
            _settingsService = settingsService;
            _idleActivityService = monitorIdleDetectionService;
            _saveValidationService = monitorSettingsValidationService;
            _monitorInfoManager = monitorInfoManager;

            SelectMonitorCommand = new RelayCommand(ExecuteSelectMonitor);
            ReloadMonitorsCommand = new RelayCommand(RefreshMonitors);
            SaveSettingsCommand = new AsyncRelayCommand(ExecuteSaveSettings, () => IsDirty);
            DiscardChangesCommand = new RelayCommand(ExecuteDiscardChanges, () => IsDirty);
        }

        #endregion Constructor

        #region Public Methods (for View Interaction)

        /// <summary>
        /// Initiates a full refresh of the monitor list, clearing any current selection and reloading from the workspace service.
        /// </summary>
        public void RefreshMonitors()
        {
            _monitorInfoManager.RefreshMonitors();
            UpdateMonitorsInternal(_containerWidth, _containerHeight, preserveSelection: false);
        }

        /// <summary>
        /// Recalculates the monitor layout based on a new container size, preserving the current selection if possible.
        /// </summary>
        /// <param name="width">The new width of the container.</param>
        /// <param name="height">The new height of the container.</param>
        public void RecalculateLayout(double width, double height)
        {
            UpdateMonitorsInternal(width, height, preserveSelection: true);
        }

        /// <summary>
        /// Handles logic for when the main window is closing. Returns true if the window should close, false to cancel.
        /// </summary>
        /// <returns>True to allow closing, false to cancel.</returns>
        public bool OnWindowClosing()
        {
            if (IsDirty)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Would you like to save them before hiding the window?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Cancel)
                {
                    return false; // Cancel closing
                }

                if (result == MessageBoxResult.Yes)
                {
                    SaveSettingsCommand.Execute(null);
                }
            }
            // Hide the window instead of closing
            Application.Current.MainWindow?.Hide();
            return false; // Always cancel closing (hide instead)
        }

        #endregion Public Methods (for View Interaction)

        #region Command Handlers

        /// <summary>
        /// Handles the selection of a monitor from the UI.
        /// </summary>
        /// <param name="parameter">The monitor to select.</param>
        private void ExecuteSelectMonitor(object? parameter)
        {
            if (parameter is MonitorLayoutViewModel monitor) { SelectedMonitor = monitor; }
        }

        /// <summary>
        /// Handles discarding unsaved changes by refreshing the monitor list.
        /// </summary>
        private void ExecuteDiscardChanges()
        {
            RefreshMonitors();
        }

        /// <summary>
        /// Orchestrates the three steps of the save process: validation, action, and feedback.
        /// </summary>
        private async Task ExecuteSaveSettings()
        {
            AppNotifications.TriggerRestoreAllMonitors();

            if (!ValidateSettings())
            {
                return; // Stop if invalid
            }

            PerformSaveActions();

            await ProvideSaveFeedbackAsync();
        }

        #endregion Command Handlers

        #region Private Helper Methods

        // --- Save Process Helpers ---

        /// <summary>
        /// Validates all monitor settings using the save validation service.
        /// </summary>
        /// <returns>True if all monitors are valid; otherwise, false.</returns>
        private bool ValidateSettings()
        {
            return _saveValidationService.ValidateAndNotify(Monitors);
        }

        /// <summary>
        /// Saves all monitor settings and updates the idle activity service. Marks all monitors as saved.
        /// </summary>
        private void PerformSaveActions()
        {
            var allSettings = Monitors.Select(m => m.Configuration.ToSettings()).ToList();
            _settingsService.SaveSettings(allSettings);

            foreach (var monitorVM in Monitors)
            {
                monitorVM.Configuration.MarkAsSaved();
            }
            CheckDirtyState();
        }

        /// <summary>
        /// Provides user feedback after saving settings by updating the save button text temporarily.
        /// </summary>
        private async Task ProvideSaveFeedbackAsync()
        {
            SaveButtonText = "Saved!";
            await Task.Delay(2000);
            SaveButtonText = "Save Settings";
        }

        // --- Monitor Update Helpers ---

        /// <summary>
        /// The core worker method for updating the monitor list and layout.
        /// </summary>
        /// <param name="width">The width of the container for layout.</param>
        /// <param name="height">The height of the container for layout.</param>
        /// <param name="preserveSelection">Whether to preserve the current monitor selection.</param>
        private void UpdateMonitorsInternal(double width, double height, bool preserveSelection)
        {
            if (width <= 0 || height <= 0) return;
            _containerWidth = width;
            _containerHeight = height;

            string? selectedMonitorId = preserveSelection ? SelectedMonitor?.HardwareId : null;
            var newMonitorLayoutViewModels = _workspaceService.BuildWorkspace(width, height);

            PopulateMonitors(newMonitorLayoutViewModels);
            RestoreSelection(selectedMonitorId);

            CheckDirtyState();
        }

        /// <summary>
        /// Populates the Monitors collection with new view models and wires up dirty state change notifications.
        /// </summary>
        /// <param name="newViewModels">The new monitor layout view models.</param>
        private void PopulateMonitors(ObservableCollection<MonitorLayoutViewModel> newViewModels)
        {
            Monitors.Clear();
            foreach (var viewModel in newViewModels)
            {
                viewModel.OnMonitorDirtyStateChanged = CheckDirtyState;
                Monitors.Add(viewModel);
            }
        }

        /// <summary>
        /// Restores the monitor selection based on a hardware ID, if available.
        /// </summary>
        /// <param name="selectedMonitorId">The hardware ID of the monitor to select.</param>
        private void RestoreSelection(string? selectedMonitorId)
        {
            SelectedMonitor = selectedMonitorId != null
                ? Monitors.FirstOrDefault(m => m.HardwareId == selectedMonitorId)
                : null;
        }

        /// <summary>
        /// Checks if any monitor is dirty and updates the IsDirty property accordingly.
        /// </summary>
        private void CheckDirtyState()
        {
            IsDirty = Monitors.Any(m => m.IsDirty);
        }

        #endregion Private Helper Methods
    }
}