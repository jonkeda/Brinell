using System.ComponentModel;
using System.Collections.Specialized;
using System.Windows.Input;

namespace Brinell.Scraper.ViewModels.Tabs;

public sealed class ScrapingTabViewModel : ViewModelBase, IDisposable
{
    private bool _isSessionPanelVisible = true;
    private bool _disposed;

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
        Recording.SessionSnapshots.CollectionChanged += OnSessionSnapshotsChanged;

        Session.SetNavigateCallback(url =>
        {
            Browser.AddressUrl = url;
            Browser.NavigateCommand.Execute(null);
        });

        Session.SyncRecordedPages(Recording.SessionSnapshots);

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

    private void OnSessionSnapshotsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Session.SyncRecordedPages(Recording.SessionSnapshots);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Inspector.PropertyChanged -= OnInspectorPropertyChanged;
        Recording.SessionSnapshots.CollectionChanged -= OnSessionSnapshotsChanged;
    }
}
