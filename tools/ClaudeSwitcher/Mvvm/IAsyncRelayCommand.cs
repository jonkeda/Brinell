using System.ComponentModel;

namespace ClaudeSwitcher.Mvvm;

public interface IAsyncRelayCommand : IRelayCommand, INotifyPropertyChanged
{
    Task? ExecutionTask { get; }
    bool IsRunning { get; }
    bool CanBeCanceled { get; }
    bool IsCancellationRequested { get; }
    Task ExecuteAsync(object? parameter);
    void Cancel();
}
