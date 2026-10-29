using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using MauiHeadless.Assertions;
using MauiHeadless.Interaction;
using MauiHeadless.Samples.App.Pages;
using MauiHeadless.Samples.App.Services;
using MauiHeadless.Samples.App.ViewModels;
using Xunit;

namespace MauiHeadless.Tests.Examples;

/// <summary>
/// The XAML settings page from the samples: its view model and service come from the context's
/// services, its OnAppearing loads them, and its Save button runs an async command.
/// </summary>
public sealed class SettingsPageTests : IDisposable
{
    private readonly MauiTestContext _ctx = new();
    private readonly FakeSettingsService _settings = new() { Name = "Ada" };

    public SettingsPageTests()
    {
        _ctx.Services.AddSingleton<ISettingsService>(_settings);
        _ctx.Services.AddTransient<SettingsViewModel>();
    }

    public void Dispose() => _ctx.Dispose();

    // 1. Label text verification
    [Fact]
    public void Status_greets_the_saved_name_once_the_page_appears()
    {
        var page = _ctx.Render<SettingsPage>();

        page.Find<Label>("Status").Should().HaveText("Welcome back, Ada.");
    }

    // 3. Command execution (the target syntax of the brief)
    [Fact]
    public void SaveButton_UpdatesStatus()
    {
        var page = _ctx.Render<SettingsPage>();

        page.Find<Button>("SaveButton").Click();

        page.Find<Label>("Status").Should().HaveText("Saved");
        Assert.Equal([("Ada", true)], _settings.Saves);
    }

    [Fact]
    public void Save_is_disabled_until_a_name_is_typed()
    {
        _settings.Name = null;
        var page = _ctx.Render<SettingsPage>();
        var save = page.Find<Button>("SaveButton");
        save.Should().BeDisabled();

        page.Find<Entry>("Name").SetText("Grace");

        save.Should().BeEnabled().And.HaveText("Save");
    }

    [Fact]
    public void Clicking_a_disabled_button_is_refused()
    {
        _settings.Name = null;
        var page = _ctx.Render<SettingsPage>();

        var refused = Assert.Throws<ElementNotInteractableException>(() => page.Find<Button>("SaveButton").Click());

        Assert.Contains("disabled", refused.Message);
        Assert.Empty(_settings.Saves);
    }

    [Fact]
    public void A_save_in_progress_shows_busy_until_the_service_answers()
    {
        _settings.Gate = new TaskCompletionSource();
        var page = _ctx.Render<SettingsPage>();

        page.Find<Button>("SaveButton").Click();
        page.Find<Label>("Status").Should().HaveText("Saving...");
        page.Find<ActivityIndicator>("Busy").Should().BeVisible();
        page.Find<Button>("SaveButton").Should().BeDisabled();

        // The command's continuation is posted to the context's queue; the next query runs it.
        _settings.Gate.SetResult();

        page.Find<Label>("Status").Should().HaveText("Saved");
        page.Find<ActivityIndicator>("Busy").Should().NotBeVisible();
    }

    [Fact]
    public void A_failed_save_says_why()
    {
        _settings.Failure = new InvalidOperationException("disk full");
        var page = _ctx.Render<SettingsPage>();

        page.Find<Button>("SaveButton").Click();

        page.FindByAutomationId("Status").Should().ContainText("disk full");
    }

    // 4. ViewModel binding updates, both ways
    [Fact]
    public void Typing_and_toggling_reach_the_view_model_and_back()
    {
        var page = _ctx.Render<SettingsPage>();
        var viewModel = (SettingsViewModel)page.Instance.BindingContext;

        page.Find<Entry>("Name").SetText("Grace");
        page.Find<Switch>("Notifications").Toggle();

        Assert.Equal("Grace", viewModel.Name);
        Assert.False(viewModel.Notifications);

        viewModel.Name = "Hopper";
        page.Find<Entry>("Name").Should().HaveText("Hopper");
        page.Find<Switch>("Notifications").Should().BeOff();
    }

    [Fact]
    public void The_markup_binds_status_to_the_view_model()
    {
        var page = _ctx.Render<SettingsPage>();

        page.Find<Label>("Status").Should()
            .BeBoundTo(Label.TextProperty, "Status")
            .And.HaveBindingContext<SettingsViewModel>(vm => vm.Name == "Ada");
    }
}
