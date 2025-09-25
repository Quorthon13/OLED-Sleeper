using OLED_Sleeper.Core.Interfaces;
using Serilog;
using System.Collections.Concurrent;
using System.Reflection;

namespace OLED_Sleeper.Core
{
    /// <summary>
    /// A simple mediator implementation that uses a service provider to resolve and execute command handlers.
    /// This implementation caches the MethodInfo for each handler's Handle method to improve performance by avoiding repeated reflection lookups.
    /// </summary>
    public class Mediator : IMediator
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Caches the MethodInfo for the Handle method of each ICommandHandler type.
        /// This avoids repeated reflection lookups for each command dispatch.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, MethodInfo> _handleMethodCache = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="Mediator"/> class.
        /// </summary>
        /// <param name="serviceProvider">The dependency injection service provider used to resolve handlers.</param>
        public Mediator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Asynchronously sends a command to its registered handler.
        /// Uses a static cache to store MethodInfo for each handler's Handle method for performance.
        /// The handler is resolved via dependency injection and invoked using reflection.
        /// </summary>
        /// <param name="command">The command to be handled.</param>
        /// <exception cref="InvalidOperationException">Thrown if no handler is registered for the given command type.</exception>
        public async Task SendAsync(ICommand command)
        {
            // Construct the specific handler type we need to resolve.
            // For a command of type ApplyBlackoutCommand, this will create the type ICommandHandler<ApplyBlackoutCommand>.
            var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());

            // Use the service provider to get an instance of the required handler.
            var handler = _serviceProvider.GetService(handlerType);

            if (handler == null)
            {
                var errorMessage = $"No handler registered for command type {command.GetType().Name}. " +
                                   $"Ensure that a class implementing ICommandHandler<{command.GetType().Name}> is registered with the dependency injection container.";
                Log.Error(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            // Cache the MethodInfo for the Handle method of each handler type to avoid repeated reflection.
            var method = _handleMethodCache.GetOrAdd(
                handlerType,
                t => t.GetMethod(nameof(ICommandHandler<ICommand>.HandleAsync)) ?? throw new InvalidOperationException()
            );
            await (Task)method.Invoke(handler, new object[] { command });
        }
    }
}