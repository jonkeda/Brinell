namespace Brinell.Samples.Maui.App.Views2.TestViews;

/// <summary>
/// Code-behind for the dialogs demo.
/// </summary>
/// <remarks>
/// The dialog calls live here rather than in the view model because DisplayAlert and
/// DisplayPromptAsync are Page methods - a view model has no Page to raise them on. The
/// result is pushed back into the view model so tests observe it the same way as every
/// other module.
/// </remarks>
public partial class DialogsView : ContentView
{
    public DialogsView()
    {
        InitializeComponent();
    }

    private ViewModels.DialogsViewModel? ViewModel => BindingContext as ViewModels.DialogsViewModel;

    private async void OnShowAlert(object? sender, EventArgs e)
    {
        var page = GetPage();
        if (page == null) return;

        await page.DisplayAlert("Alert", "This is an alert.", "OK");
        ViewModel?.Record("alert dismissed");
    }

    private async void OnShowConfirm(object? sender, EventArgs e)
    {
        var page = GetPage();
        if (page == null) return;

        var accepted = await page.DisplayAlert("Confirm", "Proceed?", "Yes", "No");
        ViewModel?.Record(accepted ? "confirmed" : "declined");
    }

    private async void OnShowPrompt(object? sender, EventArgs e)
    {
        var page = GetPage();
        if (page == null) return;

        var answer = await page.DisplayPromptAsync("Prompt", "Enter a value", "OK", "Cancel");
        ViewModel?.Record(answer == null ? "prompt cancelled" : $"prompt: {answer}");
    }

    /// <summary>
    /// Finds the hosting page. Returns null rather than throwing when the view is not yet
    /// attached, which happens during teardown.
    /// </summary>
    private Page? GetPage()
    {
        Element? element = this;
        while (element != null && element is not Page)
        {
            element = element.Parent;
        }

        return element as Page;
    }
}
