using OLED_Sleeper.Core.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace OLED_Sleeper.Infrastructure
{
    /// <summary>
    /// Runs actions on the WPF UI thread owned by <see cref="Application.Current"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ApplicationDispatcher : IDispatcher
    {
        /// <inheritdoc />
        public bool IsOnUiThread => Application.Current.Dispatcher.CheckAccess();

        /// <inheritdoc />
        public void Invoke(Action action) => Application.Current.Dispatcher.Invoke(action);

        /// <inheritdoc />
        public async Task InvokeAsync(Action action) =>
            await Application.Current.Dispatcher.InvokeAsync(action);
    }
}
