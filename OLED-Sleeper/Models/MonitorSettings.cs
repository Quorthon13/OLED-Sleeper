// File: Models/MonitorSettings.cs
namespace OLED_Sleeper.Models
{
    public class MonitorSettings
    {
        public string HardwareId { get; set; } = string.Empty;
        public bool IsManaged { get; set; } = true;
        public MonitorBehavior Behavior { get; set; } = MonitorBehavior.Dim;
        public double DimLevel { get; set; } = 15;
        public int IdleValue { get; set; } = 30;
        public TimeUnit IdleUnit { get; set; } = TimeUnit.Seconds;
    }
}