using Brinell.Maui.Interfaces;
using Brinell.Maui.UITests.Pages;
using Xunit.Abstractions;

namespace Brinell.Maui.UITests.Tests.Gestures;

/// <summary>
/// Step 10: one gesture, driven end to end through the UI Automation bridge.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the moment the architecture is proven.</b> Everything before it was measured in
/// isolation - a custom pattern round-tripping against a bare Win32 window. This asks whether
/// the same mechanism works inside a real MAUI app on WinUI, where the automation tree is
/// XAML's and the last attempt to reach into it collapsed the tree entirely.
/// </para>
/// <para>
/// <b>Addressed by <c>AutomationId</c> rather than by element</b>, and that is the finding
/// worth carrying forward. Neither control here is addressable on Windows at all - MAUI's
/// <c>SwipeView</c> and <c>RefreshView</c> map to WinUI controls whose automation peers must
/// not be overridden - so there is no element to call a method on. The bridge publishes a
/// separate element that carries the verb, which means it reaches controls Windows automation
/// cannot see, not merely controls the mouse would otherwise have to click.
/// </para>
/// <para>
/// Nothing here names a FlaUI type, so these compile into the Appium head unchanged. What they
/// do there is a later step: on Android and iOS the same gestures are real touch input.
/// </para>
/// </remarks>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Pattern", "GestureBridge")]
public class GestureBridgeTests
{
    private readonly MauiFixture _fixture;
    private readonly ITestOutputHelper _output;

    public GestureBridgeTests(MauiFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _fixture.NavigateToContainerModule();
    }

    private ContainerTestPage Page => _fixture.ContainerTestPage;

