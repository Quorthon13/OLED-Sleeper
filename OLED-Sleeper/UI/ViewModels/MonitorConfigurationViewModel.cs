using OLED_Sleeper.Features.MonitorBehavior.Models;
using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.Features.UserSettings.Models;
using OLED_Sleeper.UI.Models;
using System.ComponentModel;

namespace OLED_Sleeper.UI.ViewModels
{
    /// <summary>
    /// ViewModel for configuring an individual monitor's behavior and settings.
    /// Handles state management, validation, and dirty tracking for the monitor configuration UI.
    /// </summary>
    public class MonitorConfigurationViewModel : ViewModelBase, IDataErrorInfo
    {
        /// <summary>
        /// The underlying monitor information model.
        /// </summary>
        private readonly MonitorInfo _monitorInfo;

        /// <summary>
        /// Callback invoked when the dirty state changes.
        /// </summary>
        public Action? OnDirtyStateChanged { get; set; }

        #region Properties

        /// <summary>
        /// The display title for the monitor, including primary indicator if applicable.
        /// </summary>
        public string MonitorTitle => _monitorInfo.IsPrimary ? $"Monitor {_monitorInfo.DisplayNumber} (Primary)" : $"Monitor {_monitorInfo.DisplayNumber}";

        // --- Updated: Added explicit error properties for robust UI binding ---
        public string IdleValueError => this["IdleValue"];

        public string ActiveConditionsError => this["IsActiveOnInput"];

        // --- IsManaged ---
        private bool _isManaged;

        private bool _initialIsManaged;

        /// <summary>
        /// Gets or sets whether this monitor is managed by the application.
        /// </summary>
        public bool IsManaged
        {
            get => _isManaged;
            set
            {
                _isManaged = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IdleValueError));
                OnPropertyChanged(nameof(ActiveConditionsError));
                OnPropertyChanged(nameof(BehaviorError)); // Ensure all errors update
                UpdateDirtyState();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the "Dim" behavior option should be enabled.
        /// </summary>
        public bool IsDimBehaviorEnabled => _monitorInfo.IsDdcCiSupported;

        /// <summary>
        /// Gets the tooltip for the entire Behavior section, changing based on DDC/CI support.
        /// </summary>
        public string BehaviorSectionTooltip
        {
            get
            {
                string baseTooltip = "Choose how the monitor reacts after the idle timer expires.\n\n" +
                                    "• Blackout: Turns the monitor completely black.\n" +
                                     "• Dim: Reduces the monitor's brightness using DDC/CI.";

                if (!_monitorInfo.IsDdcCiSupported)
                {
                    baseTooltip = "THE SELECTED MONITOR DOES NOT SUPPORT DIMMING.\n\n" + baseTooltip;
                }

                return baseTooltip;
            }
        }

        // --- Behavior ---
        private MonitorBehaviorType _behavior;

        private MonitorBehaviorType _initialBehavior;

