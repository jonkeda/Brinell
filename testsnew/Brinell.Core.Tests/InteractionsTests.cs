using System.Drawing;
using Brinell.Core.Recording;

namespace Brinell.Core.Tests;

/// <summary>The interaction log a recording keeps: what a driver's element reports before it acts.</summary>
public sealed class InteractionsTests
{
    [Fact]
    public void WithNothingRecording_AReport_ReadsNothing()
    {
        var read = false;

        using (Interactions.Report(InteractionKind.Tap, () => { read = true; return Rectangle.Empty; }))
        {
        }

        Assert.Null(Interactions.Log);
        Assert.False(read);
    }

    [Fact]
    public void WhileRecording_AnAction_IsLoggedOnce_WithItsBounds_EvenWhenItRunsAnother()
    {
        var log = new Log();
        using (Interactions.Begin(log))
        {
            // A double click that runs as a gesture, which reports too: one interaction.
            using (Interactions.Report(InteractionKind.DoubleTap, () => new Rectangle(10, 20, 30, 40), "Row"))
            {
                using (Interactions.Report(InteractionKind.Tap, () => new Rectangle(0, 0, 1, 1)))
                {
                }
            }

            using (Interactions.Report(InteractionKind.Swipe, () => new Rectangle(0, 100, 400, 300), from: new Point(200, 120), to: new Point(200, 380)))
            {
            }
        }

        Assert.Equal([InteractionKind.DoubleTap, InteractionKind.Swipe], log.Taken.Select(interaction => interaction.Kind));
        Assert.Equal(new Rectangle(10, 20, 30, 40), log.Taken[0].Bounds);
        Assert.Equal("Row", log.Taken[0].Label);
        Assert.Equal(new Point(200, 380), log.Taken[1].To);
        Assert.Null(Interactions.Log);
    }

    [Fact]
    public void ATargetWhoseBoundsCannotBeRead_LeavesNoMark_AndTheActionRuns()
    {
        var log = new Log();
        using (Interactions.Begin(log))
        {
            using (Interactions.Report(InteractionKind.Tap, () => throw new InvalidOperationException("The element is stale.")))
            {
            }
        }

        Assert.Empty(log.Taken);
    }

    private sealed class Log : IInteractionLog
    {
        public List<Interaction> Taken { get; } = [];

        public void Add(Interaction interaction) => Taken.Add(interaction);
    }
}
