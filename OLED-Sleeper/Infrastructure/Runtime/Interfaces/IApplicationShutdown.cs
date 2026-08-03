namespace OLED_Sleeper.Infrastructure.Runtime.Interfaces
{
    /// <summary>
    /// Defines the contract for ending the process.
    /// </summary>
    public interface IApplicationShutdown
    {
        /// <summary>
        /// Asks the application to exit. Returns once the request is posted, before the process has
        /// torn down, so anything that must happen first has to run before the call.
        /// </summary>
        void Shutdown();
    }
}
