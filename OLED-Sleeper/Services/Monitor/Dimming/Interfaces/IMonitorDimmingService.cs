namespace OLED_Sleeper.Services.Monitor.Dimming.Interfaces
{
    /// <summary>
    /// Defines the contract for monitor dimming and brightness management services, supporting asynchronous operations.
    /// </summary>
    public interface IMonitorDimmingService
    {
        /// <summary>
        /// Dims the specified monitor to the given brightness level asynchronously.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        /// <param name="dimLevel">The brightness level to set (0-100).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DimMonitorAsync(string? hardwareId, int dimLevel);

        /// <summary>
        /// Restores the specified monitor to its original brightness asynchronously.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UndimMonitorAsync(string hardwareId);

        /// <summary>
        /// Restores the specified monitor to a previously saved brightness value asynchronously.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        /// <param name="originalBrightness">The brightness value to restore.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RestoreBrightnessAsync(string hardwareId, uint originalBrightness);

        /// <summary>
        /// Gets a dictionary of all currently dimmed monitors and their original brightness values.
        /// </summary>
        /// <returns>A dictionary mapping hardware IDs to original brightness values.</returns>
        Dictionary<string, uint> GetDimmedMonitors();
    }
}