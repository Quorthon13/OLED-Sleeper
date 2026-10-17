namespace OLED_Sleeper.Features.MonitorIdleDetection.Media.Interfaces
{
    /// <summary>
    /// Reports whether the system is being told to keep the display on, which is what video players and browsers do while a video plays.
    /// The signal is system-wide: it does not say which monitor or application holds the request.
    /// </summary>
    public interface IDisplayRequiredSignal
    {
        /// <summary>
        /// Gets whether some request is holding the display on.
        /// </summary>
        bool IsDisplayRequired();
    }
}
