using System.ComponentModel;
using System.Diagnostics;

namespace ClaudeSwitcher.Mvvm;

public class AsyncRelayCommand : IAsyncRelayCommand
{
    private readonly IViewVisible? _viewModel;
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private readonly AsyncRelayCommandOptions _options;
    private Task? _executionTask;
    private bool _hasExecuted;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? CanExecuteChanged;

    public Task? ExecutionTask
    {
        get => _executionTask;
        private set
        {
            if (ReferenceEquals(_executionTask, value)) return;
            _executionTask = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExecutionTask)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRunning)));
        }
    }

    public bool IsRunning => ExecutionTask is { IsCompleted: false };
    public bool CanBeCanceled => false;
    public bool IsCancellationRequested => false;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null,
        AsyncRelayCommandOptions options = AsyncRelayCommandOptions.None)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
        _options = options;
    }

    public AsyncRelayCommand(IViewVisible viewModel, Func<Task> execute,
        Func<bool>? canExecute = null,
        AsyncRelayCommandOptions options = AsyncRelayCommandOptions.None)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
        _options = options;
    }

    public bool CanExecute(object? parameter)
    {
        if (IsRunning && !HasOption(AsyncRelayCommandOptions.AllowConcurrentExecutions))
            return false;
        return _canExecute?.Invoke() ?? true;
    }

    public async void Execute(object? parameter) => await ExecuteAsync(parameter);

    public void NotifyCanExecuteChanged()
        => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    public async Task ExecuteAsync(object? parameter)
    {
        if (!CanExecute(parameter)) return;

        if (_viewModel != null && !_viewModel.ViewVisible) return;

        if (HasOption(AsyncRelayCommandOptions.OnceOnly) && _hasExecuted) return;
        _hasExecuted = true;

        var trackBusy = _viewModel != null && !HasOption(AsyncRelayCommandOptions.SkipBusyTracking);
        if (trackBusy) _viewModel!.BeginBusy();

        try
        {
            ExecutionTask = _execute();
            NotifyCanExecuteChanged();
            await ExecutionTask;
        }
        catch (Exception ex)
        {
            if (!HasOption(AsyncRelayCommandOptions.FlowExceptionsToTaskScheduler))
                throw;
            Debug.WriteLine($"[AsyncRelayCommand] Error: {ex.Message}");
        }
        finally
        {
            if (trackBusy) _viewModel!.EndBusy();
            NotifyCanExecuteChanged();
        }
    }

    public void Cancel() { }

    public void Reset()
    {
        _hasExecuted = false;
        NotifyCanExecuteChanged();
    }

    private bool HasOption(AsyncRelayCommandOptions option) => (_options & option) != 0;
}
