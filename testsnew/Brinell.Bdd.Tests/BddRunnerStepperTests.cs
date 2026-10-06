using Brinell.Bdd.Runner;
using Xunit;

namespace Brinell.Bdd.Tests;

/// <summary>Step through on a host: the moves over a runner's steps, with a fake runner.</summary>
public sealed class BddRunnerStepperTests
{
    private const string Feature = "Shelf.feature.md";
    private const string Steps = "Shelf.steps.md";
    private static readonly BddRunnerCall Call = new("the shelf holds \"Dune\"", Feature, 11, 1);

    // A scenario: a step, a call of two steps, two steps.
    private static readonly BddRunnerPosition[] Scenario =
    [
        new(Feature, 10, "Given an empty shelf", 0, []),
        new(Steps, 5, "When I shelve \"Dune\"", 1, [Call]),
        new(Steps, 6, "And I dust the shelf", 1, [Call]),
        new(Feature, 12, "Then the shelf holds 1 book", 0, []),
        new(Feature, 13, "And the shelf is clean", 0, []),
    ];

    private readonly HashSet<(string, int)> _breakpoints = [];
    private int _ran;
    private int? _fails;

    [Fact]
    public void ItStarts_BeforeTheFirstStep()
    {
        var stepper = Stepper();

        Assert.Equal((Feature, 10, false), At(stepper.Paused));
        Assert.Equal(0, _ran);
    }

    [Fact]
    public async Task Over_StopsAtACall_ThenRunsItWhole()
    {
        var stepper = Stepper();

        Assert.Equal((Feature, 11, true), At(await stepper.MoveAsync(BddStepMove.Over)));
        Assert.Equal(1, _ran);
        Assert.Equal((Feature, 12, false), At(await stepper.MoveAsync(BddStepMove.Over)));
        Assert.Equal(3, _ran);
    }

    [Fact]
    public async Task Into_GoesInsideACall_WithoutRunningAnything_AndOut_LeavesIt()
    {
        var stepper = Stepper();
        await stepper.MoveAsync(BddStepMove.Over);

        var inside = await stepper.MoveAsync(BddStepMove.Into);
        Assert.Equal((Steps, 5, false), At(inside));
        Assert.Equal(1, inside!.Depth);
        Assert.Equal([Call], inside.Calls);
        Assert.Equal(1, _ran);

        Assert.Equal((Steps, 6, false), At(await stepper.MoveAsync(BddStepMove.Over)));
        Assert.Equal((Feature, 12, false), At(await stepper.MoveAsync(BddStepMove.Out)));
        Assert.Equal(3, _ran);
    }

    [Fact]
    public async Task Continue_StopsAtABreakpoint_InsideACall_AndAtACallsLine()
    {
        _breakpoints.Add((Steps, 6));
        var stepper = Stepper();

        Assert.Equal((Steps, 6, false), At(await stepper.MoveAsync(null)));

        _breakpoints.Clear();
        _breakpoints.Add((Feature, 11));
        stepper = Stepper();
        Assert.Equal((Feature, 11, true), At(await stepper.MoveAsync(null)));
    }

    [Fact]
    public async Task Over_StopsAtABreakpoint_InTheCallItRuns()
    {
        _breakpoints.Add((Steps, 6));
        var stepper = Stepper();
        await stepper.MoveAsync(BddStepMove.Over);

        Assert.Equal((Steps, 6, false), At(await stepper.MoveAsync(BddStepMove.Over)));
    }

    [Fact]
    public async Task Continue_RunsToTheEnd()
    {
        var stepper = Stepper();

        Assert.Null(await stepper.MoveAsync(null));

        Assert.Null(stepper.Paused);
        Assert.Equal(5, _ran);
        Assert.False(stepper.Last!.HasNext);
        await Assert.ThrowsAsync<InvalidOperationException>(() => stepper.MoveAsync(BddStepMove.Over));
    }

    [Fact]
    public async Task AFailedStep_Stops_ThenAStepMoveShowsTheNextStep()
    {
        _fails = 3;
        var stepper = Stepper();

        var failed = await stepper.MoveAsync(null);
        Assert.Equal((Feature, 12, false), At(failed));
        Assert.Equal(BddStepResultStatus.Failed, failed!.Failed!.Status);

        Assert.Equal((Feature, 13, false), At(await stepper.MoveAsync(BddStepMove.Over)));
        Assert.Equal(4, _ran);
    }

    private BddRunnerStepper Stepper()
    {
        _ran = 0;
        return new BddRunnerStepper(Scenario[0], _ => Task.FromResult(Next()), (file, line) => _breakpoints.Contains((file!, line)));
    }

    private BddRunnerStep Next()
    {
        var index = _ran++;
        var hasNext = index + 1 < Scenario.Length;
        return new BddRunnerStep(
            index,
            index == _fails ? BddStepResultStatus.Failed : BddStepResultStatus.Passed,
            null,
            hasNext,
            [],
            [],
            Scenario[index],
            hasNext ? Scenario[index + 1] : null);
    }

    private static (string?, int, bool)? At(BddRunnerStop? stop) => stop is null ? null : (stop.File, stop.Line, stop.IsCall);
}
