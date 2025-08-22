// File: /ViewModels/MonitorViewModel.cs

using System.Windows;
using OLED_Sleeper.Models;
using System.Windows.Media;

namespace OLED_Sleeper.ViewModels
{
    public class MonitorViewModel : ViewModelBase
    {
        private readonly MonitorInfo _monitor;

        // Add a property to hold the configuration for this monitor
        public MonitorConfigurationViewModel Configuration { get; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public string MonitorTitle => _monitor.IsPrimary ? $"Monitor {DisplayNumber} (Primary)" : $"Monitor {DisplayNumber}";
        public int DisplayNumber { get; }
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

            // Initialize the configuration ViewModel
            Configuration = new MonitorConfigurationViewModel(monitor);

            ScaledWidth = _monitor.Bounds.Width * scale;
            ScaledHeight = _monitor.Bounds.Height * scale;
            ScaledLeft = ((_monitor.Bounds.Left - totalBounds.Left) * scale) + offsetX;
            ScaledTop = ((_monitor.Bounds.Top - totalBounds.Top) * scale) + offsetY;
        }
    }
}