        /// <summary>
        /// Gets or sets the behavior for this monitor (e.g., Dim, Blackout).
        /// </summary>
        public MonitorBehaviorType Behavior
        {
            get => _behavior;
            set
            {
                _behavior = value;
                if (_behavior == MonitorBehaviorType.Blackout) { DimLevel = 0; }
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDimSliderEnabled));
                OnPropertyChanged(nameof(BehaviorError));
                UpdateDirtyState();
            }
        }

        /// <summary>
        /// Returns an error message if the behavior is invalid.
        /// </summary>
        public string BehaviorError => this["Behavior"];

        // --- DimLevel ---
        private double _dimLevel;

        private double _initialDimLevel;

        /// <summary>
        /// Gets or sets the dimming level for the monitor (0-100).
        /// </summary>
        public double DimLevel
        {
            get => _dimLevel;
            set { _dimLevel = value; OnPropertyChanged(); UpdateDirtyState(); }
        }

        /// <summary>
        /// Returns true if the dim slider should be enabled (only for Dim behavior).
        /// </summary>
        public bool IsDimSliderEnabled => Behavior == MonitorBehaviorType.Dim;

        // --- Idle Timer ---
        /// <summary>
        /// List of available time units for idle timer.
        /// </summary>
        public List<TimeUnit> TimeUnits { get; } = Enum.GetValues(typeof(TimeUnit)).Cast<TimeUnit>().ToList();

        private int? _idleValue;
        private int? _initialIdleValue;

        /// <summary>
        /// Gets or sets the idle time value before the monitor behavior triggers.
        /// </summary>
        public int? IdleValue
        {
            get => _idleValue;
            // --- Updated: Notifies error property when changed ---
            set
            {
                _idleValue = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IdleValueError));
                UpdateDirtyState();
            }
        }

        private TimeUnit _selectedTimeUnit;
        private TimeUnit _initialSelectedTimeUnit;

        /// <summary>
        /// Gets or sets the selected time unit for the idle timer.
        /// </summary>
        public TimeUnit SelectedTimeUnit
        {
            get => _selectedTimeUnit;
            set { _selectedTimeUnit = value; OnPropertyChanged(); UpdateDirtyState(); }
        }

        // --- Active Conditions ---
        private bool _isActiveOnInput;

        private bool _initialIsActiveOnInput;

        /// <summary>
        /// Gets or sets whether keyboard/mouse input keeps the monitor active.
        /// </summary>
        public bool IsActiveOnInput
        {
            get => _isActiveOnInput;
            // --- Updated: Notifies error property when changed ---
            set
            {
                _isActiveOnInput = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ActiveConditionsError));
                UpdateDirtyState();
            }
        }

        private bool _isActiveOnMousePosition;
        private bool _initialIsActiveOnMousePosition;

        /// <summary>
        /// Gets or sets whether mouse position keeps the monitor active.
        /// </summary>
        public bool IsActiveOnMousePosition
        {
            get => _isActiveOnMousePosition;
            // --- Updated: Notifies error property when changed ---
            set
            {
                _isActiveOnMousePosition = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ActiveConditionsError));
                UpdateDirtyState();
            }
        }

        private bool _isActiveOnActiveWindow;
        private bool _initialIsActiveOnActiveWindow;

        /// <summary>
        /// Gets or sets whether the active window keeps the monitor active.
        /// </summary>
        public bool IsActiveOnActiveWindow
        {
            get => _isActiveOnActiveWindow;
            // --- Updated: Notifies error property when changed ---
            set
            {
                _isActiveOnActiveWindow = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ActiveConditionsError));
                UpdateDirtyState();
            }
        }

        #endregion Properties

        /// <summary>
        /// Initializes a new instance of the MonitorConfigurationViewModel class for a specific monitor.
        /// </summary>
        /// <param name="monitorInfo">The monitor information model.</param>
        /// <param name="displayNumber">The display number for the monitor.</param>
        public MonitorConfigurationViewModel(MonitorInfo monitorInfo)
        {
            _monitorInfo = monitorInfo;

            // Initialize properties from the model's defaults
            ApplySettings(new MonitorSettings());
            // Set the initial state for dirty checking
            MarkAsSaved();
        }

        #region State Management (Dirty Tracking, Saving, Loading)

        /// <summary>
        /// Gets whether the configuration has unsaved changes.
        /// </summary>
        public bool IsDirty { get; private set; }

        /// <summary>
        /// Updates the IsDirty property based on whether any tracked property has changed from its initial value.
        /// Uses a tolerance for floating point comparison.
        /// </summary>
        private void UpdateDirtyState()
        {
            const double epsilon = 0.0001;
            IsDirty = IsManaged != _initialIsManaged ||
                       Behavior != _initialBehavior ||
                       Math.Abs(DimLevel - _initialDimLevel) > epsilon ||
                       IdleValue != _initialIdleValue ||
                       SelectedTimeUnit != _initialSelectedTimeUnit ||
                       IsActiveOnInput != _initialIsActiveOnInput ||
                       IsActiveOnMousePosition != _initialIsActiveOnMousePosition ||
                       IsActiveOnActiveWindow != _initialIsActiveOnActiveWindow;

            OnPropertyChanged(nameof(IsDirty));
            OnDirtyStateChanged?.Invoke();
        }

        /// <summary>
        /// Marks the current state as saved, updating the initial values for dirty tracking.
        /// </summary>
        public void MarkAsSaved()
        {
            _initialIsManaged = IsManaged;
            _initialBehavior = Behavior;
            _initialDimLevel = DimLevel;
            _initialIdleValue = IdleValue;
            _initialSelectedTimeUnit = SelectedTimeUnit;
            _initialIsActiveOnInput = IsActiveOnInput;
            _initialIsActiveOnMousePosition = IsActiveOnMousePosition;
            _initialIsActiveOnActiveWindow = IsActiveOnActiveWindow;
            UpdateDirtyState();
        }

        /// <summary>
        /// Applies settings from a MonitorSettings model to this ViewModel.
        /// </summary>
        /// <param name="settings">The settings to apply.</param>
        public void ApplySettings(MonitorSettings settings)
        {
            IsManaged = settings.IsManaged;
            Behavior = settings.Behavior;
            DimLevel = settings.DimLevel;
            IdleValue = settings.IdleValue;
            SelectedTimeUnit = settings.IdleUnit;
            IsActiveOnInput = settings.IsActiveOnInput;
            IsActiveOnMousePosition = settings.IsActiveOnMousePosition;
            IsActiveOnActiveWindow = settings.IsActiveOnActiveWindow;
        }

        /// <summary>
        /// Converts the current ViewModel state to a MonitorSettings model.
        /// </summary>
        /// <returns>A MonitorSettings object representing the current configuration.</returns>
        public MonitorSettings ToSettings()
        {
            return new MonitorSettings
            {
                HardwareId = _monitorInfo.HardwareId,
                IsManaged = IsManaged,
                Behavior = Behavior,
                DimLevel = DimLevel,
                IdleValue = IdleValue,
                IdleUnit = SelectedTimeUnit,
                IsActiveOnInput = IsActiveOnInput,
                IsActiveOnMousePosition = IsActiveOnMousePosition,
                IsActiveOnActiveWindow = IsActiveOnActiveWindow
            };
        }

        #endregion State Management (Dirty Tracking, Saving, Loading)

        #region Validation

        /// <summary>
        /// Gets whether the current configuration is valid according to all validation rules.
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (!IsManaged) return true; // Unmanaged monitors are always "valid"

                // Check all validation rules
                return ValidateIdleValue() == null && ValidateActiveConditions() == null && ValidateBehavior() == null;
            }
        }

        /// <summary>
        /// Gets the error message for the entire object. Not used in this implementation.
        /// </summary>
        public string Error => string.Empty;

        /// <summary>
        /// Gets the error message for the property with the given name, if any.
        /// </summary>
        /// <param name="columnName">The name of the property to validate.</param>
        /// <returns>An error message if invalid, otherwise an empty string.</returns>
        public string this[string columnName]
        {
            get
            {
                if (!IsManaged) return string.Empty; // Only validate if the monitor is managed

                string? result = null;
                switch (columnName)
                {
                    case nameof(IdleValue):
                        result = ValidateIdleValue();
                        break;

                    case nameof(IsActiveOnInput):
                    case nameof(IsActiveOnMousePosition):
                    case nameof(IsActiveOnActiveWindow):
                        result = ValidateActiveConditions();
                        break;

                    case nameof(Behavior):
                        result = ValidateBehavior();
                        break;
                }
                return result ?? string.Empty;
            }
        }

        /// <summary>
        /// Validates the idle value property.
        /// </summary>
        /// <returns>An error message if invalid, otherwise null.</returns>
        private string? ValidateIdleValue()
        {
            if (IdleValue == null || IdleValue <= 0)
                return "Idle time value must be a number greater than zero.";
            return null;
        }

        /// <summary>
        /// Validates that at least one active condition is selected.
        /// </summary>
        /// <returns>An error message if invalid, otherwise null.</returns>
        private string? ValidateActiveConditions()
        {
            if (!IsActiveOnInput && !IsActiveOnMousePosition && !IsActiveOnActiveWindow)
                return "At least one 'Consider Active When' option must be selected.";
            return null;
        }

        /// <summary>
        /// Validates the Behavior property to ensure a monitor behavior is selected when managed.
        /// </summary>
        /// <returns>
        /// An error message if the behavior is not selected (None), otherwise null.
        /// </returns>
        private string? ValidateBehavior()
        {
            if (Behavior == MonitorBehaviorType.None)
                return "A monitor behavior must be selected.";
            return null;
        }

        #endregion Validation
    }
}