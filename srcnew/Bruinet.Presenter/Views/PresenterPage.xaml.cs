using System.ComponentModel;
using Bruinet.Presenter.ViewModels;

namespace Bruinet.Presenter.Views;

public partial class PresenterPage : ContentPage
{
    private const int ScenariosRow = 2;
    private const int StepsRow = 4;

    private readonly PresenterShellViewModel _viewModel;

    public PresenterPage(PresenterShellViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        viewModel.PropertyChanged += OnViewModelChanged;
        SizeSections();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
#if WINDOWS
        // Keys before the focused control sees them, so Space steps rather than pressing a button;
        // a text box keeps its keys.
        if (Handler?.PlatformView is Microsoft.UI.Xaml.UIElement element)
        {
            element.PreviewKeyDown -= OnPreviewKeyDown;
            element.PreviewKeyDown += OnPreviewKeyDown;
        }
#endif
    }

#if WINDOWS
    private void OnPreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.OriginalSource is Microsoft.UI.Xaml.Controls.TextBox or Microsoft.UI.Xaml.Controls.ComboBox)
        {
            return;
        }

        var control = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        e.Handled = _viewModel.HandleKey(e.Key.ToString(), control);
    }
#endif

    // A RowDefinition is not in the visual tree, so its height cannot be bound: a collapsed
    // section gives its row no height, and the open one takes the room.
    private void SizeSections()
    {
        // Narrator mode shows the script's tree in the Scenarios row, whatever that section's state.
        TreeTab.RowDefinitions[ScenariosRow].Height = _viewModel.IsScenariosExpanded || _viewModel.IsNarratorMode ? GridLength.Star : new GridLength(0);
        TreeTab.RowDefinitions[StepsRow].Height = _viewModel.IsStepsExpanded ? GridLength.Star : new GridLength(0);
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(PresenterShellViewModel.IsScenariosExpanded):
            case nameof(PresenterShellViewModel.IsStepsExpanded):
            case nameof(PresenterShellViewModel.IsNarratorMode):
                SizeSections();
                break;
            case nameof(PresenterShellViewModel.CurrentRow) when _viewModel.CurrentRow is { } row:
                StepList.ScrollTo(row, position: ScrollToPosition.MakeVisible, animate: false);
                break;
        }
    }
}
