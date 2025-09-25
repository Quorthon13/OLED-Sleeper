using Microsoft.Extensions.DependencyInjection;
using OLED_Sleeper.Core.Interfaces;

namespace OLED_Sleeper.Core
{
    public class Mediator : IMediator
    {
        private readonly IServiceProvider _serviceProvider;

        public Mediator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task SendAsync<TCommand>(TCommand command) where TCommand : ICommand
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            // Resolve the strongly-typed handler
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