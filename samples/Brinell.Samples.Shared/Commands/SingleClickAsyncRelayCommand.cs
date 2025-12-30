using Brinell.Samples.Shared.ViewModels;

namespace Brinell.Samples.Shared.Commands;

/// <summary>
/// Specialized command for single-click async operations with protection
/// against double-taps and execution during view transitions.
/// </summary>
public class SingleClickAsyncRelayCommand : AsyncRelayCommand
{
    /// <summary>
    /// Creates a SingleClickAsyncRelayCommand.
    /// </summary>
    /// <param name="viewModel">ViewModel for view visibility and busy tracking.</param>
    /// <param name="execute">The async operation to execute.</param>
    /// <param name="canExecute">Optional predicate for command availability.</param>
    /// <param name="runOnce">If true, command executes only once until Reset() called.</param>
    public SingleClickAsyncRelayCommand(
        IViewVisible viewModel,
        Func<Task> execute,
        Func<bool>? canExecute = null,
        bool runOnce = false)
        : base(
            viewModel,
            execute,
            canExecute,
            runOnce ? AsyncRelayCommandOptions.OnceOnly : AsyncRelayCommandOptions.None)
    {
    }
}

/// <summary>
/// Generic version with parameter support.
/// </summary>
public class SingleClickAsyncRelayCommand<T> : AsyncRelayCommand<T>
{
    /// <summary>
    /// Creates a SingleClickAsyncRelayCommand with parameter support.
    /// </summary>
    /// <param name="viewModel">ViewModel for view visibility and busy tracking.</param>
    /// <param name="execute">The async operation to execute.</param>
    /// <param name="canExecute">Optional predicate for command availability.</param>
    /// <param name="runOnce">If true, command executes only once until Reset() called.</param>
    public SingleClickAsyncRelayCommand(
        IViewVisible viewModel,
        Func<T?, Task> execute,
        Predicate<T?>? canExecute = null,
        bool runOnce = false)
        : base(
            viewModel,
            execute,
            canExecute,
            runOnce ? AsyncRelayCommandOptions.OnceOnly : AsyncRelayCommandOptions.None)
    {
    }
}
