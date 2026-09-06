namespace Brinell.Samples.Maui.ShellApp.Pages;

public partial class ControlsPage : ContentPage
{
    public ControlsPage()
    {
        InitializeComponent();
    }

    private void OnRecord(object? sender, EventArgs e)
        => ShellControlsResult.Text = "recorded";
}
