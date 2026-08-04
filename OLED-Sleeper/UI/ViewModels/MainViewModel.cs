using OLED_Sleeper.Infrastructure.Runtime.Interfaces;
using OLED_Sleeper.UI.Commands;
using OLED_Sleeper.UI.Services.Interfaces;
using Serilog;
using System.Collections.ObjectModel;
using System.Windows;
using ICommand = System.Windows.Input.ICommand;

namespace OLED_Sleeper.UI.ViewModels
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
        /// Validates and saves the monitor settings.
        /// </summary>
        private readonly IMonitorSettingsSaveService _saveService;

        /// <summary>
        /// Runs actions on the UI thread.
        /// </summary>
        private readonly IDispatcher _dispatcher;

        /// <summary>
        /// Reaches the application's main window.
        /// </summary>
        private readonly IMainWindowAccessor _mainWindowAccessor;

        /// <summary>
        /// Shows modal dialogs to the user.
        /// </summary>
        private readonly IDialogService _dialogService;

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

        /// <summary>
        /// Indicates whether the workspace is currently loading.
        /// </summary>
        private bool _isLoading;

        /// <summary>
        /// Incremented for every workspace build. A build whose number is no longer current has been
        /// superseded and its result is discarded.
        /// </summary>
        private int _buildGeneration;

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

        /// <summary>
        /// Gets or sets a value indicating whether the workspace is currently loading.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

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

        public MainViewModel(
            IWorkspaceService workspaceService,
            IMonitorSettingsSaveService saveService,
            IDispatcher dispatcher,
            IMainWindowAccessor mainWindowAccessor,
            IDialogService dialogService)
        {
            _workspaceService = workspaceService;
            _saveService = saveService;
            _dispatcher = dispatcher;
            _mainWindowAccessor = mainWindowAccessor;
            _dialogService = dialogService;

            SelectMonitorCommand = new RelayCommand(ExecuteSelectMonitor);
            ReloadMonitorsCommand = new RelayCommand(() => RefreshMonitors(false));
            SaveSettingsCommand = new AsyncRelayCommand(ExecuteSaveSettings, () => IsDirty);
            DiscardChangesCommand = new RelayCommand(ExecuteDiscardChanges, () => IsDirty);
        }

        #endregion Constructor

        #region Public Methods (for View Interaction)

        /// <summary>
        /// Initiates a full refresh of the monitor list, re-scanning the display set before rebuilding the layout.
        /// </summary>
        /// <param name="preserveSelection">Whether to reselect the current monitor once the rebuild finishes.</param>
        public void RefreshMonitors(bool preserveSelection)
        {
            ApplyWorkspace(_workspaceService.RefreshWorkspaceAsync, _containerWidth, _containerHeight, preserveSelection);
        }

        /// <summary>
        /// Recalculates the monitor layout based on a new container size, preserving the current selection if possible.
        /// </summary>
        /// <param name="width">The new width of the container.</param>
        /// <param name="height">The new height of the container.</param>
        public void RecalculateLayout(double width, double height)
        {
            ApplyWorkspace(_workspaceService.BuildWorkspaceAsync, width, height, preserveSelection: true);
        }

        /// <summary>
        /// Handles logic for when the main window is closing. Returns true if the window should close, false to cancel.
        /// </summary>
        /// <returns>True to allow closing, false to cancel.</returns>
        public bool OnWindowClosing()
        {
            if (IsDirty)
            {
                var result = _dialogService.AskYesNoCancel(
                    "You have unsaved changes. Would you like to save them before hiding the window?",
                    "Unsaved Changes");

                if (result == MessageBoxResult.Cancel)
                {
                    return false; // Cancel closing
                }

                if (result == MessageBoxResult.Yes)
                {
                    SaveSettingsCommand.Execute(null);
                }
            }
            _mainWindowAccessor.HideMainWindow();

            return false;
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
            RefreshMonitors(true);
        }

        /// <summary>
        /// Saves the monitor settings and reports the outcome on the save button.
        /// </summary>
        private async Task ExecuteSaveSettings()
        {
            if (!_saveService.TrySave(Monitors))
            {
                return; // Stop if invalid
            }

            CheckDirtyState();

            await ProvideSaveFeedbackAsync();
        }

        #endregion Command Handlers

        #region Private Helper Methods

        // --- Save Process Helpers ---

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
        /// Builds the workspace and applies the result to the UI. A build superseded by a later one
        /// leaves the monitor list untouched. Failures are logged; nothing is thrown to the caller.
        /// </summary>
        /// <param name="build">Produces the layout view models for the given container size.</param>
        /// <param name="width">The width of the container for layout.</param>
        /// <param name="height">The height of the container for layout.</param>
        /// <param name="preserveSelection">Whether to reselect the current monitor once the build finishes.</param>
        private async void ApplyWorkspace(
            Func<double, double, Task<ObservableCollection<MonitorLayoutViewModel>>> build,
            double width,
            double height,
            bool preserveSelection)
        {
            if (width <= 0 || height <= 0) return;
            _containerWidth = width;
            _containerHeight = height;

            var generation = ++_buildGeneration;
            var monitorIdToRestore = preserveSelection ? SelectedMonitor?.HardwareId : null;
            IsLoading = true;

            try
            {
                var newMonitorLayoutViewModels = await build(width, height);
                if (generation != _buildGeneration) return;

                PopulateMonitors(newMonitorLayoutViewModels);
                RestoreSelection(monitorIdToRestore);
                CheckDirtyState();
                IsLoading = false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to build the monitor workspace.");
                if (generation == _buildGeneration) { IsLoading = false; }
            }
        }

        /// <summary>
        /// Populates the Monitors collection with new view models and wires up dirty state change notifications.
        /// </summary>
        /// <param name="newViewModels">The new monitor layout view models.</param>
        private void PopulateMonitors(ObservableCollection<MonitorLayoutViewModel> newViewModels)
        {
            if (!_dispatcher.IsOnUiThread)
            {
                _dispatcher.Invoke(() => PopulateMonitors(newViewModels));
                return;
            }

            Monitors.Clear();
            foreach (var viewModel in newViewModels)
            {
                viewModel.OnMonitorDirtyStateChanged = CheckDirtyState;
                Monitors.Add(viewModel);
            }
        }

        /// <summary>
        /// Restores the monitor selection based on a hardware ID.
        /// </summary>
        /// <param name="hardwareId">The monitor to reselect, or null to clear the selection.</param>
        private void RestoreSelection(string? hardwareId)
        {
            SelectedMonitor = hardwareId != null
                ? Monitors.FirstOrDefault(m => m.HardwareId == hardwareId)
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