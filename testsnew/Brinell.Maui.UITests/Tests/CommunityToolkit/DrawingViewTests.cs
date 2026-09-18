using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.CommunityToolkit;

/// <summary>
/// UI tests for the CommunityToolkit DrawingView.
/// </summary>
/// <remarks>
/// On Windows a line is arranged through the app's <c>Pan</c> sink, not drawn with a pointer; a
/// test of real strokes belongs on the mobile head.
/// </remarks>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Control", "DrawingView")]
public class DrawingViewTests
{
    private readonly MauiFixture _fixture;

    public DrawingViewTests(MauiFixture fixture)
    {
        _fixture = fixture;
        _fixture.Open(SamplePage.CommunityToolkit);
    }

    private CommunityToolkitTestPage GetPage() => new(_fixture.Context);

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task DrawingView_ResolvesAndIsVisible()
    {
        GetPage().TestDrawingView.AssertExists()
            .TestDrawingView.AssertVisible();
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetLineCount")]
    public Task DrawingView_StartsEmpty()
    {
        var page = GetPage();

        Assert.Equal(0, page.TestDrawingView.GetLineCount());
        page.DrawingStatusLabel.AssertText("lines: 0");
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "DrawLine")]
    public Task DrawingView_DrawLine_AddsLineAndRaisesCompletion()
    {
        GetPage().TestDrawingView.DrawLine(40, 20)
            .TestDrawingView.AssertLineCount(1)
            .DrawingStatusLabel.AssertText("lines: 1")
            .TestDrawingView.DrawLine(-30, 10)
            .TestDrawingView.AssertLineCount(2)
            .DrawingStatusLabel.AssertText("lines: 2");
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "WaitLineCount")]
    public Task DrawingView_Clear_EmptiesView()
    {
        var page = GetPage();

        page.TestDrawingView.DrawLine(25, 25)
            .ClearDrawingButton.Click();

        Assert.True(page.TestDrawingView.WaitLineCount(0));
        page.DrawingStatusLabel.AssertText("lines: 0");
        return Task.CompletedTask;
    }
}
