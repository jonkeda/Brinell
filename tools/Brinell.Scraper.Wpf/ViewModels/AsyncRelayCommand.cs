using System.Windows.Input;

namespace Brinell.Scraper.ViewModels;

public class AsyncRelayCommand : ViewModelBase, ICommand
{
    private readonly Func<CancellationToken, Task> _execute;
    private readonly Func<bool>? _canExecute;
    private CancellationTokenSource? _cts;
    private bool _isRunning;

    public AsyncRelayCommand(Func<CancellationToken, Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set => SetProperty(ref _isRunning, value);
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !IsRunning && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (IsRunning) return;

        _cts = new CancellationTokenSource();
        IsRunning = true;
        RaiseCanExecuteChanged();

        try
        {
            await _execute(_cts.Token);
        }
        finally
        {
            IsRunning = false;
            _cts.Dispose();
            _cts = null;
            RaiseCanExecuteChanged();
        }
    }

    public void Cancel() => _cts?.Cancel();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public class AsyncRelayCommand<T> : ViewModelBase, ICommand
{
    private readonly Func<T?, CancellationToken, Task> _execute;
    private readonly Func<T?, bool>? _canExecute;
    private CancellationTokenSource? _cts;
    private bool _isRunning;

    public AsyncRelayCommand(Func<T?, CancellationToken, Task> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set => SetProperty(ref _isRunning, value);
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !IsRunning && (_canExecute?.Invoke((T?)parameter) ?? true);

    public async void Execute(object? parameter)
    {
        if (IsRunning) return;

        _cts = new CancellationTokenSource();
        IsRunning = true;
        RaiseCanExecuteChanged();

        try
        {
            await _execute((T?)parameter, _cts.Token);
        }
        finally
        {
            IsRunning = false;
            _cts.Dispose();
            _cts = null;
            RaiseCanExecuteChanged();
        }
    }

    public void Cancel() => _cts?.Cancel();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
