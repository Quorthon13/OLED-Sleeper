namespace OLED_Sleeper.Core.Interfaces
{
    /// <summary>
    /// Defines a generic handler for a command.
    /// </summary>
    /// <typeparam name="ICommand">The type of command to be handled.</typeparam>
    public interface ICommandHandler<ICommand>
    {
        /// <summary>
        /// Handles the specified command asynchronously.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        Task HandleAsync(ICommand command);
    }
}