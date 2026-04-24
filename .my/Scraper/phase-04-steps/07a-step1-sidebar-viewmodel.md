# Step 07a-1 — Redesign SidebarViewModel

## Objective

Replace the placeholder `SidebarViewModel` (empty `ObservableCollection<string>` for Pages/Controls) with a properly typed view model that supports corpus pages, session pages (for recording), and navigation.

## Current State

`SidebarViewModel` lives at the bottom of `BrowserViewModel.cs` (not its own file):

```csharp
public sealed class SidebarViewModel : ViewModelBase
{
    private string _corpusStats = "0 pages · 0 controls";

    public ObservableCollection<string> Pages { get; } = [];
    public ObservableCollection<string> Controls { get; } = [];

    public string CorpusStats
    {
        get => _corpusStats;
        set => SetProperty(ref _corpusStats, value);
    }
}
```

Registered in DI as `Transient`:
```csharp
services.AddTransient<SidebarViewModel>();
```

## Changes

### 1. Create `Models/SidebarPageItem.cs`

```csharp
namespace Brinell.Scraper.Models;

public sealed class SidebarPageItem
{
    public string Name { get; init; } = "";
    public string Url { get; init; } = "";
    public string StatusIcon { get; init; } = "";
}
```

| `StatusIcon` | Meaning |
|--------------|---------|
| `"✅"` | Page has generated code |
| `"⏳"` | Recorded but no code yet |
| `"🆕"` | New this session |
| `""` | In corpus but not recorded |

### 2. Move `SidebarViewModel` to its own file: `ViewModels/SidebarViewModel.cs`

```csharp
using System.Collections.ObjectModel;
using Brinell.Scraper.Models;

namespace Brinell.Scraper.ViewModels;

public sealed class SidebarViewModel : ViewModelBase
{
    private string _corpusStats = "0 pages · 0 controls";
    private string _siteHeader = "";
    private bool _isRecording;

    public ObservableCollection<SidebarPageItem> CorpusPages { get; } = [];
    public ObservableCollection<SidebarPageItem> SessionPages { get; } = [];
    public ObservableCollection<string> Controls { get; } = [];

    public string CorpusStats
    {
        get => _corpusStats;
        set => SetProperty(ref _corpusStats, value);
    }

    public string SiteHeader
    {
        get => _siteHeader;
        set => SetProperty(ref _siteHeader, value);
    }

    public bool IsRecording
    {
        get => _isRecording;
        set => SetProperty(ref _isRecording, value);
    }

    public void LoadCorpusPages(IEnumerable<SidebarPageItem> pages)
    {
        CorpusPages.Clear();
        foreach (var page in pages)
            CorpusPages.Add(page);
    }

    public void AddSessionPage(DomSnapshot snapshot)
    {
        SessionPages.Add(new SidebarPageItem
        {
            Name = snapshot.PageName,
            Url = snapshot.PageUrl,
            StatusIcon = "🆕"
        });
    }

    public void ClearSession()
    {
        SessionPages.Clear();
        IsRecording = false;
    }
}
```

### 3. Remove `SidebarViewModel` from `BrowserViewModel.cs`

Delete the class definition from the bottom of the file (lines ~136-150).

### 4. Fix DI registration in `App.xaml.cs`

Change from `Transient` to `Singleton` (shared state across the app lifetime):
```csharp
// Before:
services.AddTransient<SidebarViewModel>();
// After:
services.AddSingleton<SidebarViewModel>();
```

## Files Modified

| File | Action |
|------|--------|
| `Models/SidebarPageItem.cs` | **Create** |
| `ViewModels/SidebarViewModel.cs` | **Create** (new file) |
| `ViewModels/BrowserViewModel.cs` | **Edit** — remove `SidebarViewModel` class |
| `App.xaml.cs` | **Edit** — change `Transient` → `Singleton` |

## Verification

- Build succeeds
- All 101 existing tests pass
- `MainViewModel` still resolves `SidebarViewModel` from DI without error

## Checklist

- [ ] `SidebarPageItem` model created with Name, Url, StatusIcon
- [ ] `SidebarViewModel` extracted to own file with `CorpusPages`, `SessionPages`, `Controls`
- [ ] `SiteHeader` and `IsRecording` properties added
- [ ] `LoadCorpusPages`, `AddSessionPage`, `ClearSession` methods implemented
- [ ] Old `SidebarViewModel` removed from `BrowserViewModel.cs`
- [ ] DI registration changed to Singleton
- [ ] Build succeeds, tests pass
