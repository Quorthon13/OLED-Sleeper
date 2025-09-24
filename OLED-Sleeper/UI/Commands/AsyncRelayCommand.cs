using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OLED_Sleeper.UI.Commands
{
    /// <summary>
    /// An asynchronous implementation of <see cref="ICommand"/> for WPF, supporting async/await and disabling while executing.
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        #region Fields

        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        #endregion Fields

        #region ICommand Implementation

        /// <inheritdoc />
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Command parameter (not used).</param>
        /// <returns>True if the command can execute; otherwise, false.</returns>
        public bool CanExecute(object? parameter)
        {
            return !_isExecuting && (_canExecute?.Invoke() ?? true);
        }

        /// <summary>
        /// Executes the command asynchronously.
        /// </summary>
        /// <param name="parameter">Command parameter (not used).</param>
        public async void Execute(object? parameter)
        {
            _isExecuting = true;
            CommandManager.InvalidateRequerySuggested();
            try
            {
                await _execute();
            }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        #endregion ICommand Implementation

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncRelayCommand"/> class.
        /// </summary>
        /// <param name="execute">The asynchronous action to execute.</param>
        /// <param name="canExecute">Predicate to determine if the command can execute.</param>
        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        #endregion Constructor
    }
}