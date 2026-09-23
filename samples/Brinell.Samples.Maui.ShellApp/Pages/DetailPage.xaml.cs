namespace Brinell.Samples.Maui.ShellApp.Pages;

public partial class DetailPage : ContentPage
{
    public DetailPage()
    {
        InitializeComponent();
    }

    private async void OnPushSubPage(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync(AppShell.DetailSubRoute);
}
