using System.Diagnostics;
using Brinell.Core.Logging;
using Brinell.Html.Context;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Calls;

/// <summary>
/// One public call on a control or scope: one log entry/exit pair around the whole of it, the
/// action included (R1). The log line format is MAUI's, so one log analyser reads both stacks.
/// </summary>
internal sealed class ControlCall
{
    private readonly ITestLogger? _logger;
    private readonly string _testName;
    private readonly string _pageName;
    private readonly string _controlId;
    private readonly NearMissSettings _nearMiss;

    public ControlCall(IHtmlTestContext context, string pageName, string controlId)
    {
        _logger = context.Logger;
        // The context's name, so a scenario with several apps can tell their calls apart (?A10).
        _testName = context.Name ?? "Test";
        _pageName = pageName;
        _controlId = controlId;
        _nearMiss = context.NearMiss ?? NearMissSettings.Default;
    }

    /// <summary>
    /// Runs <paramref name="body"/> as one call: logs its entry, gives it a fresh
    /// <see cref="AttemptContext"/> with a <paramref name="budgetMs"/> deadline, and logs its exit.
    /// <paramref name="succeeded"/> is for a call that answers false rather than throwing (a
    /// <c>Wait*</c>): whether the answer counts as success in the log.
    /// </summary>
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
        _logger?.LogEntry(_testName, _pageName, _controlId, caller, value);

        var outer = AttemptContext.Current;
        AttemptContext.Current = context;
        T result;
        try
        {
            result = body(context);
        }
        catch (Exception error)
        {
            _logger?.LogExit(_testName, _pageName, _controlId, caller,
                LogResult.Error, (int)clock.ElapsedMilliseconds, error.Message);
            throw;
        }
        finally
        {
            AttemptContext.Current = outer;
        }

        var elapsed = (int)clock.ElapsedMilliseconds;
        if (succeeded != null && !succeeded(result))
        {
            _logger?.LogExit(_testName, _pageName, _controlId, caller,
                LogResult.Error, elapsed, context.Log.Summary());
        }
        else if (IsNearMiss(context))
        {
            _logger?.LogExit(_testName, _pageName, _controlId, caller,
                LogResult.Warning, elapsed, $"near-miss: {context.Log.Summary()} (budget {budgetMs} ms)");
        }
        else
        {
            _logger?.LogExit(_testName, _pageName, _controlId, caller, LogResult.Success, elapsed);
        }

        return result;
    }

    private bool IsNearMiss(AttemptContext context)
    {
        if (context.Log.Replacements >= _nearMiss.Replacements)
        {
            return true;
        }

        // Judged on the poll alone: an action's confirmation has a budget of its own (R3).
        var budget = context.Deadline.BudgetMs;
        return budget > 0
               && context.Log.Attempts > 1
               && context.Log.LastAtMs > budget * _nearMiss.BudgetShare;
    }
}
