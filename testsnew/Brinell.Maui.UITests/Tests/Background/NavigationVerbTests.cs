using Brinell.Core.Diagnostics;
using Brinell.Core.Utilities;
using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Background;

/// <summary>
/// Step 15's linchpin: returning to the hub without a mouse.
/// </summary>
/// <remarks>
/// <para>
/// <b>One line of the fixture was the whole suite's physical-input footprint.</b> An audit run
/// recorded 400 uses of real input across 209 tests, and 396 of them traced to a single mouse
/// click in <c>MauiFixture.ReturnToHub</c> - once per test, plus the <c>SetForeground</c> each
/// click implies. Every control-object click in the suite already went through a UI Automation
/// pattern; this one could not.
/// </para>
/// <para>
/// <b>It could not because of the control, not the framework.</b> The affordance is a MAUI
/// <c>ToolbarItem</c>, rendered into native window chrome. Driving it through the Invoke
/// pattern was measured four ways - Invoke alone, Invoke with a click fallback, the shared
/// activation ladder that works for every other control, and the ladder with a retry - and all
/// four fail identically: Invoke reports success and the command does not run, so navigation
/// silently does not happen and the run degrades from nine seconds to about a minute.
/// </para>
/// <para>
/// <b>So the page answers instead of the button.</b> <c>HubPage.AddBackToHub</c> declares
/// <c>NavigateBack</c> on the page it is attaching the item to, and the bridge routes it to
/// <c>Navigation.PopAsync</c>. Nothing about the app's appearance changes; the toolbar item is
/// still there and still works for a person.
/// </para>
/// </remarks>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Pattern", "GestureBridge")]
[Trait("Stage", "Background")]
public class NavigationVerbTests
{
    private readonly MauiFixture _fixture;

    public NavigationVerbTests(MauiFixture fixture) => _fixture = fixture;

    /// <summary>
    /// The app publishes a semantic route back, and it works.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task NavigateBack_ReturnsToTheHubWithoutPhysicalInput()
    {
        _fixture.Open(SamplePage.Text);

        var page = new TextTestPage(_fixture.Context);
        page.WaitLoaded(true, TestConstants.DefaultTestTimeoutMs);

        using (PhysicalInput.OverridePolicy(PhysicalInputPolicy.Refused))
        {
            // Throws with the app's own reason if it did not. There is nothing to assert on the
            // call itself any more, which is the point of it no longer returning a bool.
            _fixture.Context.Driver.NavigateBack();
        }

        Assert.True(
            _fixture.Hub.WaitLoaded(true, TestConstants.DefaultTestTimeoutMs),
            "The verb reported success but the hub never appeared.");

        return Task.CompletedTask;
    }

    /// <summary>
    /// The app can say whether it has anywhere to go back to, and it is right both times.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This test used to be forty lines of polling, and the polling was load-bearing.</b> The
    /// same fact had to be established by <i>going back</i> and seeing what came out, so every
    /// reading changed what was being read: a pop is started rather than completed when the verb
    /// returns, so an immediate second ask timed the transition rather than the contract, and the
    /// test failed about one run in three. Two earlier versions waited on the wrong thing
    /// entirely - the hub, which stays findable underneath an open page, and the page root, which
    /// appears before the page publishes itself.
    /// </para>
    /// <para>
    /// A question has no such window. Asking twice gives the same answer, asking does not move
    /// the app, and the assertion is the fact rather than a side effect of establishing it.
    /// </para>
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task IsAtNavigationRoot_KnowsWhichEndOfTheStackTheAppIsOn()
    {
        _fixture.Open(SamplePage.Text);

        Assert.False(
            _fixture.Context.Driver.IsAtNavigationRoot(),
            "A page is open, so there is something to go back to - but the app said it was at "
            + "its root. A caller believing that would never return to the hub.");

        using (PhysicalInput.OverridePolicy(PhysicalInputPolicy.Refused))
        {
            _fixture.Context.Driver.NavigateBack();
        }

        Assert.True(
            _fixture.Hub.WaitLoaded(true, TestConstants.DefaultTestTimeoutMs),
            "The pop was accepted but the hub never appeared.");

        Assert.True(
            _fixture.Context.Driver.IsAtNavigationRoot(),
            "Back at the hub with an empty stack, the app still reported something to pop. That "
            + "is the answer that used to cost every fixture reset a two-second wait.");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Going back from the root fails with a reason, rather than quietly doing nothing.
    /// </summary>
    /// <remarks>
    /// The command's half of the split. A caller that has not asked the question first should not
    /// be told a pop happened - that is how a test comes to wait for a page change that was never
    /// coming - so the command is explicit about having done nothing, and says why.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task NavigateBack_AtTheHub_FailsWithTheReason()
    {
        _fixture.Open(SamplePage.Text);

        using (PhysicalInput.OverridePolicy(PhysicalInputPolicy.Refused))
        {
            _fixture.Context.Driver.NavigateBack();
        }

        _fixture.Hub.WaitLoaded(true, TestConstants.DefaultTestTimeoutMs);

        Assert.Throws<BrinellException>(() => _fixture.Context.Driver.NavigateBack());

        return Task.CompletedTask;
    }

    /// <summary>
    /// A full navigation round trip runs with physical input refused outright.
    /// </summary>
    /// <remarks>
    /// The step 15 claim in miniature: open a page, come back, open another. That is what the
    /// fixture does between every pair of tests in the suite, and until now it could not be done
    /// without taking the mouse and the foreground window.
    /// </remarks>
    [Fact(Timeout = TestConstants.LongTestTimeoutMs)]
    public Task Fixture_NavigatesBetweenPagesWithPhysicalInputRefused()
    {
        // Start from a known place outside the scope, so the assertion is about the navigation
        // and not about whatever the previous test left behind.
        _fixture.Open(SamplePage.Text);

        using (PhysicalInput.OverridePolicy(PhysicalInputPolicy.Refused))
        {
            _fixture.Open(SamplePage.Container);

            var container = _fixture.ContainerTestPage;
            Assert.True(
                container.WaitLoaded(true, TestConstants.DefaultTestTimeoutMs),
                "The container page did not open with physical input refused.");

            _fixture.Open(SamplePage.Text);

            var text = new TextTestPage(_fixture.Context);
            Assert.True(
                text.WaitLoaded(true, TestConstants.DefaultTestTimeoutMs),
                "The text page did not reopen with physical input refused.");
        }

        return Task.CompletedTask;
    }
}
