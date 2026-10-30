using Bruinet.Core.Recording;

namespace Bruinet.Core.Tests;

[CollectionDefinition(nameof(ActionPacingTests), DisableParallelization = true)]
public sealed class ActionPacingCollection;

/// <summary>
/// The action pause of the scenes recording now. Two scenes may record at once (scenarios in two
/// collections), and end in either order: the pause stays while any of them records.
/// </summary>
[Collection(nameof(ActionPacingTests))]
public sealed class ActionPacingTests
{
    private static readonly TimeSpan Short = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan Long = TimeSpan.FromMilliseconds(700);

    [Fact]
    public void WithNothingRecording_ThereIsNoPause()
        => Assert.Null(ActionPacing.Pause);

    [Fact]
    public void TwoScenesThatOverlap_KeepThePauseUntilTheLastEnds_WhicheverEndsFirst()
    {
        var first = ActionPacing.Begin(Long);
        var second = ActionPacing.Begin(Long);

        first.Dispose();
        Assert.Equal(Long, ActionPacing.Pause);

        second.Dispose();
        Assert.Null(ActionPacing.Pause);
    }

    [Fact]
    public void TwoScenesWithTheirOwnPause_TheLongerHolds_AndTheOtherIsLeftWhenItEnds()
    {
        using (ActionPacing.Begin(Short))
        {
            var longer = ActionPacing.Begin(Long);
            Assert.Equal(Long, ActionPacing.Pause);

            longer.Dispose();
            Assert.Equal(Short, ActionPacing.Pause);
        }

        Assert.Null(ActionPacing.Pause);
    }

    [Fact]
    public void AScopeDisposedTwice_EndsOnlyItself()
    {
        using var other = ActionPacing.Begin(Long);
        var scope = ActionPacing.Begin(Short);

        scope.Dispose();
        scope.Dispose();

        Assert.Equal(Long, ActionPacing.Pause);
    }

    [Fact]
    public void AZeroPause_PacesNothing()
    {
        using (ActionPacing.Begin(TimeSpan.Zero))
        {
            Assert.Null(ActionPacing.Pause);
        }
    }
}
