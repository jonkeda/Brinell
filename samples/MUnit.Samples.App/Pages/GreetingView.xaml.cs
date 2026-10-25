using Microsoft.Maui.Controls;

namespace MUnit.Samples.App.Pages;

/// <summary>A view with code-behind and no view model: a click handler changes its labels.</summary>
public partial class GreetingView : ContentView
{
    public static readonly BindableProperty NameProperty = BindableProperty.Create(
        nameof(Name), typeof(string), typeof(GreetingView), "stranger",
        propertyChanged: (view, _, name) => ((GreetingView)view).Greeting.Text = $"Hello, {name}");

    private int _waves;

    public GreetingView()
    {
        InitializeComponent();
    }

    public string Name
    {
        get => (string)GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    private void OnWave(object? sender, EventArgs e)
    {
        _waves++;
        Waves.IsVisible = true;
        Waves.Text = _waves == 1 ? "1 wave" : $"{_waves} waves";
    }
}
