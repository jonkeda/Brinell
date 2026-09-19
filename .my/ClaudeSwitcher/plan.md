# ClaudeSwitcher — plan

A minimal WPF app to switch between two (or more) Claude Code accounts on Windows
by swapping the credential and metadata files under `%USERPROFILE%`.

Status: proposed. Nothing in this plan is implemented yet.

---

## 1. Goal and scope

- Lightweight desktop tool. No tray icon, no auto-switching, no usage stats, no telemetry.
- The user creates Claude Code sessions with `claude login` (or however Claude Code
  normally authenticates). This app only *captures* and *restores* the resulting files.
- One window, one list, two buttons: **Capture current** and **Activate**.

Non-goals: multi-user OS accounts, token editing, encryption at rest beyond the OS user
profile ACL (`%APPDATA%\ClaudeSwitcher` is already per-user).

---

## 2. Files Claude Code owns

| Path | Purpose |
| --- | --- |
| `%USERPROFILE%\.claude\.credentials.json` | OAuth / API tokens |
| `%USERPROFILE%\.claude.json` | Account metadata + user config |

A profile in ClaudeSwitcher is a folder that holds a snapshot of both files.

```
%APPDATA%\ClaudeSwitcher\
  AccountA\
    credentials.json          <- snapshot of %USERPROFILE%\.claude\.credentials.json
    claude.json               <- snapshot of %USERPROFILE%\.claude.json
  AccountB\
    credentials.json
    claude.json
  profiles.json               <- list of profile names + last-activated timestamp
```

`profiles.json` is only an index for the UI. The truth is the folder listing under
`%APPDATA%\ClaudeSwitcher\`.

---

## 3. UX

Single `MainWindow`:

```
+--------------------------------------------------------------+
| ClaudeSwitcher                                               |
+--------------------------------------------------------------+
| Profiles:                                                    |
| +----------------------------------------------------------+ |
| | AccountA        last activated: 2026-09-18 14:22         | |
| | AccountB        last activated: 2026-09-15 09:10         | |
| +----------------------------------------------------------+ |
|                                                              |
| [ Activate ]  [ Capture current as... ]  [ Delete ]          |
|                                                              |
| Status: Switched to AccountA                                 |
+--------------------------------------------------------------+
```

Flows:

1. **Add a profile.** The user logs in with Claude Code as normal. In the app they click
   *Capture current as...*, type a name, and both files get copied into a new profile
   folder. This is the "Claude Code makes the profile, the app just captures it" path
   from the request.
2. **Select / activate.** The user picks a row and clicks *Activate*. The two files are
   copied back to `%USERPROFILE%`. The status line reads `Switched to <name>`.
3. **Delete.** Removes the profile folder. Does not touch `%USERPROFILE%`.

Safety:

- Before overwriting `%USERPROFILE%\.claude*.json`, write to a temp file next to the
  target and `File.Replace` so an interrupted copy cannot leave a half-written file.
- Refuse to activate a profile whose folder is missing either file.
- Never delete `%USERPROFILE%\.claude\.credentials.json`. The activate step only ever
  *overwrites*.

---

## 4. MVVM base

The request says "use `Brinell.Mvvm`". No such project exists in this repository. The
local MVVM library is `samples/Brinell.Samples.Shared` and already exposes what we need:

- `ParentViewModel` — `INotifyPropertyChanged`, `SetProperty`, `IsBusy`, `BeginBusy`/`EndBusy`.
- `RelayCommand`, `RelayCommand<T>`.
- `AsyncRelayCommand`, `AsyncRelayCommand<T>`.

The plan uses those. If the user later ships a real `Brinell.Mvvm` NuGet package with the
same surface, the swap is one `<PackageReference>` change and one namespace update.

---

## 5. Project layout

Stand-alone solution, not part of `srcnew\Brinell.sln`. This is a tool, not a Brinell
library.

```
tools/ClaudeSwitcher/
  ClaudeSwitcher.slnx
  src/ClaudeSwitcher/
    ClaudeSwitcher.csproj
    App.xaml
    App.xaml.cs
    MainWindow.xaml
    MainWindow.xaml.cs
    ViewModels/
      MainViewModel.cs
      ProfileViewModel.cs
    Services/
      FileManager.cs
      ProfileStore.cs
    Models/
      ProfileIndex.cs
