using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using MUnit.Assertions;
using MUnit.Interaction;
using MUnit.Samples.App.Pages;
using MUnit.Samples.App.Services;
using MUnit.Samples.App.ViewModels;
using NUnit.Framework;

namespace MUnit.Tests.NUnit;

/// <summary>The settings page on NUnit: a context per test from SetUp, disposed in TearDown.</summary>
[TestFixture]
public sealed class SettingsPageTests
{
    private MauiTestContext _ctx = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new MauiTestContext();
        _ctx.Services.AddSingleton<ISettingsService>(new PreferencesSettingsService(_ctx.Preferences));
        _ctx.Services.AddTransient<SettingsViewModel>();
    }

    [TearDown]
    public void TearDown() => _ctx.Dispose();

    [Test]
    public void SaveButton_UpdatesStatus()
    {
        var page = _ctx.Render<SettingsPage>();

        page.Find<Entry>("Name").SetText("Ada");
        page.Find<Button>("SaveButton").Click();

        page.Find<Label>("Status").Should().HaveText("Saved");
        Assert.That(_ctx.Preferences.Get<string?>("name", null), Is.EqualTo("Ada"));
    }

    [Test]
    public void A_failed_MUnit_assertion_fails_the_NUnit_test_with_its_message()
    {
        var page = _ctx.Render<SettingsPage>();

        var failure = Assert.Throws<MUnitAssertionException>(() => page.Find<Label>("Status").Should().HaveText("Saved"));

        Assert.That(failure!.Message, Does.Contain("but it was \"Tell us your name.\""));
    }
}
