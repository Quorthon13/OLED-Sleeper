namespace OLED_Sleeper.Services
{
    public interface IDimmerService
    {
        /// <summary>
        /// Dims a monitor to a specified brightness level.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        /// <param name="dimLevel">The target brightness level (0-100).</param>
        void DimMonitor(string hardwareId, int dimLevel);

        /// <summary>
        /// Restores a monitor to its original brightness.
        /// </summary>
        /// <param name="hardwareId">The unique hardware ID of the monitor.</param>
        void UndimMonitor(string hardwareId);
    }
}