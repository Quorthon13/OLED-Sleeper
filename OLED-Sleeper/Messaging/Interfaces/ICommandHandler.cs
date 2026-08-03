namespace OLED_Sleeper.Messaging.Interfaces
{
    /// <summary>
    /// Defines a generic handler for a command.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to be handled.</typeparam>
    public interface ICommandHandler<TCommand> where TCommand : ICommand
    {
        /// <summary>
        /// Handles the specified command asynchronously.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        Task HandleAsync(TCommand command);
    }
}