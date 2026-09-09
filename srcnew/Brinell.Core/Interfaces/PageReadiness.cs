using System.Diagnostics;

namespace Brinell.Core.Interfaces;

/// <summary>
/// Defines whether a page must expose a page-local busy signal.
/// </summary>
public enum BusySignalPolicy
{
    Disabled,
    Required
}

/// <summary>
/// The last observable state returned by a raw page readiness probe.
/// </summary>
public enum PageReadinessState
{
    MissingRoot,
    StaleRoot,
    MissingBusySignal,
    InvalidBusySignal,
    Busy,
    Ready
}

/// <summary>
/// An instantaneous snapshot of page identity and activity.
/// </summary>
public readonly record struct PageReadinessSnapshot(
    string PageName,
    PageReadinessState State,
    BusySignalPolicy BusySignalPolicy,
    string? BusySignalValue = null,
    bool RootReacquired = false)
{
    public bool IsLoaded => State is not (PageReadinessState.MissingRoot or PageReadinessState.StaleRoot);
    public bool IsBusy => State == PageReadinessState.Busy;
    public bool IsReady => State == PageReadinessState.Ready;
}

/// <summary>
/// Classifies control operations for the automatic gate introduced after the
/// readiness contract is established.
/// </summary>
public enum ControlOperationKind
{
    Action,
    Set,
    Get,
    Assert,
    Wait,
    TransitionWait,
    Probe,
    Diagnostic
}

/// <summary>
/// A monotonic timeout budget shared by all stages of one operation.
/// </summary>
public readonly struct OperationDeadline
{
    private readonly long _startedAt;

    public OperationDeadline(TimeSpan timeout)
    {
        if (timeout < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout));

        Timeout = timeout;
        _startedAt = Stopwatch.GetTimestamp();
    }

    public TimeSpan Timeout { get; }
    public TimeSpan Elapsed => Stopwatch.GetElapsedTime(_startedAt);
    public TimeSpan Remaining
    {
        get
        {
            var remaining = Timeout - Elapsed;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }
    public bool IsExpired => Elapsed >= Timeout;
}

/// <summary>
/// Declares whether an operation uses ordinary page readiness or intentionally
/// observes/bypasses it.
/// </summary>
public enum PageOperationPolicy
{
    RequireReadyPage,
    ObserveTransition,
    BypassReadiness
}

/// <summary>Describes one public control operation before it enters the gate.</summary>
public readonly record struct ControlOperation(
    string Name,
    ControlOperationKind Kind,
    PageOperationPolicy Policy = PageOperationPolicy.RequireReadyPage);

/// <summary>
/// Async-flow-local state shared by nested helpers for one public operation.
/// Step 4 will use this to run the readiness gate once.
/// </summary>
public sealed class PageOperationContext
{
    private static readonly AsyncLocal<PageOperationContext?> CurrentContext = new();

    private PageOperationContext(
        PageOperationContext? parent,
        string pageName,
        ControlOperation operation,
        OperationDeadline deadline)
    {
        Parent = parent;
        PageName = pageName;
        Operation = operation;
        Deadline = deadline;
    }

    public static PageOperationContext? Current => CurrentContext.Value;
    public string PageName { get; }
    public ControlOperation Operation { get; }
    public OperationDeadline Deadline { get; }
    public bool ReadinessGateCompleted { get; set; }
    private PageOperationContext? Parent { get; }

    public static IDisposable Begin(
        string pageName,
        ControlOperation operation,
        OperationDeadline deadline)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageName);

        var context = new PageOperationContext(CurrentContext.Value, pageName, operation, deadline);
        CurrentContext.Value = context;
        return new ContextScope(context);
    }

    private sealed class ContextScope(PageOperationContext context) : IDisposable
    {
        private PageOperationContext? _context = context;

        public void Dispose()
        {
            var current = Interlocked.Exchange(ref _context, null);
            if (current != null && ReferenceEquals(CurrentContext.Value, current))
                CurrentContext.Value = current.Parent;
        }
    }
}