using OLED_Sleeper.Features.MonitorInformation.Models;
using OLED_Sleeper.UI.Services.Interfaces;
using OLED_Sleeper.UI.ViewModels;
using Serilog;
using System.Collections.ObjectModel;
using System.Windows;

namespace OLED_Sleeper.UI.Services
{
    /// <summary>
    /// Provides functionality to create a layout of monitor view models for UI display.
    /// </summary>
    public class MonitorLayoutService : IMonitorLayoutService
    {
        /// <summary>
        /// Creates a collection of <see cref="MonitorLayoutViewModel"/> objects for the given monitors and container size.
        /// </summary>
        /// <param name="monitorInfos">The list of monitor information models.</param>
        /// <param name="containerWidth">The width of the container for layout.</param>
        /// <param name="containerHeight">The height of the container for layout.</param>
        /// <returns>An observable collection of monitor layout view models.</returns>
        public ObservableCollection<MonitorLayoutViewModel> CreateLayout(List<MonitorInfo>? monitorInfos, double containerWidth, double containerHeight)
        {
            Log.Debug("--- Starting Layout Calculation ---");
            Log.Debug("Container size: {Width}x{Height}", containerWidth, containerHeight);

            var monitorLayoutViewModels = new ObservableCollection<MonitorLayoutViewModel>();
            if (monitorInfos == null || !monitorInfos.Any())
            {
                Log.Warning("No monitors found to create layout.");
                return monitorLayoutViewModels;
            }

            LogMonitorInfos(monitorInfos);

            var totalBounds = CalculateTotalBounds(monitorInfos);
            Log.Debug("Calculated TotalBounds: {Bounds}", totalBounds);

            var (scale, offsetX, offsetY) = CalculateLayoutParameters(containerWidth, containerHeight, totalBounds);

            monitorLayoutViewModels = CreateMonitorViewModels(monitorInfos, scale, totalBounds, offsetX, offsetY);

            Log.Debug("--- Finished Layout Calculation ---");
            return monitorLayoutViewModels;
        }

        /// <summary>
        /// Logs information about each monitor.
        /// </summary>
        /// <param name="monitorInfos">The list of monitor information models.</param>
        private static void LogMonitorInfos(List<MonitorInfo> monitorInfos)
        {
            Log.Debug("Calculating layout for {Count} monitors:", monitorInfos.Count);
            foreach (var m in monitorInfos)
            {
                Log.Debug("  - {DeviceName}: Bounds={Bounds}, DPI={Dpi}", m.DeviceName, m.Bounds, m.Dpi);
            }
        }

        /// <summary>
        /// Calculates the scale and offsets for the layout.
        /// </summary>
        /// <param name="containerWidth">The width of the container.</param>
        /// <param name="containerHeight">The height of the container.</param>
        /// <param name="totalBounds">The total bounds of all monitors.</param>
        /// <returns>A tuple containing scale, offsetX, and offsetY.</returns>
        private static (double scale, double offsetX, double offsetY) CalculateLayoutParameters(double containerWidth, double containerHeight, Rect totalBounds)
        {
            double scale = CalculateScale(containerWidth, containerHeight, totalBounds);
            Log.Debug("Calculated Scale factor: {Scale}", scale);
            double scaledLayoutWidth = totalBounds.Width * scale;
            double scaledLayoutHeight = totalBounds.Height * scale;
            double offsetX = (containerWidth - scaledLayoutWidth) / 2;
            double offsetY = (containerHeight - scaledLayoutHeight) / 2;
            Log.Debug("Calculated Offset: X={OffsetX}, Y={OffsetY}", offsetX, offsetY);
            return (scale, offsetX, offsetY);
        }

        /// <summary>
        /// Creates the collection of MonitorLayoutViewModel objects for the layout.
        /// </summary>
        /// <param name="monitorInfos">The list of monitor information models.</param>
        /// <param name="scale">The scale factor for layout display.</param>
        /// <param name="totalBounds">The total bounds of all monitors for layout calculations.</param>
        /// <param name="offsetX">The X offset for layout positioning.</param>
        /// <param name="offsetY">The Y offset for layout positioning.</param>
        /// <returns>An observable collection of monitor layout view models.</returns>
        private static ObservableCollection<MonitorLayoutViewModel> CreateMonitorViewModels(List<MonitorInfo> monitorInfos, double scale, Rect totalBounds, double offsetX, double offsetY)
        {
            var monitorLayoutViewModels = new ObservableCollection<MonitorLayoutViewModel>();
            foreach (var monitorInfo in monitorInfos)
            {
                var monitorVm = new MonitorLayoutViewModel(monitorInfo, scale, totalBounds, offsetX, offsetY);
                Log.Debug("Creating MonitorLayoutViewModel for {DeviceName}: ScaledLeft={L}, ScaledTop={T}, ScaledWidth={W}, ScaledHeight={H}",
                    monitorInfo.DeviceName, monitorVm.ScaledLeft, monitorVm.ScaledTop, monitorVm.ScaledWidth, monitorVm.ScaledHeight);
                monitorLayoutViewModels.Add(monitorVm);
            }
            return monitorLayoutViewModels;
        }

        /// <summary>
        /// Calculates the total bounding rectangle that contains all monitors.
        /// </summary>
        /// <param name="monitorInfos">The list of monitor information models.</param>
        /// <returns>The total bounding rectangle.</returns>
        private static Rect CalculateTotalBounds(List<MonitorInfo> monitorInfos)
        {
            return new Rect(
                monitorInfos.Min(m => m.Bounds.Left),
                monitorInfos.Min(m => m.Bounds.Top),
                monitorInfos.Max(m => m.Bounds.Right) - monitorInfos.Min(m => m.Bounds.Left),
                monitorInfos.Max(m => m.Bounds.Bottom) - monitorInfos.Min(m => m.Bounds.Top)
            );
        }

        /// <summary>
        /// Calculates the scale factor to fit all monitors within the container.
        /// </summary>
        /// <param name="containerWidth">The width of the container.</param>
        /// <param name="containerHeight">The height of the container.</param>
        /// <param name="totalBounds">The total bounds of all monitors.</param>
        /// <returns>The scale factor.</returns>
        private static double CalculateScale(double containerWidth, double containerHeight, Rect totalBounds)
        {
            return Math.Min(containerWidth / totalBounds.Width, containerHeight / totalBounds.Height) * 0.9;
        }
    }
}