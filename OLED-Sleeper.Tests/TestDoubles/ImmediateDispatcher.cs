using OLED_Sleeper.Infrastructure.Runtime.Interfaces;

namespace OLED_Sleeper.Tests.TestDoubles
{
    /// <summary>
    /// An <see cref="IDispatcher"/> that runs actions on the calling thread and counts them, so a test
    /// can assert work was handed to the UI thread without needing a real one.
    /// </summary>
    public class ImmediateDispatcher : IDispatcher
    {
        /// <summary>
        /// How deep invokes may nest before <see cref="Run"/> throws.
        /// </summary>
        private const int MaxNestedInvokes = 8;

        private int _nestedInvokes;

        /// <summary>
        /// How many actions have been run through <see cref="Invoke"/>.
        /// </summary>
        public int InvokeCount { get; private set; }

        /// <summary>
        /// How many actions have been run through <see cref="InvokeAsync"/>.
        /// </summary>
        public int InvokeAsyncCount { get; private set; }

        /// <summary>
        /// How many actions have been run through <see cref="InvokeAfterInputAsync"/>.
        /// </summary>
        public int InvokeAfterInputCount { get; private set; }

        /// <summary>
        /// Whether the caller counts as being on the UI thread. Starts true; set it to false to make a
        /// class under test take its marshalling branch. Reads true while an action is running.
        /// </summary>
        public bool IsOnUiThread { get; set; } = true;

        /// <inheritdoc />
        public void Invoke(Action action)
        {
            InvokeCount++;
            Run(action);
        }

        /// <inheritdoc />
        public Task InvokeAsync(Action action)
        {
            InvokeAsyncCount++;
            Run(action);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task InvokeAfterInputAsync(Action action)
        {
            InvokeAfterInputCount++;
            Run(action);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Runs the action as if on the UI thread. Throws when invokes nest more than
        /// <see cref="MaxNestedInvokes"/> deep, which fails the test instead of overflowing the stack.
        /// </summary>
        /// <param name="action">The action to run.</param>
        private void Run(Action action)
        {
            if (_nestedInvokes >= MaxNestedInvokes)
            {
                throw new InvalidOperationException($"Dispatcher invokes nested more than {MaxNestedInvokes} deep.");
            }

            bool wasOnUiThread = IsOnUiThread;
            _nestedInvokes++;
            IsOnUiThread = true;

            try
            {
                action();
            }
            finally
            {
                _nestedInvokes--;
                IsOnUiThread = wasOnUiThread;
            }
        }
    }
}
