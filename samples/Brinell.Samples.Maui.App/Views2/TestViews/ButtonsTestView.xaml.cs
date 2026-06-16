namespace Brinell.Samples.Maui.App.Views2.TestViews;

using Brinell.Samples.Maui.App.ViewModels2.TestViewModels;

public partial class ButtonsTestView : ContentPage
{
    public ButtonsTestView()
    {
        InitializeComponent();
        BindingContext = new ButtonsTestViewModel();
    }
}
