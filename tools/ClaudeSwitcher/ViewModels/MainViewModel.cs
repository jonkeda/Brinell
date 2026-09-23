using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using ClaudeSwitcher.Mvvm;
using ClaudeSwitcher.Services;
using ClaudeSwitcher.Views;

namespace ClaudeSwitcher.ViewModels;

public sealed class MainViewModel : ParentViewModel
{
    private static readonly Brush SuccessBrush =
        Freeze(new SolidColorBrush(Color.FromRgb(0x3F, 0x6B, 0x3A)));
    private static readonly Brush ErrorBrush =
        Freeze(new SolidColorBrush(Color.FromRgb(0xB0, 0x3A, 0x2E)));

    private readonly ProfileStore _store;
    private ProfileViewModel? _selectedProfile;
    private string _status = string.Empty;
    private bool _statusIsError;

    public MainViewModel() : this(new ProfileStore(new FileManager())) { }

    public MainViewModel(ProfileStore store)
    {
        _store = store;

        Profiles = new ObservableCollection<ProfileViewModel>();

        ActivateCommand = new AsyncRelayCommand(this, ActivateAsync,
            () => SelectedProfile is not null && !IsBusy);
        CaptureCommand = new AsyncRelayCommand(this, CaptureAsync, () => !IsBusy);
        DeleteCommand = new AsyncRelayCommand(this, DeleteAsync,
            () => SelectedProfile is not null && !IsBusy);
        ClearLiveCommand = new AsyncRelayCommand(this, ClearLiveAsync, () => !IsBusy);
        RefreshCommand = new AsyncRelayCommand(this, RefreshAsync, () => !IsBusy);

        _ = RefreshAsync();
    }

    public ObservableCollection<ProfileViewModel> Profiles { get; }

    public ProfileViewModel? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (SetProperty(ref _selectedProfile, value))
            {
                ActivateCommand.NotifyCanExecuteChanged();
                DeleteCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public Brush StatusBrush => _statusIsError ? ErrorBrush : SuccessBrush;

    public AsyncRelayCommand ActivateCommand { get; }
    public AsyncRelayCommand CaptureCommand { get; }
    public AsyncRelayCommand DeleteCommand { get; }
    public AsyncRelayCommand ClearLiveCommand { get; }
    public AsyncRelayCommand RefreshCommand { get; }

    private async Task RefreshAsync()
    {
        try
        {
            var entries = await _store.ListAsync();
            Profiles.Clear();
            foreach (var e in entries)
                Profiles.Add(new ProfileViewModel(e));
            SetStatus($"{Profiles.Count} profile(s) found.", error: false);
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, error: true);
        }
    }

    private async Task ActivateAsync()
    {
        if (SelectedProfile is null) return;
        var name = SelectedProfile.Name;
        try
        {
            await _store.ActivateAsync(name);
            SetStatus($"Switched to {name}", error: false);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            SetStatus($"Activate failed: {ex.Message}", error: true);
        }
    }

    private async Task CaptureAsync()
    {
        var name = NameInputDialog.Prompt(Application.Current?.MainWindow);
        if (string.IsNullOrWhiteSpace(name)) return;
        try
        {
            await _store.CaptureAsync(name);
            SetStatus($"Captured current session as {name}", error: false);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            SetStatus($"Capture failed: {ex.Message}", error: true);
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedProfile is null) return;
        var name = SelectedProfile.Name;
        var confirm = MessageBox.Show(
            $"Delete profile '{name}'? Files under %APPDATA%\\ClaudeSwitcher\\{name} will be removed.",
            "ClaudeSwitcher", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            await _store.DeleteAsync(name);
            SetStatus($"Deleted {name}", error: false);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            SetStatus($"Delete failed: {ex.Message}", error: true);
        }
    }

    private async Task ClearLiveAsync()
    {
        var confirm = MessageBox.Show(
            "Remove the current Claude Code login?\n\n" +
            "This deletes:\n" +
            $"  {_store.LiveCredentialsPath}\n" +
            $"  {_store.LiveClaudeJsonPath}\n\n" +
            "Stored profiles under %APPDATA%\\ClaudeSwitcher are not touched. " +
            "Capture the current session first if you have not already.",
            "ClaudeSwitcher", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            var result = await _store.ClearLiveAsync();
            SetStatus(
                result.AnythingRemoved
                    ? "Cleared current login. Run `claude login` to sign in again."
                    : "Nothing to clear: no live Claude Code files were present.",
                error: false);
        }
        catch (Exception ex)
        {
            SetStatus($"Clear failed: {ex.Message}", error: true);
        }
    }

    private void SetStatus(string message, bool error)
    {
        _statusIsError = error;
        Status = message;
        OnPropertyChanged(nameof(StatusBrush));
    }

    private static Brush Freeze(SolidColorBrush brush)
    {
        brush.Freeze();
        return brush;
    }
}
