namespace OLED_Sleeper.Services.Monitor.Interfaces
{
    /// <summary>
    /// Defines the contract for monitor dimming and brightness management services.
    /// </summary>
    public interface IMonitorDimmingService
    {
        /// <summary>
        /// Dims the specified monitor to the given dim level.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        /// <param name="dimLevel">The dimming level to apply.</param>
        void DimMonitor(string hardwareId, int dimLevel);

        /// <summary>
        /// Restores the specified monitor to its original brightness.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        void UndimMonitor(string hardwareId);

        /// <summary>
        /// Restores the specified monitor to a previously saved brightness value.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        /// <param name="originalBrightness">The brightness value to restore.</param>
        void RestoreBrightness(string hardwareId, uint originalBrightness);

        /// <summary>
        /// Gets a dictionary of all currently dimmed monitors and their original brightness values.
        /// </summary>
        /// <returns>A dictionary mapping hardware IDs to original brightness values.</returns>
        Dictionary<string, uint> GetDimmedMonitors();
    }
}