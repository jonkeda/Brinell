using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.CommunityToolkit;

/// <summary>
/// UI tests for the CommunityToolkit Popup.
/// </summary>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Control", "Popup")]
public class PopupTests
{
    private readonly MauiFixture _fixture;

    public PopupTests(MauiFixture fixture)
    {
        _fixture = fixture;
        _fixture.Open(SamplePage.CommunityToolkitAlerts);
    }

    private CommunityToolkitAlertsTestPage GetPage() => new(_fixture.Context);

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsOpen")]
    public Task Popup_IsNotOpenBeforeShown()
    {
        var page = GetPage();

        Assert.False(page.TestPopup.IsOpen());
        page.TestPopup.AssertOpen(false);
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "WaitOpen")]
    public Task Popup_Show_OpensWithScopedContent()
    {
        var page = GetPage();

        page.ShowPopupButton.Click();

        Assert.True(page.TestPopup.WaitOpen());
        page.TestPopup.Label("PopupMessageLabel").AssertText("Popup message");

        // The page behind is asserted only once the popup has gone: on Android a popup is a modal
        // page and the page under it leaves the tree entirely while it is open.
        page.TestPopup.CloseWith("PopupCloseButton");
        page.AlertResult.AssertText("popup closed");
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "CloseWith")]
    public Task Popup_CloseWith_ClosesAndReturnsResult()
    {
        var page = GetPage();

        page.ShowPopupButton.Click();
        page.TestPopup.AssertOpen()
            .CloseWith("PopupCloseButton")
            .AssertOpen(false);

        Assert.True(page.TestPopup.WaitOpen(false));
        page.AlertResult.AssertText("popup closed");
        return Task.CompletedTask;
    }
}
