using Brinell.Html;
using Brinell.Html.UITests.PageObjects;
using Brinell.Html.UITests.TestBase;

namespace Brinell.Html.UITests.Tests.Controls;

public sealed class SelectControlTests : BlazorSampleTestBase
{
    [Fact]
    public void Select_SelectByValue_UpdatesSelectedValue()
    {
        NavigateToPage("/form-controls");
        var page = new FormControlsPage(Context);

        page.CountrySelect.SelectByValue("de");

        Assert.Equal("de", page.CountrySelect.GetSelectedValue());
    }

    [Fact]
    public void Select_SelectByText_UpdatesSelectedValue()
    {
        NavigateToPage("/form-controls");
        var page = new FormControlsPage(Context);

        page.ColorsSelect.SelectByText("Blue");

        Assert.Equal("blue", page.ColorsSelect.GetSelectedValue());
    }

    [Fact]
    public void Select_GetSelectedValue_ReturnsCurrentValue()
    {
        NavigateToPage("/form-controls");
        var page = new FormControlsPage(Context);

        page.CountrySelect.SelectByValue("jp");
        var selected = page.CountrySelect.GetSelectedValue();

        Assert.Equal("jp", selected);
    }

    [Fact]
    public async Task Select_SelectByValue_UpdatesSelectedValue_Async()
    {
        await NavigateToPageAsync("/form-controls");
        var page = new FormControlsPage(Context);

        await page.CountrySelect.SelectByValueAsync("de");

        Assert.Equal("de", await page.CountrySelect.GetSelectedValueAsync());
    }

    [Fact]
    public async Task Select_SelectByText_UpdatesSelectedValue_Async()
    {
        await NavigateToPageAsync("/form-controls");
        var page = new FormControlsPage(Context);

        await page.ColorsSelect.SelectByTextAsync("Blue");

        Assert.Equal("blue", await page.ColorsSelect.GetSelectedValueAsync());
    }

    [Fact]
    public async Task Select_GetSelectedValue_ReturnsCurrentValue_Async()
    {
        await NavigateToPageAsync("/form-controls");
        var page = new FormControlsPage(Context);

        await page.CountrySelect.SelectByValueAsync("jp");
        var selected = await page.CountrySelect.GetSelectedValueAsync();

        Assert.Equal("jp", selected);
    }
}