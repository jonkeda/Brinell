using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.CommunityToolkit;

/// <summary>
/// UI tests for the CommunityToolkit StateContainer.
/// </summary>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Control", "StateContainer")]
public class StateContainerTests
{
    private readonly MauiFixture _fixture;

    public StateContainerTests(MauiFixture fixture)
    {
        _fixture = fixture;
        _fixture.Open(SamplePage.CommunityToolkit);
    }

    private CommunityToolkitTestPage GetPage() => new(_fixture.Context);

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsShowing")]
    public Task StateContainer_ShowsContentByDefault()
    {
        var page = GetPage();

        page.TestStateContainer.AssertExists();
        page.TestStateContainer.AssertShowing("StateContentView")
            .AssertShowing("StateLoadingView", expected: false);
        Assert.True(page.TestStateContainer.IsShowing("StateContentView"));
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "WaitShowing")]
    public Task StateContainer_SwitchToLoading_ShowsLoadingView()
    {
        var page = GetPage();

        page.StateLoadingButton.Click()
            .StateStatusLabel.AssertText("state: loading");

        Assert.True(page.TestStateContainer.WaitShowing("StateLoadingView"));
        page.TestStateContainer.AssertShowing("StateContentView", expected: false);
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "AssertShowing")]
    public Task StateContainer_ErrorThenContent_RestoresContent()
    {
        var page = GetPage();

        page.StateErrorButton.Click();
        page.TestStateContainer.AssertShowing("StateErrorView");

        page.StateContentButton.Click();
        page.TestStateContainer.AssertShowing("StateContentView")
            .AssertShowing("StateErrorView", expected: false);
        page.StateStatusLabel.AssertText("state: content");
        return Task.CompletedTask;
    }
}
