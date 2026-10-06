using System.Drawing;
using Brinell.Core.Recording;
using Brinell.Maui.Interfaces;
using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Gestures;

/// <summary>
/// What a recorded video's touch marks are drawn from: while a scene records, each action reports
/// itself with its target's bounds, taken before it runs (AD-024).
/// </summary>
/// <remarks>
/// A framework test of the driver: the log is installed here as a scene recording installs it,
/// without recording a video.
/// </remarks>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Stage", "Background")]
[Trait("Pattern", "GestureBridge")]
public class RecordedInteractionTests
{
    private readonly MauiFixture _fixture;

    public RecordedInteractionTests(MauiFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToContainerModule();
    }

    private ContainerTestPage Page => _fixture.ContainerTestPage;

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task ATap_AndAPullToRefresh_AreLogged_OverTheirControls_InTheWindow()
    {
        var log = new Log();
        var window = _fixture.Context.Driver.AppElement.Rect;

        using (Interactions.Begin(log))
        {
            Page.TriggerRefreshButton.Click();
            Page.RefreshText.WaitText("refreshed 1", TestConstants.DefaultTestTimeoutMs);

            // Pull to refresh: a control the tree cannot show, so its bounds come from the bridge.
            _fixture.Context.Driver.PerformGesture("TestRefreshView", MauiGesture.SwipeDown);
            Page.RefreshText.WaitText("refreshed 2", TestConstants.DefaultTestTimeoutMs);
        }

        Assert.Equal([InteractionKind.Tap, InteractionKind.Swipe], log.Taken.Select(interaction => interaction.Kind));
        var tap = log.Taken[0];
        Assert.False(tap.Bounds.IsEmpty);
        Assert.True(window.Contains(Center(tap.Bounds)), $"The button at {tap.Bounds} is in the window at {window}.");
        Assert.Equal("TriggerRefreshButton", tap.Label);

        var swipe = log.Taken[1];
        Assert.False(swipe.Bounds.IsEmpty, "The RefreshView's bounds came from the bridge.");
        Assert.True(window.Contains(Center(swipe.Bounds)), $"The RefreshView at {swipe.Bounds} is in the window at {window}.");
        Assert.True(swipe.From!.Value.Y < swipe.To!.Value.Y, "A pull to refresh is drawn downwards.");
        return Task.CompletedTask;
    }

    private static Point Center(Rectangle bounds) => new(bounds.X + (bounds.Width / 2), bounds.Y + (bounds.Height / 2));

    private sealed class Log : IInteractionLog
    {
        public List<Interaction> Taken { get; } = [];

        public void Add(Interaction interaction) => Taken.Add(interaction);
    }
}
