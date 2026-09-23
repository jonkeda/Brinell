using Brinell.Maui.UITests.Pages.Shell;

namespace Brinell.Maui.UITests.Tests.Shell;

/// <summary>
/// UI tests for a MAUI Shell's flyout.
/// </summary>
[Collection("Shell")]
[Trait("Category", "UITest")]
[Trait("Pattern", "Shell")]
public class ShellFlyoutTests
{
    private readonly ShellFixture _fixture;

    public ShellFlyoutTests(ShellFixture fixture)
    {
        _fixture = fixture;
        _fixture.OpenTab("Home");
    }

    private ShellSamplePage Page => _fixture.Page;

    /// <summary>
    /// 1. The flyout starts shut.
    /// </summary>
    /// <remarks>
    /// Shut, not empty. Windows keeps the pane's items in the tree once it has been opened
    /// once, hidden rather than removed, so counting items would assert a platform artifact
    /// and would answer differently on a fresh launch than on the second test in a run.
    /// Whether the flyout is open is the contract; what its hidden items do is not.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsOpen")]
    public Task Flyout_StartsShut()
    {
        Page.Shell.Flyout.AssertOpen(false);

        return Task.CompletedTask;
    }

    /// <summary>2. Opening it reveals its items.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Open")]
    public Task Flyout_Open_RevealsItems()
    {
        Page.Shell.Flyout.Open()
            .AssertOpen()
            .AssertItemCount(7);

        Assert.Equal(
            new[] { "Main", "Settings", "Profile", "Reports", "History", "Downloads", "About" },
            Page.Shell.Flyout.Items.Select(item => item.GetText()));

        return Task.CompletedTask;
    }

    /// <summary>3. Opening an open flyout is harmless.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Pattern", "Idempotence")]
    public Task Flyout_OpenTwice_StaysOpen()
    {
        Page.Shell.Flyout.Open().Open().AssertOpen();

        return Task.CompletedTask;
    }

    /// <summary>4. An item navigates to its own page.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Click")]
    public Task Flyout_Item_NavigatesToItsPage()
    {
        Page.Shell.Flyout.Open()["Profile"].Click();

        Page.FlyoutPageTitle.AssertText("Profile");

        return Task.CompletedTask;
    }

    /// <summary>
    /// 5. The last item is reachable, whatever it takes to reach it.
    /// </summary>
    /// <remarks>
    /// A flyout is a list, and a long enough one scrolls. The test asserts reachability rather
    /// than that a scroll happened: whether the last item is below the fold depends on the
    /// window and the device, so a test that insisted on scrolling would be asserting the
    /// screen size.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Pattern", "Reachability")]
    public Task Flyout_LastItem_IsReachable()
    {
        Page.Shell.Flyout.Open()["About"].Click();

        Page.FlyoutPageTitle.AssertText("About");

        return Task.CompletedTask;
    }

    /// <summary>6. Dismissing leaves the flyout shut and the tabs usable again.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Close")]
    public Task Flyout_Close_LeavesTheTabsUsable()
    {
        Page.Shell.Flyout.Open().AssertOpen();

        Page.Shell.Flyout.Close().AssertOpen(false);

        Page.Shell.Tabs["Controls"].Click();
        Page.ControlsPage.AssertExists();

        return Task.CompletedTask;
    }
}
