using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using MauiUnit.Assertions;
using MauiUnit.Interaction;
using MauiUnit.Samples.App.Pages;
using MauiUnit.Samples.App.Services;
using MauiUnit.Samples.App.ViewModels;
using Xunit;

namespace MauiUnit.Tests.Examples;

/// <summary>5. Dependency injection: the app's real service over the context's in-memory preferences, and a fake swapped in.</summary>
public sealed class DependencyInjectionTests
{
    [Fact]
    public void The_real_service_saves_into_the_contexts_preferences()
    {
        using var ctx = new MauiTestContext();
        ctx.Services.AddSingleton<ISettingsService, PreferencesSettingsService>();
        ctx.Services.AddTransient<SettingsViewModel>();
        ctx.Preferences.Set("name", "Ada");

        var page = ctx.Render<SettingsPage>();
        page.Find<Entry>("Name").Should().HaveText("Ada");
        page.Find<Entry>("Name").SetText("Grace");
        page.Find<Button>("SaveButton").Click();

        Assert.Equal("Grace", ctx.Preferences.Get<string?>("name", null));
        Assert.Same(ctx.Preferences, ctx.Services.GetRequiredService<IPreferences>());
    }

    [Fact]
    public void Replace_swaps_a_registration_for_a_fake()
    {
        using var ctx = new MauiTestContext();
        ctx.Services.AddSingleton<ISettingsService, PreferencesSettingsService>();
        ctx.Services.AddTransient<SettingsViewModel>();
        ctx.Services.Replace<ISettingsService>(new FakeSettingsService { Name = "Fake" });

        var page = ctx.Render<SettingsPage>();

        page.Find<Label>("Status").Should().HaveText("Welcome back, Fake.");
    }

    [Fact]
    public void Services_are_fixed_after_the_first_render()
    {
        using var ctx = new MauiTestContext();
        ctx.Render(new ContentPage());

        Assert.Throws<InvalidOperationException>(() => ctx.Services.AddSingleton("late"));
    }
}
