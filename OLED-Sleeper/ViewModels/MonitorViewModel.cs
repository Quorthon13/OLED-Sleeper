using OLED_Sleeper.Models;
using System.Windows;
using System.Windows.Media;

namespace OLED_Sleeper.ViewModels
{
    public class MonitorViewModel : ViewModelBase
    {
        private readonly MonitorInfo _monitor;

        // Action to notify the MainViewModel that this specific monitor's dirty state has changed.
        public Action? OnMonitorDirtyStateChanged { get; set; }

        public MonitorConfigurationViewModel Configuration { get; }

        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        private bool _isDirty;

        public bool IsDirty
        {
            get => _isDirty;
            set { _isDirty = value; OnPropertyChanged(); }
        }

        public string MonitorTitle => _monitor.IsPrimary ? $"Monitor {DisplayNumber} (Primary)" : $"Monitor {DisplayNumber}";
        public int DisplayNumber { get; }
        public string HardwareId => _monitor.HardwareId;
        public string ResolutionText => $"{(int)_monitor.Bounds.Width}x{(int)_monitor.Bounds.Height}";
        public SolidColorBrush BackgroundColor => new SolidColorBrush(Color.FromRgb(60, 60, 60));

        public double ScaledWidth { get; }
        public double ScaledHeight { get; }
        public double ScaledLeft { get; }
        public double ScaledTop { get; }

        public MonitorViewModel(MonitorInfo monitor, int displayNumber, double scale, Rect totalBounds, double offsetX, double offsetY)
        {
            _monitor = monitor;
            DisplayNumber = displayNumber;

            Configuration = new MonitorConfigurationViewModel(monitor, displayNumber);
            // Subscribe to the configuration's dirty state changes.
            Configuration.OnDirtyStateChanged = () =>
            {
                // Update this monitor's dirty state based on its configuration.
                this.IsDirty = Configuration.IsDirty;
                // Bubble the notification up to the MainViewModel.
                OnMonitorDirtyStateChanged?.Invoke();
            };

            ScaledWidth = _monitor.Bounds.Width * scale;
            ScaledHeight = _monitor.Bounds.Height * scale;
            ScaledLeft = ((_monitor.Bounds.Left - totalBounds.Left) * scale) + offsetX;
            ScaledTop = ((_monitor.Bounds.Top - totalBounds.Top) * scale) + offsetY;
        }
    }
}