using Brinell.Blazor.UITests.PageObjects;
using Brinell.Blazor.UITests.TestBase;

namespace Brinell.Blazor.UITests.Tests.Controls;

public sealed class ButtonClickTests : BlazorSampleTestBase
{
    [Fact]
    public void Button_Click_TriggersAction()
    {
        NavigateToPage("/counter");
        var page = new CounterPage(Context);

        page.CountDisplay.AssertText("Current count: 0");
        page.IncrementButton.Click();
        page.CountDisplay.AssertText("Current count: 1");
    }

    [Fact]
    public void Button_Click_WorksOnVisibleButton()
    {
        NavigateToPage("/counter");
        var page = new CounterPage(Context);

        Assert.True(page.ResetButton.IsVisible());
        page.ResetButton.Click();
    }

    [Fact]
    public void Button_Click_WorksOnEnabledButton()
    {
        NavigateToPage("/counter");
        var page = new CounterPage(Context);

        page.IncrementButton.AssertEnabled(true);
        page.IncrementButton.Click();
    }

    [Fact]
    public void Button_MultipleClicks_AllRegister()
    {
        NavigateToPage("/counter");
        var page = new CounterPage(Context);

        page.ResetButton.Click();
        page.IncrementButton.Click();
        page.IncrementButton.Click();
        page.IncrementButton.Click();

        page.CountDisplay.AssertTextContaining("3");
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
}
