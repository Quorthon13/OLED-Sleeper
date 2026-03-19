namespace OLED_Sleeper.Features.MonitorDimming.Services.Interfaces
{
    /// <summary>
    /// Provides methods for loading and saving the brightness state of monitors.
    /// </summary>
    public interface IMonitorBrightnessStateService
    {
        /// <summary>
        /// Loads the brightness state for all monitors from persistent storage.
        /// </summary>
        /// <returns>A dictionary mapping monitor hardware IDs to their brightness values.</returns>
        Dictionary<string, uint> LoadState();

        /// <summary>
        /// Saves the brightness state for all monitors to persistent storage.
        /// </summary>
        /// <param name="state">A dictionary mapping monitor hardware IDs to their brightness values.</param>
        void SaveState(Dictionary<string, uint> state);
    }
}