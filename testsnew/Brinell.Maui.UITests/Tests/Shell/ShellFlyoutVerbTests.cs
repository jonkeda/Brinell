using Xunit;

namespace Brinell.Maui.UITests.Tests.Shell;

/// <summary>
/// Step 26: opening a Shell's flyout by asking the Shell.
/// </summary>
/// <remarks>
/// <para>
/// <b>The hamburger button has no <c>AutomationId</c>.</b> MAUI draws it as Shell chrome, so
/// every route to it from outside the app was a guess about a control the platform owns: find it
/// by name, by control type, or click where it usually is. <c>FlyoutIsPresented</c> is a public
/// MAUI property, and the app setting it is not a guess about anything.
/// </para>
/// <para>
/// <b>Live, while the rest of this suite is parked.</b> The other Shell tests carry a step 32
/// skip because they were failing before this programme started; these are new and are held to
/// the ordinary standard. If they ever start failing too, that is a regression in the verb and
/// not more of step 32.
/// </para>
/// </remarks>
[Collection("Shell")]
[Trait("Category", "UITest")]
[Trait("Pattern", "GestureBridge")]
[Trait("Stage", "Background")]
public class ShellFlyoutVerbTests
{
    private readonly ShellFixture _fixture;

    /// <remarks>
    /// <b>No <c>OpenTab</c> here, and that is the point rather than an omission.</b> The
    /// fixture's own reset opens the flyout by hunting for a button named "Open Navigation" -
    /// the guess this verb exists to replace - and it throws before any test in this class
    /// runs. The flyout belongs to the Shell, so these tests need no page at all.
    /// </remarks>
    public ShellFlyoutVerbTests(ShellFixture fixture) => _fixture = fixture;

    /// <summary>
    /// The flyout opens and closes without physical input.
    /// </summary>
    /// <remarks>
    /// Both directions in one test, deliberately: a flyout that opens and cannot be closed
    /// leaves every later test looking at a covered page, so "it opened" on its own is not the
    /// claim worth making.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task Flyout_OpensAndClosesWithoutPhysicalInput()
    {
        var driver = _fixture.Context.Driver;

        Assert.False(driver.IsFlyoutOpen(), "The flyout was already open before the test.");

        driver.OpenFlyout();
        Assert.True(driver.IsFlyoutOpen(), "The app accepted OpenFlyout and nothing opened.");

        driver.CloseFlyout();
        Assert.False(driver.IsFlyoutOpen(), "The app accepted CloseFlyout and it stayed open.");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Opening an open flyout is harmless, and so is closing a shut one.
    /// </summary>
    /// <remarks>
    /// <b>The caller asked for a state, not for a transition.</b> A test that had to know which
    /// of the two happened would be asserting the order of the tests before it - and the app
    /// does say so in its own log, where the second call answers <c>S_FALSE</c>, but that never
    /// reaches this side: UI Automation reports every success HRESULT as <c>S_OK</c>.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task Flyout_AskedTwiceForTheSameState_StaysThere()
    {
        var driver = _fixture.Context.Driver;

        driver.OpenFlyout();
        driver.OpenFlyout();
        Assert.True(driver.IsFlyoutOpen());

        driver.CloseFlyout();
        driver.CloseFlyout();
        Assert.False(driver.IsFlyoutOpen());

        return Task.CompletedTask;
    }
}
