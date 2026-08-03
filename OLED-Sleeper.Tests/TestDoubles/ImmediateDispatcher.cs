using OLED_Sleeper.Core.Interfaces;

namespace OLED_Sleeper.Tests.TestDoubles
{
    /// <summary>
    /// An <see cref="IDispatcher"/> that runs actions on the calling thread and counts them, so a test
    /// can assert work was handed to the UI thread without needing a real one.
    /// </summary>
    public class ImmediateDispatcher : IDispatcher
    {
        /// <summary>
        /// How many actions have been run through <see cref="Invoke"/>.
        /// </summary>
        public int InvokeCount { get; private set; }

        /// <summary>
        /// How many actions have been run through <see cref="InvokeAsync"/>.
        /// </summary>
        public int InvokeAsyncCount { get; private set; }

        /// <inheritdoc />
        public bool IsOnUiThread => true;

        /// <inheritdoc />
        public void Invoke(Action action)
        {
            InvokeCount++;
            action();
        }

        /// <inheritdoc />
        public Task InvokeAsync(Action action)
        {
            InvokeAsyncCount++;
            action();
            return Task.CompletedTask;
        }
    }
}