    /// <summary>
    /// The app under test publishes a bridge, and both declared elements are on it.
    /// </summary>
    /// <remarks>
    /// The control group. If this fails, every other test in the class fails for a reason that
    /// has nothing to do with gestures - the app was built without the automation sources, or
    /// the two sides disagree about the contract GUID.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task Bridge_PublishesTheDeclaredElements()
    {
        var driver = _fixture.Context.Driver;

        Assert.True(
            driver.SupportsGesture("TestSwipeView", MauiGesture.SwipeRight),
            "TestSwipeView does not offer SwipeRight. Either the app under test has no Brinell "
            + "bridge, or the GestureAutomation.Verbs declaration on it is missing.");

        Assert.True(
            driver.SupportsGesture("TestRefreshView", MauiGesture.SwipeDown),
            "TestRefreshView does not offer SwipeDown.");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Step 10's walking skeleton: a gesture with an outcome the test can see.
    /// </summary>
    /// <remarks>
    /// <c>RefreshView</c> rather than <c>SwipeView</c> for the headline assertion, because its
    /// outcome reaches an addressable label: the verb sets <c>IsRefreshing</c>, which runs the
    /// view model's refresh, which writes the status. That is a full circuit - test to bridge,
    /// bridge to MAUI API, MAUI to view model, view model to a label the test can read - rather
    /// than a call that merely returned success.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task SwipeDown_OnRefreshView_RunsTheRefresh()
    {
        Page.Status.WaitText("none", TestConstants.DefaultTestTimeoutMs);

        _fixture.Context.Driver.PerformGesture("TestRefreshView", MauiGesture.SwipeDown);

        Page.Status.WaitText("Refresh", TestConstants.DefaultTestTimeoutMs);

        Page.RefreshText.WaitText("refreshed 1", TestConstants.DefaultTestTimeoutMs);

        return Task.CompletedTask;
    }

    /// <summary>The same verb twice does the work twice, rather than latching.</summary>
    /// <remarks>
    /// Worth its own test because the dispatcher returns <c>S_FALSE</c> when a refresh is
    /// already running, and a bridge that left <c>IsRefreshing</c> set would report success
    /// forever while doing nothing.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task SwipeDown_Repeats()
    {
        var driver = _fixture.Context.Driver;

        driver.PerformGesture("TestRefreshView", MauiGesture.SwipeDown);
        Page.RefreshText.WaitText("refreshed 1", TestConstants.DefaultTestTimeoutMs);

        driver.PerformGesture("TestRefreshView", MauiGesture.SwipeDown);
        Page.RefreshText.WaitText("refreshed 2", TestConstants.DefaultTestTimeoutMs);

        return Task.CompletedTask;
    }

    /// <summary>
    /// The swipe verb reaches <c>SwipeView.Open</c>, and reports whether Windows shows anything
    /// for it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>SwipeRight, not SwipeLeft.</b> The items live in <c>SwipeView.LeftItems</c>, and a
    /// swipe to the right is what uncovers them. The inversion reads like a bug and is pinned in
    /// the dispatcher's unit tests as well as here.
    /// </para>
    /// <para>
    /// <b>Measured result: the items do not become addressable.</b> The verb reaches
    /// <c>SwipeView.Open(OpenSwipeItem.LeftItems)</c> and returns success, and the app's
    /// automation tree is byte-for-byte the same before and after - no <c>SwipeItem</c>, named
    /// or unnamed, appears. A MAUI <c>SwipeItem</c> is a <c>MenuItem</c> rather than a
    /// <c>View</c>, and WinUI's <c>SwipeControl</c> publishes nothing for it.
    /// </para>
    /// <para>
    /// So this asserts what can be checked - the verb is accepted, and the swipe view is
    /// reachable - and <b>reports</b> the reveal rather than requiring it. Following the
    /// <c>AutomationProbeTests</c> precedent: a negative result about the platform is a finding
    /// to record, not a defect to fail on. Step 18's gesture surface must therefore be backed by
    /// a <c>SwipeGestureRecognizer</c> with a command, whose outcome a test can see, rather than
    /// by <c>SwipeView</c>'s built-in items.
    /// </para>
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task SwipeRight_OnSwipeView_IsAcceptedAndReported()
    {
        var driver = _fixture.Context.Driver;

        // Accepted: PerformGesture throws unless the app returns success.
        driver.PerformGesture("TestSwipeView", MauiGesture.SwipeRight);

        // Two seconds, not the usual timeout: this is a report, and the expected answer is
        // "nothing appeared". Waiting the full budget for a known negative just makes the run
        // slower.
        var revealed = Page.WaitForSwipeItem(2_000);

        _output.WriteLine(
            revealed is not null
                ? "SwipeItem became addressable after SwipeRight."
                : "SwipeItem did NOT become addressable after SwipeRight - WinUI publishes "
                  + "nothing for SwipeView's items. Expected; see this test's remarks.");

        // Still usable afterwards, which is the part that would break if Open left the control
        // in a state the rest of the page could not recover from.
        driver.PerformGesture("TestSwipeView", MauiGesture.SwipeRight);
        Page.SwipeContentLabel.AssertExists();

        return Task.CompletedTask;
    }

    /// <summary>
    /// An undeclared verb is refused by name, and refused fast.
    /// </summary>
    /// <remarks>
    /// The negotiation this whole design rests on. A test asking for something the app under
    /// test does not offer must be told so, not left to a timeout - and the message has to name
    /// what to change, because the usual cause is a missing declaration in the app's markup
    /// rather than anything wrong with the test.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task UndeclaredGesture_IsRefusedWithAUsefulMessage()
    {
        var driver = _fixture.Context.Driver;

        // The question and the command, in that order and kept apart. Asking is allowed to
        // answer no; performing is not - it throws. There used to be a TryPerformGesture that
        // was the two glued together, and gluing them is what let a caller carry on as though
        // the gesture had happened. See .my/fix/design-actions-do-not-try.md.
        Assert.False(driver.SupportsGesture("TestSwipeView", MauiGesture.Pinch));

        var failure = Assert.ThrowsAny<NotSupportedException>(
            () => driver.PerformGesture("TestSwipeView", MauiGesture.Pinch));

        _output.WriteLine(failure.Message);
        Assert.Contains("TestSwipeView", failure.Message, StringComparison.Ordinal);

        return Task.CompletedTask;
    }

    /// <summary>An element that was never declared is distinguished from a missing bridge.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task UndeclaredElement_SaysWhatToAdd()
    {
        var failure = Assert.ThrowsAny<NotSupportedException>(
            () => _fixture.Context.Driver.PerformGesture(
                "ContainerResetButton", MauiGesture.SwipeLeft));

        _output.WriteLine(failure.Message);

        Assert.Contains("GestureAutomation.Verbs", failure.Message, StringComparison.Ordinal);

        return Task.CompletedTask;
    }

    /// <summary>
    /// The regression guard, and the one test here that is not deferrable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The failure this design exists to avoid did not look like a failure. Overriding a WinUI
    /// control's automation peer left the app rendering perfectly while every element in it
    /// silently stopped being addressable, and the only symptom was
    /// <c>ElementNotFoundException</c> everywhere at once.
    /// </para>
    /// <para>
    /// So this asserts the ordinary tree still works with the bridge attached: the containers
    /// resolve, and one of them still works as a scope. A bridge that collapsed the tree would
    /// fail here first.
    /// </para>
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task Bridge_DoesNotDisturbTheExistingTree()
    {
        Page.PageTitle.AssertExists();
        Page.Status.AssertExists();
        Page.TestGrid.AssertExists();
        Page.TestBorder.AssertExists();
        Page.TestContentView.AssertExists();
        Page.TestScrollView.AssertExists();

        // Through the container, not from page scope: a collapsed tree loses scoped search
        // first, and finding the cell any other way would not notice.
        Page.GridCellTopLeft.AssertExists();
        Page.BorderChildLabel.AssertExists();

        return Task.CompletedTask;
    }
}
