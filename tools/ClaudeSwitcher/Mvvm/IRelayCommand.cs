using System.Windows.Input;

namespace ClaudeSwitcher.Mvvm;

public interface IRelayCommand : ICommand
{
    void NotifyCanExecuteChanged();
}

public interface IRelayCommand<in T> : IRelayCommand
{
    bool CanExecute(T? parameter);
    void Execute(T? parameter);
}
