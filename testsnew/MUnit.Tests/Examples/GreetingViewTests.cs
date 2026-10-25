using Microsoft.Maui.Controls;
using MUnit.Assertions;
using MUnit.Interaction;
using MUnit.Samples.App.Pages;
using Xunit;

namespace MUnit.Tests.Examples;

/// <summary>A ContentView with code-behind: rendered on a page of its own, its click handler run.</summary>
public sealed class GreetingViewTests
{
    // 2. Button click interaction (an event handler, no command)
    [Fact]
    public void Waving_shows_the_count()
    {
        using var ctx = new MauiTestContext();
        var view = ctx.Render<GreetingView>();
        view.Find<Label>("Waves").Should().NotBeVisible();

        view.Find<Button>("Wave").Click().Click();

        view.Find<Label>("Waves").Should().BeVisible().And.HaveText("2 waves");
    }

    [Fact]
    public void A_bindable_property_set_by_the_test_reaches_the_label()
    {
        using var ctx = new MauiTestContext();
        var view = ctx.Render(new GreetingView { Name = "Ada" });

        view.Find<Label>("Greeting").Should().HaveText("Hello, Ada");
    }
}
