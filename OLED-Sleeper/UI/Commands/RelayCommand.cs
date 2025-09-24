using System;
using System.Windows.Input;

namespace OLED_Sleeper.UI.Commands
{
    public class RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        : ICommand
    {
        private readonly Action<object?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));

        // Overload for parameter-less delegates
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
            : this(p => execute(), p => canExecute?.Invoke() ?? true)
        {
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return canExecute == null || canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }
    }
}