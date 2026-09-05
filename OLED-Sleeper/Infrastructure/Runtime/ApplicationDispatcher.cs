using OLED_Sleeper.Infrastructure.Runtime.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Threading;

namespace OLED_Sleeper.Infrastructure.Runtime
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

        /// <inheritdoc />
        public async Task InvokeAfterInputAsync(Action action) =>
            await Application.Current.Dispatcher.InvokeAsync(action, DispatcherPriority.Background);
    }
}
