using OLED_Sleeper.Models;
using System.Windows;
using System.Windows.Media;

namespace OLED_Sleeper.ViewModels
{
    /// <summary>
    /// ViewModel representing a monitor in the layout view.
    /// Handles scaled layout, selection, and dirty state tracking for a single monitor.
    /// </summary>
    public class MonitorLayoutViewModel : ViewModelBase
    {
        /// <summary>
        /// The underlying monitor information model.
        /// </summary>
        private readonly MonitorInfo _monitor;

        /// <summary>
        /// Action to notify the MainViewModel that this specific monitor's dirty state has changed.
        /// </summary>
        public Action? OnMonitorDirtyStateChanged { get; set; }

        /// <summary>
        /// The configuration ViewModel for this monitor.
        /// </summary>
        public MonitorConfigurationViewModel Configuration { get; }

        private bool _isSelected;

        /// <summary>
        /// Gets or sets whether this monitor is currently selected in the UI.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        private bool _isDirty;

        /// <summary>
        /// Gets or sets whether this monitor has unsaved changes in its configuration.
        /// </summary>
        public bool IsDirty
        {
            get => _isDirty;
            set { _isDirty = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// The display title for the monitor, including primary indicator if applicable.
        /// </summary>
        public string MonitorTitle => _monitor.IsPrimary ? $"Monitor {_monitor.DisplayNumber} (Primary)" : $"Monitor {_monitor.DisplayNumber}";

        /// <summary>
        /// The display number for the monitor.
        /// </summary>
        public int DisplayNumber { get; }

        /// <summary>
        /// The hardware ID for the monitor.
        /// </summary>
        public string HardwareId => _monitor.HardwareId;

        /// <summary>
        /// The resolution of the monitor as a formatted string (e.g., 1920x1080).
        /// </summary>
        public string ResolutionText => $"{(int)_monitor.Bounds.Width}x{(int)_monitor.Bounds.Height}";

        /// <summary>
        /// The background color for the monitor representation in the layout.
        /// </summary>
        public SolidColorBrush BackgroundColor => new SolidColorBrush(Color.FromRgb(60, 60, 60));

        /// <summary>
        /// The scaled width of the monitor for layout display.
        /// </summary>
        public double ScaledWidth { get; }

        /// <summary>
        /// The scaled height of the monitor for layout display.
        /// </summary>
        public double ScaledHeight { get; }

        /// <summary>
        /// The scaled left position of the monitor for layout display.
        /// </summary>
        public double ScaledLeft { get; }

        /// <summary>
        /// The scaled top position of the monitor for layout display.
        /// </summary>
        public double ScaledTop { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorLayoutViewModel"/> class.
        /// </summary>
        /// <param name="monitor">The monitor information model.</param>
        /// <param name="displayNumber">The display number for the monitor.</param>
        /// <param name="scale">The scale factor for layout display.</param>
        /// <param name="totalBounds">The total bounds of all monitors for layout calculations.</param>
        /// <param name="offsetX">The X offset for layout positioning.</param>
        /// <param name="offsetY">The Y offset for layout positioning.</param>
        public MonitorLayoutViewModel(MonitorInfo monitor, double scale, Rect totalBounds, double offsetX, double offsetY)
        {
            _monitor = monitor;

            Configuration = new MonitorConfigurationViewModel(monitor);
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