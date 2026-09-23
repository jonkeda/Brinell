using Brinell.Maui.UITests.Pages.Shell;

namespace Brinell.Maui.UITests.Tests.Shell;

/// <summary>
/// UI tests for a MAUI Shell's tabs.
/// </summary>
/// <remarks>
/// The same tests run on Windows and Android unchanged. If they ever need a per-platform
/// branch, the adapter is in the wrong place - see <c>ShellChrome</c>.
/// </remarks>
[Collection("Shell")]
[Trait("Category", "UITest")]
[Trait("Pattern", "Shell")]
public class ShellTabTests
{
    private readonly ShellFixture _fixture;

    public ShellTabTests(ShellFixture fixture)
    {
        _fixture = fixture;
        _fixture.OpenTab("Home");
    }

    private ShellSamplePage Page => _fixture.Page;

    /// <summary>1. The shell reports its tabs.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Control", "Shell")]
    public Task Shell_ReportsItsTabs()
    {
        Page.Shell.Tabs.AssertItemCount(4);

        Assert.Equal(
            new[] { "Home", "Controls", "Detail", "Status" },
            Page.Shell.Tabs.Items.Select(tab => tab.GetText()));

        return Task.CompletedTask;
    }

    /// <summary>2. Selecting a tab shows its page, live.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Click")]
    public Task Shell_SelectTab_ShowsItsPage()
    {
        _fixture.OpenTab("Controls");

        Page.ControlsPage.AssertExists();

        // Not just rendered - working. A page marker alone would pass for a dead page.
        Page.ControlsButton.Click();
        Page.ControlsResult.AssertText("recorded");

        return Task.CompletedTask;
    }

    /// <summary>3. The shell reports which tab is current.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsSelected")]
    public Task Shell_ReportsTheCurrentTab()
    {
        _fixture.OpenTab("Detail");

        Page.Shell.Tabs["Detail"].AssertSelected();
        Page.Shell.Tabs["Home"].AssertSelected(false);

        return Task.CompletedTask;
    }

    /// <summary>4. Selecting the tab you are already on changes nothing.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Pattern", "Idempotence")]
    public Task Shell_SelectingTheCurrentTab_IsHarmless()
    {
        _fixture.OpenTab("Status");
        Page.StatusPage.AssertExists();

        Page.Shell.Tabs["Status"].Click();

        Page.StatusPage.AssertExists();
        Page.Shell.Tabs["Status"].AssertSelected();

        return Task.CompletedTask;
    }
}