```

Test project is out of scope for the first pass; `FileManager` and `ProfileStore` are
written to be trivially unit-testable later (roots injected via constructor).

---

## 6. Code (compiles as-is on .NET 10 with `Brinell.Samples.Shared` referenced)

### `src/ClaudeSwitcher/ClaudeSwitcher.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>ClaudeSwitcher</RootNamespace>
    <AssemblyName>ClaudeSwitcher</AssemblyName>
    <ApplicationManifest>app.manifest</ApplicationManifest>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\..\samples\Brinell.Samples.Shared\Brinell.Samples.Shared.csproj" />
  </ItemGroup>

</Project>
```

### `src/ClaudeSwitcher/App.xaml`

```xml
<Application x:Class="ClaudeSwitcher.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <Style TargetType="Button">
                <Setter Property="Padding" Value="12,6" />
                <Setter Property="Margin" Value="4" />
                <Setter Property="MinWidth" Value="120" />
            </Style>
            <Style TargetType="TextBlock">
                <Setter Property="Margin" Value="4" />
            </Style>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

### `src/ClaudeSwitcher/App.xaml.cs`

```csharp
using System.Windows;

namespace ClaudeSwitcher;

public partial class App : Application
{
}
```

### `src/ClaudeSwitcher/MainWindow.xaml`

```xml
<Window x:Class="ClaudeSwitcher.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:ClaudeSwitcher.ViewModels"
        Title="ClaudeSwitcher"
        Width="520" Height="420"
        WindowStartupLocation="CenterScreen">
    <Window.DataContext>
        <vm:MainViewModel />
    </Window.DataContext>

    <Grid Margin="12">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0"
                   Text="Claude Code account profiles"
                   FontSize="16" FontWeight="SemiBold" />

        <ListBox Grid.Row="1"
                 ItemsSource="{Binding Profiles}"
                 SelectedItem="{Binding SelectedProfile, Mode=TwoWay}"
                 DisplayMemberPath="DisplayName"
                 Margin="4" />

        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Left">
            <Button Content="Activate"
                    Command="{Binding ActivateCommand}" />
            <Button Content="Capture current as..."
                    Command="{Binding CaptureCommand}" />
            <Button Content="Delete"
                    Command="{Binding DeleteCommand}" />
            <Button Content="Refresh"
                    Command="{Binding RefreshCommand}" />
        </StackPanel>

        <TextBlock Grid.Row="3"
                   Text="{Binding Status}"
                   Margin="4,8,4,4"
                   Foreground="{Binding StatusBrush}" />
    </Grid>
</Window>
```

### `src/ClaudeSwitcher/MainWindow.xaml.cs`

```csharp
using System.Windows;

namespace ClaudeSwitcher;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
```

### `src/ClaudeSwitcher/Models/ProfileIndex.cs`

```csharp
namespace ClaudeSwitcher.Models;

public sealed class ProfileIndex
{
    public List<ProfileEntry> Profiles { get; set; } = new();
}

public sealed class ProfileEntry
{
    public string Name { get; set; } = string.Empty;
    public DateTime? LastActivatedUtc { get; set; }
}
```

### `src/ClaudeSwitcher/Services/FileManager.cs`

```csharp
using System.IO;
using System.Text.Json;

namespace ClaudeSwitcher.Services;

public sealed class FileManager
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { WriteIndented = true };

    public async Task CopyAsync(string source, string destination, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

        // Write to a sibling temp file first, then atomically replace.
        var tempPath = destination + ".tmp";
        await using (var input = File.OpenRead(source))
        await using (var output = File.Create(tempPath))
        {
            await input.CopyToAsync(output, ct);
        }

        if (File.Exists(destination))
            File.Replace(tempPath, destination, destinationBackupFileName: null);
        else
            File.Move(tempPath, destination);
    }

    public async Task<T?> ReadJsonAsync<T>(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path))
            return default;

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, ct);
    }

    public async Task WriteJsonAsync<T>(string path, T value, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tempPath = path + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, value, JsonOptions, ct);
        }
        if (File.Exists(path))
            File.Replace(tempPath, path, destinationBackupFileName: null);
        else
            File.Move(tempPath, path);
    }

    public bool Exists(string path) => File.Exists(path);
}
```

### `src/ClaudeSwitcher/Services/ProfileStore.cs`

