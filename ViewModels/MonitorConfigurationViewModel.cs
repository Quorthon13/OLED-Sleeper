// File: ViewModels/MonitorConfigurationViewModel.cs
using OLED_Sleeper.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OLED_Sleeper.ViewModels
{
    public class MonitorConfigurationViewModel : ViewModelBase
    {
        private readonly MonitorInfo _monitorInfo;

        public Action? OnDirtyStateChanged { get; set; }

        private bool _initialIsManaged;
        private MonitorBehavior _initialBehavior;
        private double _initialDimLevel;

        // New fields for dirty tracking
        private int _initialIdleValue;

        private TimeUnit _initialSelectedTimeUnit;

        public bool IsDirty { get; private set; }

        public string MonitorTitle => _monitorInfo.IsPrimary ? $"Monitor {DisplayNumber} (Primary)" : $"Monitor {DisplayNumber}";
        public int DisplayNumber { get; }

        private bool _isManaged = true;

        public bool IsManaged
        {
            get => _isManaged;
            set { _isManaged = value; OnPropertyChanged(); UpdateDirtyState(); }
        }

        private MonitorBehavior _behavior = MonitorBehavior.Dim;

        public MonitorBehavior Behavior
        {
            get => _behavior;
            set
            {
                _behavior = value;
                if (_behavior == MonitorBehavior.Blackout) { DimLevel = 0; }
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDimSliderEnabled));
                UpdateDirtyState();
            }
        }

        private double _dimLevel = 15;

        public double DimLevel
        {
            get => _dimLevel;
            set { _dimLevel = value; OnPropertyChanged(); UpdateDirtyState(); }
        }

        public bool IsDimSliderEnabled => Behavior == MonitorBehavior.Dim;

        // --- New Properties for Idle Time ---
        public List<TimeUnit> TimeUnits { get; } = Enum.GetValues(typeof(TimeUnit)).Cast<TimeUnit>().ToList();

        private int _idleValue = 30;

        public int IdleValue
        {
            get => _idleValue;
            set { _idleValue = value; OnPropertyChanged(); UpdateDirtyState(); }
        }

        private TimeUnit _selectedTimeUnit = TimeUnit.Seconds;

        public TimeUnit SelectedTimeUnit
        {
            get => _selectedTimeUnit;
            set { _selectedTimeUnit = value; OnPropertyChanged(); UpdateDirtyState(); }
        }

        // --- End of New Properties ---

        public MonitorConfigurationViewModel(MonitorInfo monitorInfo, int displayNumber)
        {
            _monitorInfo = monitorInfo;
            DisplayNumber = displayNumber;
            MarkAsSaved(); // Initialize the "saved" state
        }

        private void UpdateDirtyState()
        {
            IsDirty = (IsManaged != _initialIsManaged ||
                       Behavior != _initialBehavior ||
                       DimLevel != _initialDimLevel ||
                       IdleValue != _initialIdleValue ||
                       SelectedTimeUnit != _initialSelectedTimeUnit); // Updated logic
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
            UpdateDirtyState();
        }

        public void ApplySettings(MonitorSettings settings)
        {
            IsManaged = settings.IsManaged;
            Behavior = settings.Behavior;
            DimLevel = settings.DimLevel;
            IdleValue = settings.IdleValue;
            SelectedTimeUnit = settings.IdleUnit;
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
                IdleUnit = this.SelectedTimeUnit
            };
        }
    }
}