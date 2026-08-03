namespace OLED_Sleeper.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for managing single-instance enforcement and inter-process signaling.
    /// </summary>
    public interface IApplicationInstanceManager : IDisposable
    {
        /// <summary>
        /// Indicates whether this is the first instance of the application.
        /// </summary>
        bool IsFirstInstance { get; }

        /// <summary>
        /// Initializes the single-instance check and event signaling.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Sets the delegate to show the main window when signaled by a second instance.
        /// Should be called after DI and services are initialized.
        /// </summary>
        /// <param name="showMainWindowAction">Action to show the main window.</param>
        void SetShowMainWindowAction(Action showMainWindowAction);
    }
}