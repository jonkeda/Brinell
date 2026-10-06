namespace Brinell.Bdd.Runner;

/// <summary>Where a Step-through run is paused: before a step, at a <c>## Step:</c> call before its first step, or after a failed step.</summary>
/// <param name="File">The step's or the call's file.</param>
/// <param name="Line">Its line.</param>
/// <param name="Text">The step or the call as written, with a call's values filled in.</param>
/// <param name="Depth">How many calls deep: 0 for a step written in the scenario, and for a call written there.</param>
/// <param name="IsCall">Whether it is a call: Step into goes inside without running anything.</param>
/// <param name="Calls">The calls around it, outermost first.</param>
/// <param name="Failed">For a stop after a failure, the failed step's result; null before a step.</param>
public sealed record BddRunnerStop(string? File, int Line, string Text, int Depth, bool IsCall, IReadOnlyList<BddRunnerCall> Calls, BddRunnerStep? Failed = null);

/// <summary>
/// Step through on a host (D1-D3 of the debugger plan): the host pauses between the runner's steps
/// and moves as an IDE's step commands do, as <see cref="BddBreakpoints"/> does in a debugged run.
/// Over at depth d runs to the next step at depth d or less, or to a call starting at level d + 1;
/// out of depth d is over at d - 1; into goes to the next position, inside a call; continue runs to
/// a breakpoint. Every move stops at a breakpoint and after a failed step.
/// </summary>
public sealed class BddRunnerStepper
{
    private readonly Func<CancellationToken, Task<BddRunnerStep>> _next;
    private readonly Func<string?, int, bool> _listed;
    private List<BddRunnerStop> _pending = [];
    private int _at;
    private int? _firstNew;
    private BddRunnerStop? _failed;

    /// <summary>Starts paused before the scenario's first step, or ended when it has none.</summary>
    /// <param name="first">Where the first step is (<see cref="BddRunnerOpened.Next"/>).</param>
    /// <param name="next">Runs the next step (<see cref="BddRunnerNext"/>).</param>
    /// <param name="listed">Whether a file's line has a breakpoint; read again at each step.</param>
    public BddRunnerStepper(BddRunnerPosition? first, Func<CancellationToken, Task<BddRunnerStep>> next, Func<string?, int, bool> listed)
    {
        _next = next;
        _listed = listed;
        if (first is not null)
        {
            Expect(first, []);
        }
    }

    /// <summary>Where the run is paused; null once it has ended.</summary>
    public BddRunnerStop? Paused => _failed ?? (_pending.Count > 0 ? _pending[_at] : null);

    /// <summary>The last step's result; null before the first.</summary>
    public BddRunnerStep? Last { get; private set; }

    /// <summary>Moves from the paused position; continue for null.</summary>
    /// <param name="move">Over, into or out; null to continue to a breakpoint, a failure or the end.</param>
    /// <param name="cancellationToken">Cancels the step that runs.</param>
    /// <returns>Where the run stops next; null when it ended.</returns>
    public async Task<BddRunnerStop?> MoveAsync(BddStepMove? move, CancellationToken cancellationToken = default)
    {
        var from = Paused ?? throw new InvalidOperationException("The run has ended.");

        // After a failure the next step's positions are not shown yet: a step move shows them.
        if (_failed is not null)
        {
            _failed = null;
            if (move is not null)
            {
                return Paused;
            }
        }
        else if (move == BddStepMove.Into && _at + 1 < _pending.Count)
        {
            _at++;
            return Paused;
        }

        var level = move switch
        {
            BddStepMove.Over => from.Depth,
            BddStepMove.Out => from.Depth - 1,
            _ => (int?)null,
        };

        while (true)
        {
            var step = await _next(cancellationToken).ConfigureAwait(false);
            Last = step;
            if (!step.HasNext || step.Next is null)
            {
                _pending = [];
                return null;
            }

            Expect(step.Next, step.Ran?.Calls ?? []);
            if (step.Status == BddStepResultStatus.Failed && step.Ran is { } ran)
            {
                _failed = new BddRunnerStop(ran.File, ran.Line, ran.Text, ran.Depth, false, ran.Calls, step);
                return _failed;
            }

            var listed = _pending.FindIndex(stop => _listed(stop.File, stop.Line));
            var fits = move == BddStepMove.Into || (level is { } at && (step.Next.Depth <= at || _firstNew <= at + 1));
            if (fits || listed >= 0)
            {
                _at = fits ? 0 : listed;
                return Paused;
            }
        }
    }

    // The positions before a step: each call it starts, outermost first, then the step.
    private void Expect(BddRunnerPosition step, IReadOnlyList<BddRunnerCall> previous)
    {
        var calls = step.Calls;
        _firstNew = null;
        for (var level = 1; level <= calls.Count; level++)
        {
            if (level > previous.Count || calls[level - 1] != previous[level - 1])
            {
                _firstNew = level;
                break;
            }
        }

        _pending = [];
        for (var level = _firstNew ?? calls.Count + 1; level <= calls.Count; level++)
        {
            var call = calls[level - 1];
            _pending.Add(new BddRunnerStop(call.File, call.Line, call.Text, level - 1, true, calls.Take(level - 1).ToList()));
        }

        _pending.Add(new BddRunnerStop(step.File, step.Line, step.Text, step.Depth, false, calls));
        _at = 0;
    }
}
