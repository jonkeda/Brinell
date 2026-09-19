namespace Brinell.Maui.Calls;

/// <summary>
/// One public call on a control or scope: one log entry/exit pair around the whole of it, the
/// action included (<c>.my/stale-readiness/design.md</c>, R1).
/// </summary>
/// <remarks>
/// <para>
/// There is no page gate here: readiness is the first step of every attempt, inside the call's
/// one poll, so it shares the call's one budget.
/// </para>
/// <para>
/// A call that succeeds only after trouble is still reported (R0): when the phase's attempts saw
/// the element replaced <see cref="NearMissReplacements"/> times or more, or used more than
/// <see cref="NearMissBudgetShare"/> of the budget, the exit is logged as
/// <see cref="LogResult.Warning"/> with a "near-miss:" summary. A UI that keeps re-rendering, or a
/// page slow to become idle, then shows up in the log although the test passed.
/// </para>
/// </remarks>
internal sealed class ControlCall
{
    /// <summary>Replacements at or above which a successful call is a near-miss (X5).</summary>
    public const int NearMissReplacements = 3;

    /// <summary>The share of the budget above which a successful call is a near-miss (X5).</summary>
    public const double NearMissBudgetShare = 0.5;

    private const string TestName = "Test";

    private readonly ITestLogger? _logger;
    private readonly string _pageName;
    private readonly string _controlId;

    public ControlCall(ITestLogger? logger, string pageName, string controlId)
    {
        _logger = logger;
        _pageName = pageName;
        _controlId = controlId;
    }

    /// <summary>
    /// Runs <paramref name="body"/> as one call: logs its entry, gives it a fresh
    /// <see cref="AttemptContext"/> with a <paramref name="budgetMs"/> deadline, and logs its exit.
    /// </summary>
    /// <param name="caller">The public member's name, for the log.</param>
    /// <param name="value">The value the member was given, for the log.</param>
    /// <param name="budgetMs">The call's budget: the caller's timeout, or the default wait.</param>
    /// <param name="animationMs">The scroll throttle interval.</param>
    /// <param name="body">The call. Its first phase polls with the context it is given.</param>
    /// <param name="succeeded">For a call that answers false rather than throwing (a <c>Wait*</c>): whether the answer counts as success in the log.</param>
    public T Run<T>(
        string caller,
        string? value,
        int budgetMs,
        int animationMs,
        Func<AttemptContext, T> body,
        Func<T, bool>? succeeded = null)
    {
        var clock = Stopwatch.StartNew();
        var context = new AttemptContext(Deadline.In(budgetMs), animationMs);
        _logger?.LogEntry(TestName, _pageName, _controlId, caller, value);

        T result;
        try
        {
            result = body(context);
        }
        catch (Exception error)
        {
            _logger?.LogExit(TestName, _pageName, _controlId, caller,
                LogResult.Error, (int)clock.ElapsedMilliseconds, error.Message);
            throw;
        }

        var elapsed = (int)clock.ElapsedMilliseconds;
        if (succeeded != null && !succeeded(result))
        {
            _logger?.LogExit(TestName, _pageName, _controlId, caller,
                LogResult.Error, elapsed, context.Log.Summary());
        }
        else if (IsNearMiss(context))
        {
            _logger?.LogExit(TestName, _pageName, _controlId, caller,
                LogResult.Warning, elapsed, $"near-miss: {context.Log.Summary()} (budget {budgetMs} ms)");
        }
        else
        {
            _logger?.LogExit(TestName, _pageName, _controlId, caller, LogResult.Success, elapsed);
        }

        return result;
    }

    private static bool IsNearMiss(AttemptContext context)
    {
        if (context.Log.Replacements >= NearMissReplacements)
        {
            return true;
        }

        // Judged on the poll alone: an action's confirmation has a budget of its own (R3).
        var budget = context.Deadline.BudgetMs;
        return budget > 0
               && context.Log.Attempts > 1
               && context.Log.LastAtMs > budget * NearMissBudgetShare;
    }
}
