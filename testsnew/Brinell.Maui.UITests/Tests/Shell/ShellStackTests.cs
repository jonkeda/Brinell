using Brinell.Maui.UITests.Pages.Shell;

namespace Brinell.Maui.UITests.Tests.Shell;

/// <summary>
/// UI tests for a Shell tab's navigation stack.
/// </summary>
/// <remarks>
/// The suite that matters most. Shell's stack behaviour cost seven minutes of mystery once
/// (RCA-001), because a fixture assumed clicking a tab returned it to a known state. It does
/// not, and the second test below says so out loud.
/// </remarks>
[Collection("Shell")]
[Trait("Category", "UITest")]
[Trait("Pattern", "Shell")]
public class ShellStackTests
{
    private readonly ShellFixture _fixture;

    public ShellStackTests(ShellFixture fixture)
    {
        _fixture = fixture;
        _fixture.OpenTab("Detail");
    }

    private ShellSamplePage Page => _fixture.Page;

    /// <summary>1. A route pushes onto the tab's stack, and the app's back affordance pops it.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Pattern", "Stack")]
    public Task Shell_PushedPage_PopsBack()
    {
        Page.PushSubPageButton.Click();
        Page.DetailSubPage.AssertExists();

        Page.SubPageBackButton.Click();

        Page.DetailPage.AssertExists();
        Assert.False(Page.IsSubPagePushed());

        return Task.CompletedTask;
    }

    /// <summary>
    /// 2. Re-selecting the tab you are already on does <b>not</b> pop its stack.
    /// </summary>
    /// <remarks>
    /// This is not a defect, it is how Shell works, and it is the trap that made a fixture's
    /// "recovery" a no-op. Asserted as documented behaviour so that a future change to it is a
    /// failing test rather than a mystery. The pushed page is left standing on purpose; the
    /// fixture's reset is what clears it, which is what the next test checks.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Pattern", "Stack")]
    public Task Shell_ReselectingTheTab_DoesNotPop()
    {
        Page.PushSubPageButton.Click();
        Page.DetailSubPage.AssertExists();

        Page.Shell.Tabs["Detail"].Click();

        Page.DetailSubPage.AssertExists();

        return Task.CompletedTask;
    }

    /// <summary>3. The fixture's reset clears a pushed page, so tests cannot inherit one.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Pattern", "Reset")]
    public Task Shell_FixtureReset_ClearsAPushedPage()
    {
        Page.PushSubPageButton.Click();
        Page.DetailSubPage.AssertExists();

        // What every test's constructor does.
        _fixture.OpenTab("Detail");

        Assert.False(Page.IsSubPagePushed());
        Page.DetailPage.AssertExists();

        return Task.CompletedTask;
    }
}
