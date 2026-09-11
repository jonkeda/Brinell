using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Navigation;

/// <summary>
/// The back affordance, driven as a control object scoped to the app rather than to a page.
/// </summary>
/// <remarks>
/// <para>
/// <b>These exist because nothing else reaches this code on Windows.</b> Since stage B the
/// fixture asks the app to pop itself through the automation bridge, so
/// <see cref="MauiFixture.BackToHub"/> is only used on platforms with no bridge - Android and
/// iOS, where the tests do not run on this head. Without a test that drives it deliberately,
/// <c>AppRoot</c> and <c>ToolbarButton</c> would be code that compiles on the platform that can
/// run it and is only exercised on the platforms that cannot.
/// </para>
/// <para>
/// <b>And this is the arrangement that once took the whole suite down.</b> The button used to be
/// a <c>Button&lt;HubPage&gt;</c>, which is a contradiction - the hub is not loaded precisely
/// when the way back to it is wanted - and both readiness gates refused, correctly. The fix at
/// the time abandoned the control object for a hand-rolled lookup; <c>AppRoot</c> is the scope
/// that was actually missing. See <c>.my/fix/rca-backtohub-is-not-a-control-object.md</c>.
/// </para>
/// </remarks>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Pattern", "Navigation")]
public class AppRootScopeTests
{
    private readonly MauiFixture _fixture;

    public AppRootScopeTests(MauiFixture fixture) => _fixture = fixture;

    /// <summary>
    /// A control on the app scope resolves while a page is open, with no page gate refusing it.
    /// </summary>
    /// <remarks>
    /// The regression guard for the original outage. A page-scoped control here throws
    /// <c>PageLoadException</c> with <c>MissingRoot</c>, because it asks a page object for
    /// something that is not in it.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task BackToHub_ResolvesWhileAPageIsOpen()
    {
        _fixture.Open(SamplePage.Buttons);

        Assert.True(
            _fixture.BackToHub.WaitExists(true, TestConstants.ShortTestTimeoutMs),
            "The back toolbar item did not resolve from the app scope while a page was open.");

        return Task.CompletedTask;
    }

    /// <summary>
    /// It is absent at the hub, and says so rather than throwing.
    /// </summary>
    /// <remarks>
    /// The hub attaches the item to the pages it opens, so it does not have one itself. A scope
    /// that is always ready must still report an element that is genuinely not there, or the
    /// fixture's fallback loop cannot tell "no button" from "not yet".
    /// </remarks>
    [Trait(PhysicalInputTrait.Name, PhysicalInputTrait.Deliberate)]
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task BackToHub_IsAbsentAtTheHub()
    {
        _fixture.Open(SamplePage.Buttons);
        _fixture.BackToHub.Click();
        _fixture.Hub.WaitLoaded(true, TestConstants.DefaultTestTimeoutMs);

        Assert.False(
            _fixture.BackToHub.IsExists(),
            "The hub reported a back toolbar item, which it does not have.");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Clicking it returns to the hub - the fallback route, exercised on purpose.
    /// </summary>
    /// <remarks>
    /// This is the path every non-Windows run takes on every test. It clicks rather than
    /// invoking a pattern, deliberately: see <c>ToolbarButton</c> for the four measurements
    /// behind that, the worst of which is that Invoke reports success and does nothing.
    /// </remarks>
    [Trait(PhysicalInputTrait.Name, PhysicalInputTrait.Deliberate)]
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task BackToHub_Click_ReturnsToTheHub()
    {
        _fixture.Open(SamplePage.Container);

        _fixture.BackToHub.Click();

        Assert.True(
            _fixture.Hub.WaitLoaded(true, TestConstants.DefaultTestTimeoutMs),
            "Clicking the back toolbar item did not return to the hub.");

        return Task.CompletedTask;
    }
}
