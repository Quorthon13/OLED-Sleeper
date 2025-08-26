namespace OLED_Sleeper.Models
{
    /// <summary>
    /// Specifies the time unit for idle timeout and other duration-based settings.
    /// </summary>
    public enum TimeUnit
    {
        /// <summary>
        /// Time is measured in seconds.
        /// </summary>
        Seconds,

        /// <summary>
        /// Time is measured in minutes.
        /// </summary>
        Minutes,

        /// <summary>
        /// Time is measured in hours.
        /// </summary>
        Hours
    }
}