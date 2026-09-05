using OLED_Sleeper.Features.MonitorBlackout.Services.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace OLED_Sleeper.Features.MonitorBlackout.Services
{
    /// <summary>
    /// Creates <see cref="OverlayWindow"/> instances.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class OverlayWindowFactory : IOverlayWindowFactory
    {
        /// <inheritdoc />
        public IOverlayWindow Create() => new OverlayWindow();
    }
}
