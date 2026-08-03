namespace OLED_Sleeper.Features.MonitorDimming.Services.Interfaces
{
    /// <summary>
    /// An open DDC/CI channel to one monitor. Reads and writes issued through it share a
    /// single physical monitor handle, which is released on disposal.
    /// </summary>
    public interface IDdcCiSession : IDisposable
    {
        /// <summary>
        /// Reads the monitor's current brightness.
        /// </summary>
        /// <returns>The raw brightness value, or null when the monitor did not answer.</returns>
        uint? GetBrightness();

        /// <summary>
        /// Writes a brightness value to the monitor.
        /// </summary>
        /// <param name="brightness">The raw value, on the monitor's own scale rather than a percentage.</param>
        /// <returns>True when the monitor accepted the write; otherwise, false.</returns>
        bool SetBrightness(uint brightness);
    }
}
