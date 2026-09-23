namespace Brinell.Samples.Maui.ShellApp.Pages;

public partial class DetailSubPage : ContentPage
{
    public DetailSubPage()
    {
        InitializeComponent();
    }

    private async void OnGoBack(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
}
