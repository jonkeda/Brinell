using Brinell.Maui.UITests.Pages;
using Brinell.Samples.Maui.App;
using MUnit;

namespace Brinell.Maui.Munit.Tests;

/// <summary>
/// The sample app's Buttons and Toggle pages, driven by the device suite's own page objects through
/// the MUnit head: the same members the Windows and Android runs call, in memory.
/// </summary>
public sealed class SamplePagesOnMunitTests : IDisposable
{
    private readonly MauiTestContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public void A_button_runs_its_command_and_the_status_says_so()
    {
        var page = new ButtonsTestPage(_ctx.Render<ButtonsPage>().AsBrinellContext());

        page.StatusLabel.AssertText("Ready. Click any button to test.");
        page.TestButton.Click();
        page.TestButton.Click();

        page.StatusLabel.AssertTextContains("Button tapped 2 times");
        page.ResetButton.Click();
        page.StatusLabel.AssertText("Ready. Click any button to test.");
    }

    [Fact]
    public void Toggles_reach_the_view_model_through_two_way_bindings()
    {
        var page = new ToggleTestPage(_ctx.Render<TogglePage>().AsBrinellContext());

        page.TestCheckBox.Check();
        page.TestSwitch.SetOn(true);
        page.TestRadioButton2.Select();

        page.CheckBoxStatusLabel.AssertTextContains("checked");
        page.SwitchStatusLabel.AssertTextContains("on");
        page.RadioButtonStatusLabel.AssertTextContains("2");
        Assert.True(page.TestSwitch.IsOn() == true);
        Assert.True(page.TestCheckBox.IsChecked() == true);
    }

    [Fact]
    public void The_page_object_finds_its_page_by_its_root_AutomationId()
    {
        var page = new ButtonsTestPage(_ctx.Render<ButtonsPage>().AsBrinellContext());

        Assert.True(page.IsLoaded());
        page.TestImageButton.AssertEnabled();
    }
}
