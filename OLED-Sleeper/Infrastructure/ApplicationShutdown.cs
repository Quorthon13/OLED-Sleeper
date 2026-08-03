using OLED_Sleeper.Core.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace OLED_Sleeper.Infrastructure
{
    /// <summary>
    /// Ends the process by shutting down the running <see cref="Application"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ApplicationShutdown : IApplicationShutdown
    {
        /// <inheritdoc />
        public void Shutdown() => Application.Current?.Shutdown();
    }
}