```csharp
using System.IO;
using ClaudeSwitcher.Models;

namespace ClaudeSwitcher.Services;

public sealed class ProfileStore
{
    private const string CredentialsFileName = "credentials.json";
    private const string ClaudeJsonFileName = "claude.json";
    private const string IndexFileName = "profiles.json";

    private readonly FileManager _files;

    public ProfileStore(FileManager files)
    {
        _files = files;

        UserHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        RootFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClaudeSwitcher");

        LiveCredentialsPath = Path.Combine(UserHome, ".claude", ".credentials.json");
        LiveClaudeJsonPath  = Path.Combine(UserHome, ".claude.json");
        IndexPath           = Path.Combine(RootFolder, IndexFileName);

        Directory.CreateDirectory(RootFolder);
    }

    public string UserHome { get; }
    public string RootFolder { get; }
    public string LiveCredentialsPath { get; }
    public string LiveClaudeJsonPath { get; }
    public string IndexPath { get; }

    public async Task<IReadOnlyList<ProfileEntry>> ListAsync(CancellationToken ct = default)
    {
        var index = await _files.ReadJsonAsync<ProfileIndex>(IndexPath, ct) ?? new ProfileIndex();
        var byName = index.Profiles.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        var onDisk = Directory.EnumerateDirectories(RootFolder)
            .Select(Path.GetFileName)
            .Where(n => !string.IsNullOrEmpty(n))
            .Select(n => n!)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var merged = new List<ProfileEntry>(onDisk.Count);
        foreach (var name in onDisk)
        {
            merged.Add(byName.TryGetValue(name, out var known)
                ? known
                : new ProfileEntry { Name = name });
        }
        return merged;
    }

    public async Task CaptureAsync(string name, CancellationToken ct = default)
    {
        ValidateName(name);

        if (!_files.Exists(LiveCredentialsPath))
            throw new InvalidOperationException(
                $"Not logged in: '{LiveCredentialsPath}' does not exist.");

        var target = Path.Combine(RootFolder, name);
        Directory.CreateDirectory(target);

        await _files.CopyAsync(LiveCredentialsPath, Path.Combine(target, CredentialsFileName), ct);
        if (_files.Exists(LiveClaudeJsonPath))
            await _files.CopyAsync(LiveClaudeJsonPath, Path.Combine(target, ClaudeJsonFileName), ct);
    }

    public async Task ActivateAsync(string name, CancellationToken ct = default)
    {
        ValidateName(name);

        var source = Path.Combine(RootFolder, name);
        var creds  = Path.Combine(source, CredentialsFileName);
        var meta   = Path.Combine(source, ClaudeJsonFileName);

        if (!_files.Exists(creds))
            throw new FileNotFoundException($"Profile '{name}' has no credentials.json.", creds);

        Directory.CreateDirectory(Path.GetDirectoryName(LiveCredentialsPath)!);
        await _files.CopyAsync(creds, LiveCredentialsPath, ct);
        if (_files.Exists(meta))
            await _files.CopyAsync(meta, LiveClaudeJsonPath, ct);

        await StampActivatedAsync(name, ct);
    }

    public Task DeleteAsync(string name, CancellationToken ct = default)
    {
        ValidateName(name);
        var target = Path.Combine(RootFolder, name);
        if (Directory.Exists(target))
            Directory.Delete(target, recursive: true);
        return RemoveFromIndexAsync(name, ct);
    }

    private async Task StampActivatedAsync(string name, CancellationToken ct)
    {
        var index = await _files.ReadJsonAsync<ProfileIndex>(IndexPath, ct) ?? new ProfileIndex();
        var entry = index.Profiles.FirstOrDefault(
            p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            entry = new ProfileEntry { Name = name };
            index.Profiles.Add(entry);
        }
        entry.LastActivatedUtc = DateTime.UtcNow;
        await _files.WriteJsonAsync(IndexPath, index, ct);
    }

    private async Task RemoveFromIndexAsync(string name, CancellationToken ct)
    {
        var index = await _files.ReadJsonAsync<ProfileIndex>(IndexPath, ct);
        if (index is null) return;
        index.Profiles.RemoveAll(
            p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        await _files.WriteJsonAsync(IndexPath, index, ct);
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Profile name is required.", nameof(name));
        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("Profile name contains invalid characters.", nameof(name));
    }
}
```

