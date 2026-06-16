using CommunityToolkit.Maui.Views;

namespace Brinell.Samples.Maui.App.Views2;

public partial class SamplePopup : Popup
{
    public SamplePopup()
    {
        InitializeComponent();
    }

    private void OnOkClicked(object? sender, EventArgs e)
    {
        Close("OK");
    }

    private void OnCancelClicked(object? sender, EventArgs e)
    {
        Close("Cancel");
    }
}
