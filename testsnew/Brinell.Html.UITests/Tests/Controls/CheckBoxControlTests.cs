using Brinell.Html;
using Brinell.Html.UITests.PageObjects;
using Brinell.Html.UITests.TestBase;

namespace Brinell.Html.UITests.Tests.Controls;

public sealed class CheckBoxControlTests : BlazorSampleTestBase
{
    [Fact]
    public void CheckBox_Check_SetsCheckedTrue()
    {
        NavigateToPage("/form-controls");
        var page = new FormControlsPage(Context);

        page.TermsCheckBox.Uncheck();
        page.TermsCheckBox.Check();

        Assert.True(page.TermsCheckBox.IsChecked());
    }

    [Fact]
    public void CheckBox_Uncheck_SetsCheckedFalse()
    {
        NavigateToPage("/form-controls");
        var page = new FormControlsPage(Context);

        page.NewsletterCheckBox.Check();
        page.NewsletterCheckBox.Uncheck();

        Assert.False(page.NewsletterCheckBox.IsChecked());
    }

    [Fact]
    public void CheckBox_Toggle_FlipsCheckedState()
    {
        NavigateToPage("/form-controls");
        var page = new FormControlsPage(Context);

        var initial = page.TermsCheckBox.IsChecked();
        page.TermsCheckBox.Toggle();

        Assert.NotEqual(initial, page.TermsCheckBox.IsChecked());
    }

    [Fact]
    public async Task CheckBox_Check_SetsCheckedTrue_Async()
    {
        await NavigateToPageAsync("/form-controls");
        var page = new FormControlsPage(Context);

        await page.TermsCheckBox.UncheckAsync();
        await page.TermsCheckBox.CheckAsync();

        Assert.True(await page.TermsCheckBox.IsCheckedAsync());
    }

    [Fact]
    public async Task CheckBox_Uncheck_SetsCheckedFalse_Async()
    {
        await NavigateToPageAsync("/form-controls");
        var page = new FormControlsPage(Context);

        await page.NewsletterCheckBox.CheckAsync();
        await page.NewsletterCheckBox.UncheckAsync();

        Assert.False(await page.NewsletterCheckBox.IsCheckedAsync());
    }

    [Fact]
    public async Task CheckBox_Toggle_FlipsCheckedState_Async()
    {
        await NavigateToPageAsync("/form-controls");
        var page = new FormControlsPage(Context);

        var initial = await page.TermsCheckBox.IsCheckedAsync();
        await page.TermsCheckBox.ToggleAsync();

        Assert.NotEqual(initial, await page.TermsCheckBox.IsCheckedAsync());
    }
}