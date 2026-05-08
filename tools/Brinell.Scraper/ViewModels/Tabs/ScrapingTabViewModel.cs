using System.ComponentModel;
using System.Windows.Input;

namespace Brinell.Scraper.ViewModels.Tabs;

public sealed class ScrapingTabViewModel : ViewModelBase
{
    private bool _isSessionPanelVisible = true;

    public ScrapingTabViewModel(
        BrowserViewModel browser,
        InspectorViewModel inspector,
        RecordingViewModel recording,
        SessionPanelViewModel session)
    {
        Browser = browser;
        Inspector = inspector;
        Recording = recording;
        Session = session;

        Inspector.PropertyChanged += OnInspectorPropertyChanged;

        ToggleSessionPanelCommand = new RelayCommand(
            () => IsSessionPanelVisible = !IsSessionPanelVisible);
    }

    public BrowserViewModel Browser { get; }
    public InspectorViewModel Inspector { get; }
    public RecordingViewModel Recording { get; }
    public SessionPanelViewModel Session { get; }

    public bool IsSessionPanelVisible
    {
        get => _isSessionPanelVisible;
        set => SetProperty(ref _isSessionPanelVisible, value);
    }

    public bool IsInspectorVisible => Inspector.IsInspecting;

    public ICommand ToggleSessionPanelCommand { get; }

    private void OnInspectorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(InspectorViewModel.IsInspecting))
            OnPropertyChanged(nameof(IsInspectorVisible));
    }
}
