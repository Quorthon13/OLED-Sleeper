namespace OLED_Sleeper.Models
{
    public class MonitorSettings
    {
        public string HardwareId { get; set; } = string.Empty;
        public bool IsManaged { get; set; } = false;
        public MonitorBehavior Behavior { get; set; } = MonitorBehavior.Dim;
        public double DimLevel { get; set; } = 15;
        public int? IdleValue { get; set; } = 30;

        public TimeUnit IdleUnit { get; set; } = TimeUnit.Seconds;

        public bool IsActiveOnInput { get; set; } = true;
        public bool IsActiveOnMousePosition { get; set; } = false;
        public bool IsActiveOnActiveWindow { get; set; } = false;

        public int IdleTimeMilliseconds
        {
            get
            {
                if (IdleValue == null) return 0;

                switch (IdleUnit)
                {
                    case TimeUnit.Minutes:
                        return IdleValue.Value * 60 * 1000;

                    case TimeUnit.Hours:
                        return IdleValue.Value * 60 * 60 * 1000;

                    case TimeUnit.Seconds:
                    default:
                        return IdleValue.Value * 1000;
                }
            }
        }
    }
}