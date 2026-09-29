using System.ComponentModel;
using Brinell.Presenter.ViewModels;

namespace Brinell.Presenter.Views;

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

    // A RowDefinition is not in the visual tree, so its height cannot be bound: a collapsed
    // section gives its row no height, and the open one takes the room.
    private void SizeSections()
    {
        TreeTab.RowDefinitions[ScenariosRow].Height = _viewModel.IsScenariosExpanded ? GridLength.Star : new GridLength(0);
        TreeTab.RowDefinitions[StepsRow].Height = _viewModel.IsStepsExpanded ? GridLength.Star : new GridLength(0);
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(PresenterShellViewModel.IsScenariosExpanded):
            case nameof(PresenterShellViewModel.IsStepsExpanded):
                SizeSections();
                break;
            case nameof(PresenterShellViewModel.CurrentRow) when _viewModel.CurrentRow is { } row:
                StepList.ScrollTo(row, position: ScrollToPosition.MakeVisible, animate: false);
                break;
        }
    }
}
