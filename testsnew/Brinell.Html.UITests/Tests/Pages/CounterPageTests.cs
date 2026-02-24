using Brinell.Html;
using Brinell.Html.UITests.PageObjects;
using Brinell.Html.UITests.TestBase;

namespace Brinell.Html.UITests.Tests.Pages;

public sealed class CounterPageTests : BlazorSampleTestBase
{
    [Fact]
    public void Counter_MultipleIncrements_DisplaysCorrectCount()
    {
        NavigateToPage("/counter");
        var page = new CounterPage(Context);

        page.IncrementButton.Click();
        page.IncrementButton.Click();
        page.IncrementButton.Click();

        page.CountDisplay.AssertText("Current count: 3");
    }

    [Fact]
    public void Counter_ResetAfterIncrements_DisplaysZero()
    {
        NavigateToPage("/counter");
        var page = new CounterPage(Context);

        page.IncrementButton.Click();
        page.IncrementButton.Click();
        page.ResetButton.Click();

        page.CountDisplay.AssertText("Current count: 0");
    }

    [Fact]
    public async Task Counter_MultipleIncrements_DisplaysCorrectCount_Async()
    {
        await NavigateToPageAsync("/counter");
        var page = new CounterPage(Context);

        await page.IncrementButton.ClickAsync();
        await page.IncrementButton.ClickAsync();
        await page.IncrementButton.ClickAsync();

        await page.CountDisplay.AssertTextAsync("Current count: 3");
    }

    [Fact]
    public async Task Counter_ResetAfterIncrements_DisplaysZero_Async()
    {
        await NavigateToPageAsync("/counter");
        var page = new CounterPage(Context);

        await page.IncrementButton.ClickAsync();
        await page.IncrementButton.ClickAsync();
        await page.ResetButton.ClickAsync();

        await page.CountDisplay.AssertTextAsync("Current count: 0");
    }
}