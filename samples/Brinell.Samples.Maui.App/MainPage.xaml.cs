namespace Brinell.Samples.Maui.App;

public partial class MainPage : ContentPage
{
    private int _counter = 0;

    public MainPage()
    {
        InitializeComponent();
    }

    private void OnIncrementClicked(object? sender, EventArgs e)
    {
        _counter++;
        CounterLabel.Text = $"Counter: {_counter}";
    }

    private void OnDecrementClicked(object? sender, EventArgs e)
    {
        _counter--;
        CounterLabel.Text = $"Counter: {_counter}";
    }

    private void OnResetClicked(object? sender, EventArgs e)
    {
        _counter = 0;
        CounterLabel.Text = $"Counter: {_counter}";
    }

    private void OnGreetClicked(object? sender, EventArgs e)
    {
        var name = NameEntry.Text;
        if (string.IsNullOrWhiteSpace(name))
        {
            GreetingLabel.Text = "Please enter your name";
        }
        else
        {
            GreetingLabel.Text = $"Hello, {name}!";
        }
    }

    private void OnVolumeChanged(object? sender, ValueChangedEventArgs e)
    {
        var volume = (int)e.NewValue;
        VolumeLabel.Text = $"Volume: {volume}%";
        VolumeProgress.Progress = volume / 100.0;
    }

    private void OnColorSelected(object? sender, EventArgs e)
    {
        if (ColorPicker.SelectedItem is string color)
        {
            SelectedColorLabel.Text = $"Selected: {color}";
        }
    }

    private void OnToggleLoadingClicked(object? sender, EventArgs e)
    {
        LoadingIndicator.IsRunning = !LoadingIndicator.IsRunning;
    }
}
