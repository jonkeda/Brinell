# Step 1.2 — MVVM Foundation

## Objective

Build the custom lightweight MVVM infrastructure — ViewModelBase, commands, async commands, and DI wiring — before any UI work that depends on data binding.

## Dependencies

- Step 1.1 (project exists)
- NuGet: `Microsoft.Extensions.DependencyInjection`

## Implementation

### ViewModelBase

Implements `INotifyPropertyChanged` with `SetProperty<T>()` and `OnPropertyChanged()`.

```csharp
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
```

### RelayCommand / RelayCommand\<T\>

Minimal `ICommand` implementation with `Action` / `Func` delegates.

```csharp
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
```

`RelayCommand<T>` follows the same pattern with `Action<T?>` and `Func<T?, bool>`.

### AsyncRelayCommand / AsyncRelayCommand\<T\>

Async command wrapping `Func<CancellationToken, Task>` with `IsRunning` property and cancellation support.

```csharp
public class AsyncRelayCommand : ViewModelBase, ICommand
{
    private readonly Func<CancellationToken, Task> _execute;
    private readonly Func<bool>? _canExecute;
    private CancellationTokenSource? _cts;
    private bool _isRunning;

    public bool IsRunning { get => _isRunning; private set => SetProperty(ref _isRunning, value); }
    public bool CanExecute(object? parameter) => !IsRunning && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (IsRunning) return;
        _cts = new CancellationTokenSource();
        IsRunning = true;
        RaiseCanExecuteChanged();
        try { await _execute(_cts.Token); }
        finally { IsRunning = false; _cts.Dispose(); _cts = null; RaiseCanExecuteChanged(); }
    }

    public void Cancel() => _cts?.Cancel();
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    public event EventHandler? CanExecuteChanged;
}
```

- Inherits `ViewModelBase` so `IsRunning` raises `PropertyChanged` (bindable to loading indicators)
- `CanExecute` returns `false` while running — prevents double execution

### DI Container Setup

In `App.xaml.cs` (remove default `StartupUri` from `App.xaml`):

```csharp
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<SiteSelectionViewModel>();
        services.AddTransient<BrowserViewModel>();
        services.AddTransient<SidebarViewModel>();
        services.AddTransient<InspectorViewModel>();
        services.AddTransient<CodePreviewViewModel>();
        services.AddTransient<RecordingViewModel>();
        services.AddTransient<AnalysisViewModel>();
        services.AddTransient<ControlsManagerViewModel>();
        services.AddTransient<GenerationViewModel>();
        services.AddTransient<CorpusBrowserViewModel>();

        // Services (registered in later phases)
        // services.AddSingleton<ICorpusService, CorpusService>();
        // services.AddSingleton<IAnalysisService, AnalysisService>();
        // ...

        services.AddTransient<MainWindow>();
        Services = services.BuildServiceProvider();

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
```

### ViewModels

| ViewModel | Responsibility |
|---|---|
| `MainViewModel` | Active view, sidebar state, menu commands, window title, status bar |
| `SiteSelectionViewModel` | Site corpus list, create new site, URL alias management |
| `BrowserViewModel` | WebView2 nav — `AddressUrl`, `IsLoading`, `StatusText`, nav commands |
| `SidebarViewModel` | Page list with status icons, control list, corpus stats |
| `InspectorViewModel` | DOM tree, element selection, highlight toggle |
| `CodePreviewViewModel` | Generated code display, Roslyn validation, copy/save |
| `RecordingViewModel` | Recording session — captured pages, timer, start/stop/pause |
| `AnalysisViewModel` | Proposed custom controls, confidence %, approve/reject |
| `ControlsManagerViewModel` | Custom controls CRUD, generate/edit/delete |
| `GenerationViewModel` | Batch page generation progress, token stats |
| `CorpusBrowserViewModel` | All recorded snapshots, sort/filter, diff view |

## Key decisions

- **No CommunityToolkit.Mvvm** — custom lightweight implementation
- Constructor injection is the primary pattern; `App.Services` static property for edge cases only
- ViewModels communicate via events or a simple messenger (no tight coupling)
- `MainViewModel` orchestrates view switching and holds the active `SiteCorpus` reference

## Checklist

- [ ] `ViewModelBase.SetProperty<T>()` raises `PropertyChanged` only when value changes
- [ ] `RelayCommand` executes and evaluates `CanExecute`
- [ ] `AsyncRelayCommand` sets `IsRunning = true` during execution, prevents re-entry
- [ ] `AsyncRelayCommand.Cancel()` cancels the running operation
- [ ] All ViewModels resolved from DI container — no `new ViewModel()` calls
- [ ] `MainWindow.DataContext` is set to `MainViewModel` via DI
- [ ] Solution builds, app launches with DI wired up
