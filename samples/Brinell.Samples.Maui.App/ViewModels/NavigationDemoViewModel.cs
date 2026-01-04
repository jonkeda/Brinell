using System.Collections.ObjectModel;
using Brinell.Samples.Shared.Commands;
using Brinell.Samples.Shared.ViewModels;

namespace Brinell.Samples.Maui.App.ViewModels;

/// <summary>
/// ViewModel for the NavigationDemo page demonstrating expander, flyout, toolbar, and menu controls.
/// </summary>
public class NavigationDemoViewModel : ViewModelBase
{
    private bool _expander1Expanded = true;
    private bool _expander2Expanded;
    private bool _expander3Expanded;
    private string _navigationResult = string.Empty;

    public bool Expander1Expanded
    {
        get => _expander1Expanded;
        set => SetProperty(ref _expander1Expanded, value);
    }

    public bool Expander2Expanded
    {
        get => _expander2Expanded;
        set => SetProperty(ref _expander2Expanded, value);
    }

    public bool Expander3Expanded
    {
        get => _expander3Expanded;
        set => SetProperty(ref _expander3Expanded, value);
    }

    public string NavigationResult
    {
        get => _navigationResult;
        set => SetProperty(ref _navigationResult, value);
    }

    public ObservableCollection<string> ExpanderContent1 { get; } = new()
    {
        "Content item 1.1",
        "Content item 1.2",
        "Content item 1.3"
    };

    public ObservableCollection<string> ExpanderContent2 { get; } = new()
    {
        "Content item 2.1",
        "Content item 2.2"
    };

    public ObservableCollection<string> ExpanderContent3 { get; } = new()
    {
        "Content item 3.1",
        "Content item 3.2",
        "Content item 3.3",
        "Content item 3.4"
    };

    public IAsyncRelayCommand PushPageCommand { get; }
    public IAsyncRelayCommand PopPageCommand { get; }
    public IAsyncRelayCommand ModalPageCommand { get; }
    public IAsyncRelayCommand PopToRootCommand { get; }
    public IAsyncRelayCommand ExpandAllCommand { get; }
    public IAsyncRelayCommand CollapseAllCommand { get; }
    public IAsyncRelayCommand ToolbarSaveCommand { get; }
    public IAsyncRelayCommand ToolbarEditCommand { get; }
    public IAsyncRelayCommand ToolbarDeleteCommand { get; }
    public IAsyncRelayCommand OpenFlyoutCommand { get; }

    public NavigationDemoViewModel()
    {
        PushPageCommand = new AsyncRelayCommand(this, PushPageAsync);
        PopPageCommand = new AsyncRelayCommand(this, PopPageAsync);
        ModalPageCommand = new AsyncRelayCommand(this, ModalPageAsync);
        PopToRootCommand = new AsyncRelayCommand(this, PopToRootAsync);
        ExpandAllCommand = new AsyncRelayCommand(this, ExpandAllAsync);
        CollapseAllCommand = new AsyncRelayCommand(this, CollapseAllAsync);
        ToolbarSaveCommand = new AsyncRelayCommand(this, ToolbarSaveAsync);
        ToolbarEditCommand = new AsyncRelayCommand(this, ToolbarEditAsync);
        ToolbarDeleteCommand = new AsyncRelayCommand(this, ToolbarDeleteAsync);
        OpenFlyoutCommand = new AsyncRelayCommand(this, OpenFlyoutAsync);
    }

    private async Task PushPageAsync()
    {
        NavigationResult = "Push page requested";
        await Task.CompletedTask;
    }

    private async Task PopPageAsync()
    {
        NavigationResult = "Pop page requested";
        await Task.CompletedTask;
    }

    private async Task ModalPageAsync()
    {
        NavigationResult = "Modal page requested";
        await Task.CompletedTask;
    }

    private async Task PopToRootAsync()
    {
        NavigationResult = "Pop to root requested";
        await Task.CompletedTask;
    }

    private async Task ExpandAllAsync()
    {
        Expander1Expanded = true;
        Expander2Expanded = true;
        Expander3Expanded = true;
        await Task.CompletedTask;
    }

    private async Task CollapseAllAsync()
    {
        Expander1Expanded = false;
        Expander2Expanded = false;
        Expander3Expanded = false;
        await Task.CompletedTask;
    }

    private async Task ToolbarSaveAsync()
    {
        NavigationResult = "Toolbar: Save clicked";
        await Task.CompletedTask;
    }

    private async Task ToolbarEditAsync()
    {
        NavigationResult = "Toolbar: Edit clicked";
        await Task.CompletedTask;
    }

    private async Task ToolbarDeleteAsync()
    {
        NavigationResult = "Toolbar: Delete clicked";
        await Task.CompletedTask;
    }

    private async Task OpenFlyoutAsync()
    {
        NavigationResult = "Flyout opened";
        await Task.CompletedTask;
    }
}
