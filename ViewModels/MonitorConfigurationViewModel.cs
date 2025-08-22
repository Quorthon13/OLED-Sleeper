// File: /ViewModels/MonitorConfigurationViewModel.cs
using OLED_Sleeper.Models;
using System;

namespace OLED_Sleeper.ViewModels
{
    public class MonitorConfigurationViewModel : ViewModelBase
    {
        private readonly MonitorInfo _monitorInfo;

        // Action to notify the parent that a change has occurred
        public Action? OnDirtyStateChanged { get; set; }

        // Store the initial state
        private bool _initialIsManaged;
        private string _initialBehavior;
        private double _initialDimLevel;

        public bool IsDirty { get; private set; }

        public string MonitorTitle => _monitorInfo.IsPrimary ? $"Monitor {DisplayNumber} (Primary)" : $"Monitor {DisplayNumber}";
        public int DisplayNumber { get; }

        private bool _isManaged = true;
        public bool IsManaged
        {
            get => _isManaged;
            set { _isManaged = value; OnPropertyChanged(); UpdateDirtyState(); }
        }

        private string _behavior = "Dim";
        public string Behavior
        {
            get => _behavior;
            set
            {
                _behavior = value;
                if (_behavior == "Blackout") { DimLevel = 0; }
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

        public bool IsDimSliderEnabled => Behavior == "Dim";

        public MonitorConfigurationViewModel(MonitorInfo monitorInfo)
        {
            _monitorInfo = monitorInfo;
            DisplayNumber = int.Parse(System.Text.RegularExpressions.Regex.Match(monitorInfo.DeviceName, @"\d+$").Value);

            // Save the initial state when created
            _initialIsManaged = IsManaged;
            _initialBehavior = Behavior;
            _initialDimLevel = DimLevel;
        }

        private void UpdateDirtyState()
        {
            IsDirty = (IsManaged != _initialIsManaged ||
                       Behavior != _initialBehavior ||
                       DimLevel != _initialDimLevel);

            // Notify the parent MainViewModel that something has changed
            OnDirtyStateChanged?.Invoke();
        }

        public void MarkAsSaved()
        {
            // Reset the "saved" state to the current state
            _initialIsManaged = IsManaged;
            _initialBehavior = Behavior;
            _initialDimLevel = DimLevel;
            IsDirty = false;
        }
    }
}