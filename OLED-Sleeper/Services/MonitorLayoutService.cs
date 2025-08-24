// File: Services/MonitorLayoutService.cs
using OLED_Sleeper.Models;
using OLED_Sleeper.ViewModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace OLED_Sleeper.Services
{
    // Refactored: Implements IMonitorLayoutService for dependency injection.
    public class MonitorLayoutService : IMonitorLayoutService
    {
        public ObservableCollection<MonitorLayoutViewModel> CreateLayout(List<MonitorInfo> monitorInfos, double containerWidth, double containerHeight)
        {
            Log.Debug("--- Starting Layout Calculation ---");
            Log.Debug("Container size: {Width}x{Height}", containerWidth, containerHeight);
            var monitorLayoutViewModels = new ObservableCollection<MonitorLayoutViewModel>();
            if (monitorInfos == null || !monitorInfos.Any())
            {
                Log.Warning("No monitors found to create layout.");
                return monitorLayoutViewModels;
            }
            Log.Debug("Calculating layout for {Count} monitors:", monitorInfos.Count);
            foreach (var m in monitorInfos)
            {
                Log.Debug("  - {DeviceName}: Bounds={Bounds}, DPI={Dpi}", m.DeviceName, m.Bounds, m.Dpi);
            }
            var totalBounds = new Rect(
                monitorInfos.Min(m => m.Bounds.Left),
                monitorInfos.Min(m => m.Bounds.Top),
                monitorInfos.Max(m => m.Bounds.Right) - monitorInfos.Min(m => m.Bounds.Left),
                monitorInfos.Max(m => m.Bounds.Bottom) - monitorInfos.Min(m => m.Bounds.Top)
            );
            Log.Debug("Calculated TotalBounds: {Bounds}", totalBounds);
            double scale = Math.Min(containerWidth / totalBounds.Width, containerHeight / totalBounds.Height) * 0.9;
            Log.Debug("Calculated Scale factor: {Scale}", scale);
            double scaledLayoutWidth = totalBounds.Width * scale;
            double scaledLayoutHeight = totalBounds.Height * scale;
            double offsetX = (containerWidth - scaledLayoutWidth) / 2;
            double offsetY = (containerHeight - scaledLayoutHeight) / 2;
            Log.Debug("Calculated Offset: X={OffsetX}, Y={OffsetY}", offsetX, offsetY);
            int fallbackMonitorNumber = 1;
            foreach (var monitorInfo in monitorInfos)
            {
                // Refactored: Centralized display number parsing.
                var monitorVm = new MonitorLayoutViewModel(monitorInfo, scale, totalBounds, offsetX, offsetY);
                Log.Debug("Creating MonitorLayoutViewModel for {DeviceName}: ScaledLeft={L}, ScaledTop={T}, ScaledWidth={W}, ScaledHeight={H}",
                    monitorInfo.DeviceName, monitorVm.ScaledLeft, monitorVm.ScaledTop, monitorVm.ScaledWidth, monitorVm.ScaledHeight);
                monitorLayoutViewModels.Add(monitorVm);
            }
            Log.Debug("--- Finished Layout Calculation ---");
            return monitorLayoutViewModels;
        }
    }
}