using Brinell.Samples.Maui.App.ViewModels;

namespace Brinell.Samples.Maui.App.Pages;

public partial class AdvancedPage : ContentPage
{
    public AdvancedPage()
    {
        InitializeComponent();
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (BindingContext is AdvancedViewModel vm)
        {
            vm.OnPanUpdated(e.TotalX, e.TotalY, e.StatusType == GestureStatus.Completed);
        }
    }

    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (BindingContext is AdvancedViewModel vm)
        {
            vm.OnPinchUpdated(e.Scale);
        }
    }
}
