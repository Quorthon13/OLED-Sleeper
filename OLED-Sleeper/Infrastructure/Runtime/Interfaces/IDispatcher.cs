namespace OLED_Sleeper.Infrastructure.Runtime.Interfaces
{
    /// <summary>
    /// Defines the contract for running an action on the UI thread from any thread.
    /// </summary>
    public interface IDispatcher
    {
        /// <summary>
        /// Gets true when the caller is already on the UI thread, false when it is on any other thread.
        /// </summary>
        bool IsOnUiThread { get; }

        /// <summary>
        /// Runs the action on the UI thread and waits for it to finish.
        /// Runs it directly when the caller is already on the UI thread.
        /// </summary>
        /// <param name="action">The action to run.</param>
        void Invoke(Action action);

        /// <summary>
        /// Hands the action to the UI thread and returns without waiting for it to run.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <returns>A task that completes once the action has finished.</returns>
        Task InvokeAsync(Action action);
    }
}