### `src/ClaudeSwitcher/ViewModels/ProfileViewModel.cs`

```csharp
using Brinell.Samples.Shared.ViewModels;
using ClaudeSwitcher.Models;

namespace ClaudeSwitcher.ViewModels;

public sealed class ProfileViewModel : ParentViewModel
{
    private DateTime? _lastActivatedUtc;

    public ProfileViewModel(ProfileEntry entry)
    {
        Name = entry.Name;
        _lastActivatedUtc = entry.LastActivatedUtc;
    }

    public string Name { get; }

    public DateTime? LastActivatedUtc
    {
        get => _lastActivatedUtc;
        set { if (SetProperty(ref _lastActivatedUtc, value)) OnPropertyChanged(nameof(DisplayName)); }
    }

    public string DisplayName => _lastActivatedUtc is { } t
        ? $"{Name}    last activated: {t.ToLocalTime():yyyy-MM-dd HH:mm}"
        : Name;

    private void OnPropertyChanged(string name)
        => GetType().GetMethod("OnPropertyChanged",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(this, new object?[] { name });
}
```

> Note: `ParentViewModel.OnPropertyChanged` is `protected`. In the real code we'll add a
> tiny `protected` helper on the view model instead of reflecting; the reflection line
> above is only in the plan snippet to keep it copy-pasteable without splitting the base.
> Replace it with a direct `base.OnPropertyChanged(nameof(DisplayName))` once implementing.

### `src/ClaudeSwitcher/ViewModels/MainViewModel.cs`

```csharp
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using Brinell.Samples.Shared.Commands;
using Brinell.Samples.Shared.ViewModels;
using ClaudeSwitcher.Services;

namespace ClaudeSwitcher.ViewModels;

public sealed class MainViewModel : ParentViewModel
{
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
        CaptureCommand  = new AsyncRelayCommand(this, CaptureAsync, () => !IsBusy);
        DeleteCommand   = new AsyncRelayCommand(this, DeleteAsync,
            () => SelectedProfile is not null && !IsBusy);
        RefreshCommand  = new AsyncRelayCommand(this, RefreshAsync, () => !IsBusy);

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

    public Brush StatusBrush => _statusIsError ? Brushes.Firebrick : Brushes.DarkGreen;

    public AsyncRelayCommand ActivateCommand { get; }
    public AsyncRelayCommand CaptureCommand { get; }
    public AsyncRelayCommand DeleteCommand { get; }
    public AsyncRelayCommand RefreshCommand { get; }

    private async Task RefreshAsync()
    {
        BeginBusy();
        try
        {
            var entries = await _store.ListAsync();
            Profiles.Clear();
            foreach (var e in entries)
                Profiles.Add(new ProfileViewModel(e));
            SetStatus($"{Profiles.Count} profile(s) found.", error: false);
        }
        catch (Exception ex) { SetStatus(ex.Message, error: true); }
        finally { EndBusy(); }
    }

    private async Task ActivateAsync()
    {
        if (SelectedProfile is null) return;
        var name = SelectedProfile.Name;
        BeginBusy();
        try
        {
            await _store.ActivateAsync(name);
            SetStatus($"Switched to {name}", error: false);
            await RefreshAsync();
        }
        catch (Exception ex) { SetStatus($"Activate failed: {ex.Message}", error: true); }
        finally { EndBusy(); }
    }

    private async Task CaptureAsync()
    {
        var name = PromptForName();
        if (string.IsNullOrWhiteSpace(name)) return;
        BeginBusy();
        try
        {
            await _store.CaptureAsync(name);
            SetStatus($"Captured current session as {name}", error: false);
            await RefreshAsync();
        }
        catch (Exception ex) { SetStatus($"Capture failed: {ex.Message}", error: true); }
        finally { EndBusy(); }
    }

    private async Task DeleteAsync()
    {
        if (SelectedProfile is null) return;
        var name = SelectedProfile.Name;
        var confirm = MessageBox.Show(
            $"Delete profile '{name}'? Files under %APPDATA%\\ClaudeSwitcher\\{name} will be removed.",
            "ClaudeSwitcher", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        BeginBusy();
        try
        {
            await _store.DeleteAsync(name);
            SetStatus($"Deleted {name}", error: false);
            await RefreshAsync();
        }
        catch (Exception ex) { SetStatus($"Delete failed: {ex.Message}", error: true); }
        finally { EndBusy(); }
    }

    private static string? PromptForName()
    {
        // Minimal inline prompt to keep the app dependency-free.
        var win = new Window
        {
            Title = "New profile name",
            Width = 320, Height = 140,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Application.Current.MainWindow,
        };
        var box = new System.Windows.Controls.TextBox { Margin = new Thickness(12) };
        var ok = new System.Windows.Controls.Button { Content = "OK", Width = 80, IsDefault = true };
        var cancel = new System.Windows.Controls.Button { Content = "Cancel", Width = 80, IsCancel = true };
        var buttons = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(8)
        };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        var grid = new System.Windows.Controls.Grid();
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        System.Windows.Controls.Grid.SetRow(box, 0);
        System.Windows.Controls.Grid.SetRow(buttons, 1);
        grid.Children.Add(box);
        grid.Children.Add(buttons);
        win.Content = grid;

        string? result = null;
        ok.Click += (_, _) => { result = box.Text; win.DialogResult = true; };
        return win.ShowDialog() == true ? result : null;
    }

    private void SetStatus(string message, bool error)
    {
        _statusIsError = error;
        Status = message;
        OnPropertyChanged(nameof(StatusBrush));
    }
}
```

