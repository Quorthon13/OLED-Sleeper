using Microsoft.Extensions.DependencyInjection;
using OLED_Sleeper.Core.Interfaces;

namespace OLED_Sleeper.Core
{
    public class Mediator(IServiceProvider serviceProvider) : IMediator
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        /// <inheritdoc />
        public async Task SendAsync<TCommand>(TCommand command) where TCommand : ICommand
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var handler = _serviceProvider.GetService<ICommandHandler<TCommand>>();
            if (handler == null)
            {
                var errorMessage = $"No handler registered for command type {typeof(TCommand).Name}.";
                Serilog.Log.Error(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            await handler.HandleAsync(command);
        }
    }
}