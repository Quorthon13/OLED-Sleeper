namespace OLED_Sleeper.Core.Interfaces
{
    /// <summary>
    /// Defines a mediator to decouple command senders from their handlers.
    /// </summary>
    public interface IMediator
    {
        /// <summary>
        /// Asynchronously sends a command to be handled by its corresponding handler.
        /// </summary>
        /// <param name="command">The command object.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task SendAsync<TCommand>(TCommand command) where TCommand : ICommand;
    }
}