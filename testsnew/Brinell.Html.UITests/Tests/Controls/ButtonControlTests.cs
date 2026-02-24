using Brinell.Html;
using Brinell.Html.UITests.PageObjects;
using Brinell.Html.UITests.TestBase;

namespace Brinell.Html.UITests.Tests.Controls;

public sealed class ButtonControlTests : BlazorSampleTestBase
{
    [Fact]
    public void Button_Click_IncrementsCounter()
    {
        NavigateToPage("/counter");
        var page = new CounterPage(Context);

        page.CountDisplay.AssertText("Current count: 0");
        page.IncrementButton.Click();
        page.CountDisplay.AssertText("Current count: 1");
    }

    [Fact]
    public void Button_IsVisible_ReturnsTrueForVisibleButton()
    {
        NavigateToPage("/counter");
        var page = new CounterPage(Context);

        Assert.True(page.IncrementButton.IsVisible());
    }

    [Fact]
    public void Button_AssertEnabled_PassesForEnabledButton()
    {
        NavigateToPage("/counter");
        var page = new CounterPage(Context);

        page.IncrementButton.AssertEnabled(true);
    }

    [Fact]
    public async Task Button_Click_IncrementsCounter_Async()
    {
        await NavigateToPageAsync("/counter");
        var page = new CounterPage(Context);

        await page.CountDisplay.AssertTextAsync("Current count: 0");
        await page.IncrementButton.ClickAsync();
        await page.CountDisplay.AssertTextAsync("Current count: 1");
    }

    [Fact]
    public async Task Button_IsVisible_ReturnsTrueForVisibleButton_Async()
    {
        await NavigateToPageAsync("/counter");
        var page = new CounterPage(Context);

        Assert.True(await page.IncrementButton.IsVisibleAsync());
    }

    [Fact]
    public async Task Button_AssertEnabled_PassesForEnabledButton_Async()
    {
        await NavigateToPageAsync("/counter");
        var page = new CounterPage(Context);

        await page.IncrementButton.AssertEnabledAsync(true);
    }
}