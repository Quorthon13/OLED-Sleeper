namespace OLED_Sleeper.Features.MonitorIdleDetection.Media.Interfaces
{
    /// <summary>
    /// Reports which process families have recently been producing audio.
    /// </summary>
    public interface IAudioActivityProbe
    {
        /// <summary>
        /// Gets the families whose audio peak rose above the audibility threshold within the recent window.
        /// </summary>
        IReadOnlySet<string> GetAudibleFamilies();
    }
}
