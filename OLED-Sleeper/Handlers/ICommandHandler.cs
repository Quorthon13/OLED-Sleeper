using OLED_Sleeper.Core.Interfaces;

namespace OLED_Sleeper.Handlers
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
        Task Handle(TCommand command);
    }
}