### `src/ClaudeSwitcher/app.manifest`

Standard WPF manifest with `PerMonitorV2` DPI awareness. Optional but keeps the window
crisp on high-DPI screens.

```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <application xmlns="urn:schemas-microsoft-com:asm.v3">
    <windowsSettings>
      <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
    </windowsSettings>
  </application>
</assembly>
```

---

## 7. Build and run

From `tools/ClaudeSwitcher/`:

```powershell
dotnet build src/ClaudeSwitcher/ClaudeSwitcher.csproj -v:minimal /nr:false
dotnet run  --project src/ClaudeSwitcher/ClaudeSwitcher.csproj
```

Requires `Brinell.Samples.Shared` at the relative path in the `csproj`. If that library
moves, update the `ProjectReference` or publish `Brinell.Samples.Shared` (or a real
`Brinell.Mvvm`) as a NuGet package and switch to `<PackageReference>`.

---

## 8. Open questions

1. **Package name.** The request says `Brinell.Mvvm`. Do we (a) rename
   `Brinell.Samples.Shared` to `Brinell.Mvvm` and ship it, (b) leave the sample library
   in place and reference it, or (c) copy the three small files (`ParentViewModel`,
   `RelayCommand`, `AsyncRelayCommand`) directly into the ClaudeSwitcher project?
   The plan assumes (b) because it is the smallest step and the request said no
   external dependencies.
2. **Location on disk.** `tools/ClaudeSwitcher/` fits alongside `tools/Brinell.Scraper/`.
   Alternative: keep this out of the Brinell repo entirely because it has no Brinell
   dependency beyond MVVM. If it stays here, it should be excluded from the main
   `srcnew\Brinell.sln`.
3. **Profile of "current" detection.** Should the app also detect *which* stored profile
   currently matches `%USERPROFILE%\.claude.json` (e.g. by hashing) and mark it as
   active in the list? Trivial to add on top of `ProfileStore`.
4. **Backup.** Should activate first copy the existing `%USERPROFILE%\.claude*` into a
   `_backup` folder inside the target profile? Cheap safety, one extra step.

---

## 9. Implementation checklist

- [ ] Create `tools/ClaudeSwitcher/` folder and `.slnx`.
- [ ] Add the `csproj`, `App.xaml`(.cs), `MainWindow.xaml`(.cs), `app.manifest`.
- [ ] Add `Services/FileManager.cs` and `Services/ProfileStore.cs`.
- [ ] Add `Models/ProfileIndex.cs`.
- [ ] Add `ViewModels/MainViewModel.cs` and `ProfileViewModel.cs`.
- [ ] Replace the reflection hack in `ProfileViewModel.DisplayName` refresh with a
      direct `base.OnPropertyChanged` call (add a `protected` shim on `ParentViewModel`
      if needed, or `OnPropertyChanged` is already reachable — verify at implementation
      time).
- [ ] Verify build: `dotnet build`.
- [ ] Smoke test manually: log in with Claude Code, capture, log out, log in with a
      second account, capture, activate first, confirm `%USERPROFILE%\.claude.json` and
      `.claude\.credentials.json` change.
