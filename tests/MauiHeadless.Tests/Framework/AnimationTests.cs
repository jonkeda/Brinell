using Microsoft.Maui.Controls;
using MauiHeadless.Assertions;
using MauiHeadless.Interaction;
using Xunit;

namespace MauiHeadless.Tests.Framework;

/// <summary>Animations finish at once, as with the system's animations off, so a test never waits on one.</summary>
public sealed class AnimationTests
{
    [Fact]
    public void A_fade_finishes_at_once_and_the_handler_carries_on()
    {
        using var ctx = new MauiTestContext();
        var label = new Label { AutomationId = "Faded", Text = "before" };
        var button = new Button { AutomationId = "Fade" };
        button.Clicked += async (_, _) =>
        {
            await label.FadeToAsync(0.25, 250);
            label.Text = "after";
        };
        var page = ctx.Render(new ContentPage { Content = new VerticalStackLayout { label, button } });

        page.Find<Button>("Fade").Click();

        page.Find<Label>("Faded").Should().HaveText("after");
        Assert.Equal(0.25, label.Opacity, 3);
    }
}
