using OLED_Sleeper.Infrastructure.Runtime.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace OLED_Sleeper.Infrastructure.Runtime
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
