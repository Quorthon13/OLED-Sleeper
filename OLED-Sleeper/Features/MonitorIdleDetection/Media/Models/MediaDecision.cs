namespace OLED_Sleeper.Features.MonitorIdleDetection.Media.Models
{
    /// <summary>
    /// The outcome of the video rule for one monitor.
    /// </summary>
    /// <param name="IsVideoPlaying">Whether a video, fullscreen or windowed, is playing on the monitor.</param>
    /// <param name="Signal">A short description of the signals that made the decision true, or an empty string.</param>
    public sealed record MediaDecision(bool IsVideoPlaying, string Signal)
    {
        /// <summary>
        /// The decision for a monitor with no video.
        /// </summary>
        public static MediaDecision None { get; } = new(false, string.Empty);
    }
}
