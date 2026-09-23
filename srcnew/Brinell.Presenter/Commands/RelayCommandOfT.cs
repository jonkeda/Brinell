using System.Windows.Input;

namespace Brinell.Presenter.Commands;

public sealed class RelayCommand<T> : ICommand
{
    private readonly Predicate<T?>? _canExecute;
    private readonly Action<T?> _execute;

    public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke(Coerce(parameter)) ?? true;
    }

    public void Execute(object? parameter)
    {
        _execute(Coerce(parameter));
    }

    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private static T? Coerce(object? parameter)
    {
        return parameter is T value ? value : default;
    }
}
