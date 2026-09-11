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
            Assert.True(
                _fixture.Context.Driver.TryNavigateBack(),
                "The app under test published no semantic route back. Without one the suite "
                + "cannot run in background mode: see HubPage.AddBackToHub.");
        }

        Assert.True(
            _fixture.Hub.WaitLoaded(true, TestConstants.DefaultTestTimeoutMs),
            "The verb reported success but the hub never appeared.");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Asked from the hub, with nothing to pop, it reports that rather than claiming success.
    /// </summary>
    /// <remarks>
    /// The distinction the return value exists for. The verb answers <c>S_FALSE</c> - succeeded
    /// and did nothing - and the client reports false, so a caller does not go on to wait for a
    /// page change that was never going to come. Reporting plain success here would turn an
    /// immediate, accurate answer into a timeout somewhere else.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task NavigateBack_AtTheHub_ReportsNothingToPop()
    {
        _fixture.Open(SamplePage.Text);

        // Polled until the app answers, rather than asked once.
        //
        // A page publishes its bridge target on Loaded, and that is a different event from its
        // root appearing in the automation tree - so there is a window just after Open in which
        // the app truthfully reports nothing to pop about a page that is on its way in. Two
        // earlier versions of this test read that window: one waited for the hub (which stays
        // findable underneath an open page and so proves nothing), one waited for the page root
        // (which happens first). Both passed alone and failed in a suite, which is the signature
        // of an arrangement racing the thing it is arranging.
        //
        // Waiting for the verb itself to succeed has no such window: it is the same question the
        // assertion is about.
        var popped = WaitHelper.WaitFor(
            () => _fixture.Context.Driver.TryNavigateBack(),
            timeoutMs: TestConstants.ShortTestTimeoutMs,
            pollingIntervalMs: 100);

        Assert.True(popped, "The app never published a route back from the page that was opened.");

        // Settle before asserting, and the assertion is about staying settled.
        //
        // A pop is started, not completed, when the verb returns - it is deliberately not
        // awaited, so the navigation stack reads one page deeper for a moment afterwards.
        // Asserting immediately therefore times the transition rather than the contract, which
        // is what made this test fail about one run in three.
        //
        // What the fixture actually depends on is that once the app is at its root, asking again
        // keeps saying no. So: wait for the first no, then require the next one to agree.
        Assert.True(
            WaitHelper.WaitFor(
                () => !_fixture.Context.Driver.TryNavigateBack(),
                timeoutMs: TestConstants.ShortTestTimeoutMs,
                pollingIntervalMs: 100),
            "The app never settled at its root: it kept reporting that something was popped.");

        Assert.False(
            _fixture.Context.Driver.TryNavigateBack(),
            "Going back from the hub reported success, but there was nothing on the stack.");

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
