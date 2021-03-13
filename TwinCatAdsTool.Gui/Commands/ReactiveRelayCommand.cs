using System;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Windows.Input;

namespace TwinCatAdsTool.Gui.Commands
{
    public class ReactiveRelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public ReactiveRelayCommand(Action<object> execute)
            : this(execute, null)
        {
        }

        public ReactiveRelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            _execute = execute;
            _canExecute = canExecute;
        }

        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
            _executed.OnNext(parameter);
        }

        private readonly Subject<object> _executed = new Subject<object>();

        public IObservable<object> Executed => _executed;
    }
}
