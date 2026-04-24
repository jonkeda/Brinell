# Step 07a-6 — Wire Session Captures to Sidebar

## Objective

When recording captures a page on navigation, add it to the sidebar's "This Session" list in real-time. Also wire sidebar page clicks to navigate the browser.

## Current State

In `MainViewModel.OnNavigationSucceeded()`:
```csharp
if (Recording.IsRecording)
{
    var webView = Browser.GetCoreWebView2?.Invoke();
    if (webView is not null)
    {
        var snapshot = await _domCapture.CaptureAsync(webView);
        snapshot.SiteName = ActiveSite?.Name ?? "";
        snapshot.PageName = snapshot.PageTitle;
        Recording.OnPageTransition(snapshot.PageUrl, snapshot);
    }
}
```

`Recording.OnPageTransition` adds to `SessionSnapshots` but nothing feeds the sidebar.

## Changes

### 1. Update `MainViewModel.OnNavigationSucceeded()` — feed sidebar

After calling `Recording.OnPageTransition`, if it returns `true` (page was captured, not a dedup skip), also add to the sidebar:

```csharp
if (Recording.IsRecording)
{
    var webView = Browser.GetCoreWebView2?.Invoke();
    if (webView is not null)
    {
        var snapshot = await _domCapture.CaptureAsync(webView);
        snapshot.SiteName = ActiveSite?.Name ?? "";
        snapshot.PageName = snapshot.PageTitle;

        if (Recording.OnPageTransition(snapshot.PageUrl, snapshot))
        {
            Sidebar.AddSessionPage(snapshot);
        }
    }
}
```

### 2. Update `MainViewModel.ToggleRecording()` — sync sidebar recording state

```csharp
private void ToggleRecording()
{
    if (Recording.IsRecording)
    {
        Recording.StopRecording();
        Sidebar.IsRecording = false;
    }
    else
    {
        Recording.StartRecording();
        Sidebar.IsRecording = true;
    }
}
```

### 3. Add click-to-navigate on sidebar pages

Add a `NavigateToPageCommand` to `SidebarViewModel`:

```csharp
public ICommand NavigateToPageCommand { get; }

// In constructor (needs BrowserViewModel or an Action<string> callback):
public SidebarViewModel()
{
    NavigateToPageCommand = new RelayCommand<SidebarPageItem>(NavigateToPage);
}

private Action<string>? _navigateCallback;

public void SetNavigateCallback(Action<string> callback)
{
    _navigateCallback = callback;
}

private void NavigateToPage(SidebarPageItem? item)
{
    if (item?.Url is { Length: > 0 } url)
        _navigateCallback?.Invoke(url);
}
```

Wire in `MainViewModel` constructor:
```csharp
Sidebar.SetNavigateCallback(url =>
{
    Browser.AddressUrl = url;
    Browser.NavigateCommand.Execute(null);
});
```

In XAML, use `InputBindings` or `MouseLeftButtonUp` on the page items:
```xml
<TextBlock Margin="4,1" Cursor="Hand" FontSize="11"
           MouseLeftButtonUp="OnSidebarPageClicked">
    <Run Text="{Binding StatusIcon, Mode=OneWay}"/>
    <Run Text=" "/>
    <Run Text="{Binding Name, Mode=OneWay}"/>
</TextBlock>
```

Or use a `Button` styled as a `TextBlock` with the command:
```xml
<Button Command="{Binding DataContext.Sidebar.NavigateToPageCommand,
                  RelativeSource={RelativeSource AncestorType=Window}}"
        CommandParameter="{Binding}"
        Style="{StaticResource LinkButtonStyle}">
    <TextBlock FontSize="11">
        <Run Text="{Binding StatusIcon, Mode=OneWay}"/>
        <Run Text=" "/>
        <Run Text="{Binding Name, Mode=OneWay}"/>
    </TextBlock>
</Button>
```

The `LinkButtonStyle` removes button chrome (no border, no background, hand cursor).

### 4. Add `RelayCommand<T>` if not already present

Check if `RelayCommand<T>` (generic) exists. If only `RelayCommand` (non-generic) exists, add a typed version:

```csharp
public sealed class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) =>
        _canExecute?.Invoke(parameter is T t ? t : default) ?? true;

    public void Execute(object? parameter) =>
        _execute(parameter is T t ? t : default);

    public void RaiseCanExecuteChanged() =>
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
```

## Files Modified

| File | Action |
|------|--------|
| `ViewModels/MainViewModel.cs` | **Edit** — update `OnNavigationSucceeded` + `ToggleRecording` |
| `ViewModels/SidebarViewModel.cs` | **Edit** — add `NavigateToPageCommand` + `SetNavigateCallback` |
| `ViewModels/RelayCommand.cs` | **Edit** — add `RelayCommand<T>` if missing |
| `MainWindow.xaml` | **Edit** — wire click-to-navigate on page items |

## Verification

- Build succeeds
- Start recording → navigate to pages → "This Session" list updates in real-time
- Dedup'd pages (same URL < 2s) don't appear twice
- Click a page in sidebar → browser navigates to that URL
- All existing tests pass

## Checklist

- [ ] `OnNavigationSucceeded` adds captured page to `Sidebar.SessionPages`
- [ ] `ToggleRecording` syncs `Sidebar.IsRecording`
- [ ] Sidebar pages are clickable → navigate browser
- [ ] `RelayCommand<T>` exists for typed command parameter
- [ ] Build succeeds, tests pass
