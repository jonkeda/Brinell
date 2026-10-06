namespace Brinell.Presenter.ViewModels;

/// <summary>A move from the row a run is paused before.</summary>
public enum PresenterMove
{
    /// <summary>Runs the row: a step, or a <c>## Step:</c> call whole. Stops before the next row.</summary>
    Step,

    /// <summary>At a call, goes inside it without running anything.</summary>
    Into,

    /// <summary>Inside a call, runs the rest of it and stops after it.</summary>
    Out,

    /// <summary>Runs on until a breakpoint, a failure or the end.</summary>
    Play,
}

/// <summary>What a move runs: steps until <see cref="Until"/> have run, then it pauses.</summary>
/// <param name="Until">How many of the scenario's steps have run when the move ends.</param>
/// <param name="CheckBreaks">Whether a breakpoint on the way stops it.</param>
/// <param name="StopRow">The row it pauses at; null for the row after the last step it ran.</param>
public readonly record struct StepPlan(int Until, bool CheckBreaks, int? StopRow);

/// <summary>
/// Where the moves go in a scenario's step list: the rows are the places a run can pause, and a
/// <c>## Step:</c> call is a place of its own (as in the engine's stepping, AD-022).
/// </summary>
public static class PresenterStepping
{
    /// <summary>The plan of a move from row <paramref name="at"/>; null when the move does not apply there.</summary>
    /// <param name="rows">The step list.</param>
    /// <param name="at">The row the run is paused before.</param>
    /// <param name="move">The move.</param>
    public static StepPlan? Plan(IReadOnlyList<BddStepRowViewModel> rows, int at, PresenterMove move)
    {
        if (at < 0 || at >= rows.Count)
        {
            return null;
        }

        var row = rows[at];
        return move switch
        {
            PresenterMove.Step => new StepPlan(row.IsCall ? row.EndStep : row.FirstStep + 1, false, null),
            PresenterMove.Into when row.IsCall => new StepPlan(row.FirstStep, false, at + 1),
            PresenterMove.Out when CallOf(rows, at) is { } call => new StepPlan(rows[call].EndStep, false, null),
            PresenterMove.Play => new StepPlan(int.MaxValue, true, null),
            _ => null,
        };
    }

    /// <summary>Run until here: from row <paramref name="at"/> on to row <paramref name="target"/>, pausing before it; null when it is not ahead.</summary>
    /// <param name="rows">The step list.</param>
    /// <param name="at">The row the run is paused before.</param>
    /// <param name="target">The row to pause at.</param>
    public static StepPlan? PlanUntil(IReadOnlyList<BddStepRowViewModel> rows, int at, int target)
        => target > at && target < rows.Count ? new StepPlan(rows[target].FirstStep, false, target) : null;

    /// <summary>
    /// The row to pause at once <paramref name="completed"/> steps have run, after row
    /// <paramref name="after"/>: a call's row before its first step, else the step's; the end when none.
    /// </summary>
    /// <param name="rows">The step list.</param>
    /// <param name="completed">How many steps have run.</param>
    /// <param name="after">The row the move started from.</param>
    public static int PositionAfter(IReadOnlyList<BddStepRowViewModel> rows, int completed, int after)
    {
        for (var index = after + 1; index < rows.Count; index++)
        {
            if (rows[index].FirstStep >= completed)
            {
                return index;
            }
        }

        return rows.Count;
    }

    /// <summary>The row a breakpoint stops at before step <paramref name="step"/> runs: its call's, when the step starts a call with one; else its own; null when none.</summary>
    /// <param name="rows">The step list.</param>
    /// <param name="step">The step about to run.</param>
    public static BddStepRowViewModel? BreakBefore(IReadOnlyList<BddStepRowViewModel> rows, int step)
        => rows.FirstOrDefault(row => row.IsCall && row.FirstStep == step && row.HasBreakpoint)
           ?? rows.FirstOrDefault(row => !row.IsCall && row.FirstStep == step && row.HasBreakpoint);

    /// <summary>
    /// Whether the next step is written in a step file (a <c>## Step:</c> body) on a line an IDE set a
    /// breakpoint on: the runner says where it is (AD-022). The feature file's own
    /// lines are the rows' breakpoints (<see cref="BreakBefore"/>).
    /// </summary>
    /// <param name="next">Where the next step is, as the runner says.</param>
    /// <param name="scenarioFile">The scenario's feature file.</param>
    /// <param name="breakpointsPath">The breakpoints file.</param>
    public static bool BreaksInStepFile(Brinell.Bdd.Runner.BddRunnerPosition? next, string scenarioFile, string breakpointsPath)
        => next is { File: { } file }
           && !string.Equals(Path.GetFullPath(file), Path.GetFullPath(scenarioFile), StringComparison.OrdinalIgnoreCase)
           && BddBreakpoints.Lists(file, next.Line, breakpointsPath);

    /// <summary>The call a row is inside, as its row index; null for a row outside every call, and for a call's own row.</summary>
    /// <param name="rows">The step list.</param>
    /// <param name="at">The row.</param>
    public static int? CallOf(IReadOnlyList<BddStepRowViewModel> rows, int at)
    {
        if (!rows[at].IsInCall)
        {
            return null;
        }

        for (var index = at - 1; index >= 0; index--)
        {
            if (rows[index].IsCall)
            {
                return index;
            }
        }

        return null;
    }
}
