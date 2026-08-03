namespace OLED_Sleeper.Features.MonitorDimming.Services.Interfaces
{
    /// <summary>
    /// Holds each dimmed monitor's pre-dim brightness and keeps the state file in step with it.
    /// </summary>
    public interface IOriginalBrightnessStore
    {
        /// <summary>
        /// Gets the recorded pre-dim brightness for a monitor.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        /// <param name="brightness">The recorded raw brightness value, or zero when there is no recording.</param>
        /// <returns>True when the monitor has a recording; otherwise, false.</returns>
        bool TryGetOriginal(string hardwareId, out uint brightness);

        /// <summary>
        /// Records the pre-dim brightness for a monitor and saves the state.
        /// An existing entry is kept, not overwritten.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        /// <param name="brightness">The raw brightness value to record.</param>
        void RecordOriginal(string hardwareId, uint brightness);

        /// <summary>
        /// Removes a monitor's recording and saves the state. Does nothing when there is no recording.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        void RemoveOriginal(string hardwareId);

        /// <summary>
        /// Gets every recording.
        /// </summary>
        /// <returns>A copy of the map from hardware ID to raw pre-dim brightness.</returns>
        Dictionary<string, uint> GetAll();
    }
}
