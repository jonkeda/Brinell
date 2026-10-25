using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;

namespace MauiUnit.Samples.App.Pages;

/// <summary>A countdown on a dispatcher timer: a test advances MauiUnit's virtual clock instead of waiting.</summary>
public sealed class CountdownPage : ContentPage
{
    private readonly Label _left = new() { AutomationId = "Left" };
    private readonly IDispatcherTimer _timer;
    private int _seconds = 3;

    public CountdownPage(IDispatcher dispatcher)
    {
        AutomationId = "CountdownPage";
        _left.Text = $"{_seconds} s";
        _timer = dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) =>
        {
            _seconds--;
            _left.Text = _seconds > 0 ? $"{_seconds} s" : "Go!";
            if (_seconds == 0)
                _timer.Stop();
        };
        var start = new Button { AutomationId = "Start", Text = "Start" };
        start.Clicked += (_, _) => _timer.Start();
        Content = new VerticalStackLayout { _left, start };
    }
}
