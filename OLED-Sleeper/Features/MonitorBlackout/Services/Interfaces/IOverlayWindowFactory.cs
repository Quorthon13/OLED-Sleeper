namespace OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for creating blackout overlay windows.
    /// </summary>
    public interface IOverlayWindowFactory
    {
        /// <summary>
        /// Creates an overlay that has not been shown yet. Must be called on the UI thread.
        /// </summary>
        /// <returns>The new overlay.</returns>
        IOverlayWindow Create();
    }
}
