using Brinell.Blazor.UITests.PageObjects;
using Brinell.Blazor.UITests.TestBase;

namespace Brinell.Blazor.UITests.Tests.Pages;

public sealed class CounterPageTests : BlazorSampleTestBase
{
    [Fact]
    public void Counter_NavigateToPage_ShowsCounterTitle()
    {
        NavigateToPage("/counter");
        var page = new CounterPage(Context);

        page.CounterTitle.AssertVisible(true);
    }

    [Fact]
    public void Counter_InitialState_ShowsZero()
    {
        NavigateToPage("/counter");
        var page = new CounterPage(Context);

        page.CountDisplay.AssertText("Current count: 0");
    }

    [Fact]
    public void Counter_ClickIncrement_IncreasesCount()
    {
        NavigateToPage("/counter");
        var page = new CounterPage(Context);

        page.CountDisplay.AssertText("Current count: 0");
        page.IncrementButton.Click();
        page.CountDisplay.AssertText("Current count: 1");
    }

    [Fact]
    public void Counter_ClickReset_ResetsToZero()
    {
        NavigateToPage("/counter");
        var page = new CounterPage(Context);

        page.IncrementButton.Click();
        page.IncrementButton.Click();
        page.IncrementButton.Click();
        page.ResetButton.Click();

        page.CountDisplay.AssertText("Current count: 0");
    }

    [Fact]
    public void Counter_MultipleIncrements_AccumulatesCorrectly()
    {
        NavigateToPage("/counter");
        var page = new CounterPage(Context);

        page.ResetButton.Click();
        page.IncrementButton.Click();
        page.IncrementButton.Click();
        page.IncrementButton.Click();
        page.IncrementButton.Click();
        page.IncrementButton.Click();

        page.CountDisplay.AssertTextContaining("5");
    }

    [Fact]
    public void Counter_IncrementButton_IsVisibleAndEnabled()
    {
        NavigateToPage("/counter");
        var page = new CounterPage(Context);

        Assert.True(page.IncrementButton.IsVisible());
        page.IncrementButton.AssertEnabled(true);
    }

    [Fact]
    public void Counter_ResetButton_IsVisibleAndEnabled()
    {
        NavigateToPage("/counter");
        var page = new CounterPage(Context);

        Assert.True(page.ResetButton.IsVisible());
        page.ResetButton.AssertEnabled(true);
    }

    [Fact]
    public void Counter_CountDisplay_UpdatesAfterClick()
    {
        NavigateToPage("/counter");
        var page = new CounterPage(Context);

        page.ResetButton.Click();
        page.IncrementButton.Click();

        page.CountDisplay.AssertTextContaining("1");
    }
}
