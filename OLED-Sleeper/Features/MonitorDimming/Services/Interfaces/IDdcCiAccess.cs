namespace OLED_Sleeper.Features.MonitorDimming.Services.Interfaces
{
    /// <summary>
    /// Opens DDC/CI channels to attached monitors.
    /// </summary>
    public interface IDdcCiAccess
    {
        /// <summary>
        /// Opens a channel to the monitor with the given device name.
        /// </summary>
        /// <param name="deviceName">The display device name, such as <c>\\.\DISPLAY1</c>.</param>
        /// <returns>The channel, or null when the monitor could not be reached. The caller disposes it.</returns>
        IDdcCiSession? OpenSession(string deviceName);
    }
}
