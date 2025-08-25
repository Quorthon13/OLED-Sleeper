// File: Models/MonitorInfo.cs
using System.Windows;

namespace OLED_Sleeper.Models
{
    public class MonitorInfo
    {
        public string DeviceName { get; set; }
        public string HardwareId { get; set; }
        public Rect Bounds { get; set; }
        public bool IsPrimary { get; set; }
        public uint Dpi { get; set; }
        public int DisplayNumber { get; set; }
        public bool IsDdcCiSupported { get; set; }
    }
}