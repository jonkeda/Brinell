using Microsoft.Maui.Controls;
using MauiUnit.Assertions;
using MauiUnit.Inspection;
using MauiUnit.Interaction;
using Xunit;

namespace MauiUnit.Tests.Framework;

/// <summary>The tree as the testing surface: queries, their errors, interactions refused, and the snapshot text.</summary>
public sealed class TreeTests
{
    private static ContentPage Form() => new()
    {
        AutomationId = "Form",
        Title = "Form",
        Content = new VerticalStackLayout
        {
            new Entry { AutomationId = "Name", Placeholder = "Your name" },
            new Switch { AutomationId = "Agree", IsToggled = true },
            new Button { AutomationId = "Send", Text = "Send", IsEnabled = false },
            new VerticalStackLayout
            {
                IsVisible = false,
                Children = { new Label { AutomationId = "Secret", Text = "hidden \"text\"" } },
            },
        },
    };

    [Fact]
    public void The_snapshot_prints_what_a_user_sees()
    {
        using var ctx = new MauiTestContext();
        var page = ctx.Render(Form());

        var expected = """
            Window
              ContentPage #Form "Form"
                VerticalStackLayout
                  Entry #Name placeholder="Your name"
                  Switch #Agree [on]
                  Button #Send "Send" [disabled]
                  VerticalStackLayout [hidden]
                    Label #Secret "hidden \"text\""

            """;
        Assert.Equal(expected.ReplaceLineEndings("\n"), page.ToSnapshot());
    }

    [Fact]
    public void The_tree_reads_effective_state()
    {
        using var ctx = new MauiTestContext();
        var page = ctx.Render(Form());

        var secret = page.Tree.DescendantsAndSelf().Single(node => node.AutomationId == "Secret");

        Assert.False(secret.IsVisible);
        Assert.True(secret.IsEnabled);
        Assert.Equal("hidden \"text\"", secret.Text);
        Assert.Equal(typeof(Label), secret.Type);
    }

    [Fact]
    public void A_miss_shows_the_tree_searched()
    {
        using var ctx = new MauiTestContext();
        var page = ctx.Render(Form());

        var miss = Assert.Throws<ElementNotFoundException>(() => page.Find<Button>("Save"));

        Assert.Contains("No Button with AutomationId 'Save' was found.", miss.Message);
        Assert.Contains("Button #Send \"Send\" [disabled]", miss.Message);
    }

    [Fact]
    public void Find_wants_exactly_one()
    {
        using var ctx = new MauiTestContext();
        var page = ctx.Render(new ContentPage { Content = new VerticalStackLayout { new Label(), new Label() } });

        var many = Assert.Throws<ElementNotFoundException>(() => page.Find<Label>());

        Assert.Contains("2 elements match Label", many.Message);
        Assert.Equal(2, page.FindAll<Label>().Count);
    }

    [Fact]
    public void Hidden_elements_cannot_be_typed_into()
    {
        using var ctx = new MauiTestContext();
        var entry = new Entry { AutomationId = "Hidden" };
        var page = ctx.Render(new ContentPage { Content = new ContentView { IsVisible = false, Content = entry } });

        var refused = Assert.Throws<ElementNotInteractableException>(() => page.Find<Entry>().SetText("x"));

        Assert.Contains("not visible", refused.Message);
    }

    [Fact]
    public void A_failed_assertion_names_the_element_and_the_screen()
    {
        using var ctx = new MauiTestContext();
        var page = ctx.Render(Form());

        var failure = Assert.Throws<MauiUnitAssertionException>(() => page.Find<Button>("Send").Should().BeEnabled());

        Assert.StartsWith("Expected Button #Send \"Send\" [disabled] to be enabled", failure.Message);
        Assert.Contains("Switch #Agree [on]", failure.Message);
    }

    [Fact]
    public void Text_input_is_cut_at_MaxLength_as_on_a_device()
    {
        using var ctx = new MauiTestContext();
        var page = ctx.Render(new ContentPage { Content = new Entry { MaxLength = 3 } });

        page.Find<Entry>().SetText("abcdef");

        page.Find<Entry>().Should().HaveText("abc");
    }

    [Fact]
    public void A_tap_runs_the_recognizers_command()
    {
        using var ctx = new MauiTestContext();
        var taps = 0;
        var label = new Label { Text = "tap me" };
        label.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => taps++) });
        var page = ctx.Render(new ContentPage { Content = label });

        page.Find<Label>().Tap();

        Assert.Equal(1, taps);
    }
}
