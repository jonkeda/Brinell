using Microsoft.Maui.Controls;
using MauiHeadless.Samples.App.Pages;

namespace MauiHeadless.Samples.App;

/// <summary>The app's Shell: two tabs and a route pushed over them.</summary>
public sealed class AppShell : Shell
{
    public AppShell()
    {
        Items.Add(new TabBar
        {
            Items =
            {
                new ShellContent { Route = "home", Title = "Home", ContentTemplate = new DataTemplate(typeof(HomePage)) },
                new ShellContent { Route = "about", Title = "About", ContentTemplate = new DataTemplate(typeof(AboutPage)) },
            },
        });
        Routing.RegisterRoute("help", typeof(HelpPage));
    }
}

public sealed class HomePage : ContentPage
{
    public HomePage()
    {
        AutomationId = "HomePage";
        var help = new Button { AutomationId = "Help", Text = "Help" };
        help.Clicked += async (_, _) => await Shell.Current.GoToAsync("help");
        Content = new VerticalStackLayout { new Label { AutomationId = "Welcome", Text = "Welcome home" }, help };
    }
}

public sealed class AboutPage : ContentPage
{
    public AboutPage()
    {
        AutomationId = "AboutPage";
        Content = new Label { AutomationId = "Version", Text = "Version 1.0" };
    }
}

public sealed class HelpPage : ContentPage
{
    public HelpPage()
    {
        AutomationId = "HelpPage";
        Content = new Label { AutomationId = "HelpText", Text = "Ask a survivor" };
    }
}
