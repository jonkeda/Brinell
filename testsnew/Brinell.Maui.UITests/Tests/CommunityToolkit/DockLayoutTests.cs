using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.CommunityToolkit;

/// <summary>
/// UI tests for the CommunityToolkit DockLayout used as a scope: its docked children resolve and
/// act through it, and controls outside it do not.
/// </summary>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Control", "DockLayout")]
public class DockLayoutTests
{
    private readonly MauiFixture _fixture;

    public DockLayoutTests(MauiFixture fixture)
    {
        _fixture = fixture;
        _fixture.Open(SamplePage.CommunityToolkit);
    }

    private CommunityToolkitTestPage GetPage() => new(_fixture.Context);

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "AssertExists")]
    public Task DockLayout_OnPage_ResolvesItsDockedChildren()
    {
        GetPage().TestDockLayout
            .AssertExists()
            .HeaderLabel.AssertText("Dock header")
            .CenterLabel.AssertText("Dock centre")
            .ActionButton.AssertExists();
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Child")]
    public Task DockLayout_ClickDockedButton_UpdatesDockedStatus()
    {
        GetPage().TestDockLayout
            .ActionButton.Click()
            .StatusLabel.AssertText("dock: pressed");
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Label")]
    public Task DockLayout_ControlOutsideTheDock_IsNotFoundThroughIt()
    {
        var page = GetPage();

        page.StateStatusLabel.AssertExists();
        page.TestDockLayout
            .AssertExists()
            .Label("StateStatusLabel").AssertExists(false);
        return Task.CompletedTask;
    }
}
