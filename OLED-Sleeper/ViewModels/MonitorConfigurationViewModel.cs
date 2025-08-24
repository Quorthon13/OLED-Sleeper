// File: ViewModels/MonitorConfigurationViewModel.cs
using OLED_Sleeper.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace OLED_Sleeper.ViewModels
{
    // Implement IDataErrorInfo for validation
    public class MonitorConfigurationViewModel : ViewModelBase, IDataErrorInfo
    {
        #region Existing Properties

        private readonly MonitorInfo _monitorInfo;
        public Action? OnDirtyStateChanged { get; set; }
        private bool _initialIsManaged;
        private MonitorBehavior _initialBehavior;
        private double _initialDimLevel;
        private int? _initialIdleValue;
        private TimeUnit _initialSelectedTimeUnit;
        private bool _initialIsActiveOnInput;
        private bool _initialIsActiveOnMousePosition;
        private bool _initialIsActiveOnActiveWindow;
        public bool IsDirty { get; private set; }
        public string MonitorTitle => _monitorInfo.IsPrimary ? $"Monitor {DisplayNumber} (Primary)" : $"Monitor {DisplayNumber}";
        public int DisplayNumber { get; }

        private bool _isManaged = true;

        public bool IsManaged
        {
            get => _isManaged;
            set
            {
                _isManaged = value;
                OnPropertyChanged();
                UpdateDirtyState();
            }
        }

        private MonitorBehavior _behavior = MonitorBehavior.Dim;

        public MonitorBehavior Behavior
        {
            get => _behavior;
            set
            {
                _behavior = value;
                if (_behavior == MonitorBehavior.Blackout)
                {
                    DimLevel = 0;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDimSliderEnabled));
                UpdateDirtyState();
            }
        }

        private double _dimLevel = 15;

        public double DimLevel
        {
            get => _dimLevel;
            set
            {
                _dimLevel = value;
                OnPropertyChanged();
                UpdateDirtyState();
            }
        }

        public bool IsDimSliderEnabled => Behavior == MonitorBehavior.Dim;
        public List<TimeUnit> TimeUnits { get; } = Enum.GetValues(typeof(TimeUnit)).Cast<TimeUnit>().ToList();

        private int? _idleValue = 30; // Changed to nullable

        public int? IdleValue
        {
            get => _idleValue;
            set
            {
                _idleValue = value;
                OnPropertyChanged();
                UpdateDirtyState();
            }
        }

        private TimeUnit _selectedTimeUnit = TimeUnit.Seconds;

        public TimeUnit SelectedTimeUnit
        {
            get => _selectedTimeUnit;
            set
            {
                _selectedTimeUnit = value;
                OnPropertyChanged();
                UpdateDirtyState();
            }
        }

        private bool _isActiveOnInput = true;

        public bool IsActiveOnInput
        {
            get => _isActiveOnInput;
            set
            {
                _isActiveOnInput = value;
                OnPropertyChanged();
                UpdateDirtyState();
            }
        }

        private bool _isActiveOnMousePosition;

        public bool IsActiveOnMousePosition
        {
            get => _isActiveOnMousePosition;
            set
            {
                _isActiveOnMousePosition = value;
                OnPropertyChanged();
                UpdateDirtyState();
            }
        }

        private bool _isActiveOnActiveWindow;

        public bool IsActiveOnActiveWindow
        {
            get => _isActiveOnActiveWindow;
            set
            {
                _isActiveOnActiveWindow = value;
                OnPropertyChanged();
                UpdateDirtyState();
            }
        }

        #endregion Existing Properties

        public MonitorConfigurationViewModel(MonitorInfo monitorInfo, int displayNumber)
        {
            _monitorInfo = monitorInfo;
            DisplayNumber = displayNumber;
            MarkAsSaved();
        }

        #region Existing Methods

        private void UpdateDirtyState()
        {
            IsDirty = (IsManaged != _initialIsManaged ||
                       Behavior != _initialBehavior ||
                       DimLevel != _initialDimLevel ||
                       IdleValue != _initialIdleValue ||
                       SelectedTimeUnit != _initialSelectedTimeUnit ||
                       IsActiveOnInput != _initialIsActiveOnInput ||
                       IsActiveOnMousePosition != _initialIsActiveOnMousePosition ||
                       IsActiveOnActiveWindow != _initialIsActiveOnActiveWindow);

            OnPropertyChanged(nameof(IsDirty));
            OnDirtyStateChanged?.Invoke();
        }

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
            MarkAsSaved();
        }

        public MonitorSettings ToSettings()
        {
            return new MonitorSettings
            {
                HardwareId = _monitorInfo.HardwareId,
                IsManaged = this.IsManaged,
                Behavior = this.Behavior,
                DimLevel = this.DimLevel,
                IdleValue = this.IdleValue,
                IdleUnit = this.SelectedTimeUnit,
                IsActiveOnInput = this.IsActiveOnInput,
                IsActiveOnMousePosition = this.IsActiveOnMousePosition,
                IsActiveOnActiveWindow = this.IsActiveOnActiveWindow
            };
        }

        #endregion Existing Methods

        public string Error => string.Empty;

        public string this[string columnName]
        {
            get
            {
                string? result = null;
                if (!IsManaged) return result;

                switch (columnName)
                {
                    case nameof(IdleValue):
                        // Updated validation to check for null
                        if (IdleValue == null || IdleValue <= 0)
                        {
                            result = "Idle time value must be a number greater than zero.";
                        }
                        break;

                    case nameof(IsActiveOnInput):
                    case nameof(IsActiveOnMousePosition):
                    case nameof(IsActiveOnActiveWindow):
                        if (!IsActiveOnInput && !IsActiveOnMousePosition && !IsActiveOnActiveWindow)
                        {
                            result = "At least one 'Consider Active When' option must be selected.";
                        }
                        break;
                }
                return result ?? string.Empty;
            }
        }

        public bool IsValid
        {
            get
            {
                if (!IsManaged) return true;

                // Updated validation to check for null
                if (IdleValue == null || IdleValue <= 0) return false;
                if (!IsActiveOnInput && !IsActiveOnMousePosition && !IsActiveOnActiveWindow) return false;

                return true;
            }
        }
    }
}