# Brinell MVVM Infrastructure Plan

## Status: ✅ IMPLEMENTED

## Overview

Copy MVVM infrastructure from `Oravey.Tools.Wpf` to `Brinell.Samples.Shared` for reuse across WPF and MAUI sample applications.

**Goal**: Create a shared MVVM library that both `Brinell.Samples.Wpf` and `Brinell.Samples.Maui` can reference.

## IsBusy Pattern Support

The implementation fully supports the IsBusy pattern:

- **`IViewVisible.IsBusy`** - Property to check if ViewModel is busy
- **`IViewVisible.BeginBusy()`** - Increment busy counter (thread-safe)
- **`IViewVisible.EndBusy()`** - Decrement busy counter (thread-safe)
- **`ViewModelBase`** - Implements IsBusy with thread-safe counter
- **`AsyncRelayCommand`** - Automatically calls BeginBusy/EndBusy unless `SkipBusyTracking` option set
- **`AsyncRelayCommandOptions.SkipBusyTracking`** - Option to skip IsBusy tracking for background operations

### Usage Example

```csharp
public class LoginViewModel : ViewModelBase
{
    public IAsyncRelayCommand LoginCommand { get; }
    
    public LoginViewModel()
    {
        // IsBusy automatically tracked during LoginAsync execution
        LoginCommand = new AsyncRelayCommand(this, LoginAsync);
    }
    
    private async Task LoginAsync()
    {
        // IsBusy = true (automatic)
        await _authService.LoginAsync(Username, Password);
        // IsBusy = false (automatic)
    }
}

// In UI test:
await page.LoginButton.Click();
await page.WaitForNotBusy(); // Wait for IsBusy = false
```

---

## Source Files to Copy

### From `Oravey.Tools.Wpf\Infrastructure\Commands\`

| Source File | Target File |
|-------------|-------------|
| `RelayCommand.cs` | `Brinell.Samples.Shared\Commands\RelayCommand.cs` |
| `AsyncRelayCommand.cs` | `Brinell.Samples.Shared\Commands\AsyncRelayCommand.cs` |
| `AsyncRelayCommandT.cs` | `Brinell.Samples.Shared\Commands\AsyncRelayCommandT.cs` |
| `AsyncRelayCommandOptions.cs` | `Brinell.Samples.Shared\Commands\AsyncRelayCommandOptions.cs` |
| `SingleClickAsyncRelayCommand.cs` | `Brinell.Samples.Shared\Commands\SingleClickAsyncRelayCommand.cs` |
| `Interfaces\IRelayCommand.cs` | `Brinell.Samples.Shared\Commands\IRelayCommand.cs` |
| `Interfaces\IRelayCommandT.cs` | `Brinell.Samples.Shared\Commands\IRelayCommandT.cs` |
| `Interfaces\IAsyncRelayCommand.cs` | `Brinell.Samples.Shared\Commands\IAsyncRelayCommand.cs` |
| `Interfaces\IAsyncRelayCommandT.cs` | `Brinell.Samples.Shared\Commands\IAsyncRelayCommandT.cs` |

### From `Oravey.Tools.Wpf\Features\Shell\ViewModels\`

| Source File | Target File |
|-------------|-------------|
| `ViewModelBase.cs` | `Brinell.Samples.Shared\ViewModels\ViewModelBase.cs` |
| `IViewVisible.cs` | `Brinell.Samples.Shared\ViewModels\IViewVisible.cs` |
| `ICurrentViewModelContainer.cs` | `Brinell.Samples.Shared\ViewModels\ICurrentViewModelContainer.cs` |

### From `Oravey.Tools.Wpf\Infrastructure\Navigation\`

| Source File | Target File |
|-------------|-------------|
| `INavigationService.cs` | `Brinell.Samples.Shared\Navigation\INavigationService.cs` |

---

## Namespace Renames

| Old Namespace | New Namespace |
|---------------|---------------|
| `Oravey.Tools.Wpf.Infrastructure.Commands` | `Brinell.Samples.Shared.Commands` |
| `Oravey.Tools.Wpf.Features.Shell.ViewModels` | `Brinell.Samples.Shared.ViewModels` |
| `Oravey.Tools.Wpf.Infrastructure.Navigation` | `Brinell.Samples.Shared.Navigation` |

---

## Target Structure

```
samples/
├── Brinell.Samples.Shared/
│   ├── Brinell.Samples.Shared.csproj
│   ├── Commands/
│   │   ├── IRelayCommand.cs
│   │   ├── IRelayCommandT.cs
│   │   ├── IAsyncRelayCommand.cs
│   │   ├── IAsyncRelayCommandT.cs
│   │   ├── RelayCommand.cs
│   │   ├── AsyncRelayCommand.cs
│   │   ├── AsyncRelayCommandT.cs
│   │   ├── AsyncRelayCommandOptions.cs
│   │   └── SingleClickAsyncRelayCommand.cs
│   ├── ViewModels/
│   │   ├── ViewModelBase.cs
│   │   ├── IViewVisible.cs
│   │   └── ICurrentViewModelContainer.cs
│   └── Navigation/
│       └── INavigationService.cs
│
├── Brinell.Samples.Wpf/              # WPF sample app (uses Shared)
│   └── ...
│
└── Brinell.Samples.Maui/             # MAUI sample app (uses Shared)
    └── ...
```

---

## Project File

### Brinell.Samples.Shared.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>Brinell.Samples.Shared</RootNamespace>
    <Description>Shared MVVM infrastructure for Brinell sample applications</Description>
  </PropertyGroup>

  <!-- No WPF/MAUI dependencies - pure .NET Standard-like -->

</Project>
```

**Note**: The shared project should NOT reference WPF or MAUI directly. Platform-specific implementations (like `CommandManager.InvalidateRequerySuggested()` in WPF) need abstraction.

---

## Platform Abstraction Required

### Issue: WPF CommandManager Dependency

`RelayCommand.cs` uses `CommandManager.RequerySuggested` which is WPF-specific.

**Solution**: Create interface and platform-specific implementations:

```csharp
// Brinell.Samples.Shared/Commands/ICommandManager.cs
public interface ICommandManager
{
    event EventHandler RequerySuggested;
    void InvalidateRequerySuggested();
}

// Set globally on app startup
public static class CommandManagerProvider
{
    public static ICommandManager? Current { get; set; }
}
```

Then in WPF app:
```csharp
CommandManagerProvider.Current = new WpfCommandManager();
```

In MAUI app:
```csharp
CommandManagerProvider.Current = new MauiCommandManager();
```

---

## Implementation Steps

### Step 1: Create Project (10 min)
- [ ] Create `samples/Brinell.Samples.Shared/` folder
- [ ] Create `Brinell.Samples.Shared.csproj`
- [ ] Add to `Brinell.sln`

### Step 2: Copy & Rename Commands (30 min)
- [ ] Copy all command files
- [ ] Replace namespace `Oravey.Tools.Wpf.Infrastructure.Commands` → `Brinell.Samples.Shared.Commands`
- [ ] Replace namespace `Oravey.Tools.Wpf.Features.Shell.ViewModels` → `Brinell.Samples.Shared.ViewModels`
- [ ] Abstract `CommandManager` to interface

### Step 3: Copy & Rename ViewModels (15 min)
- [ ] Copy `ViewModelBase.cs`, `IViewVisible.cs`, `ICurrentViewModelContainer.cs`
- [ ] Update namespaces

### Step 4: Copy Navigation Interface (5 min)
- [ ] Copy `INavigationService.cs`
- [ ] Update namespace

### Step 5: Build & Verify (10 min)
- [ ] Build shared project
- [ ] Ensure no WPF/MAUI dependencies leak in

**Total: ~70 minutes**

---

## Files Summary

| Count | Category |
|-------|----------|
| 9 | Command files (interfaces + implementations) |
| 3 | ViewModel files (base class + interfaces) |
| 1 | Navigation interface |
| 1 | Project file |
| **14** | **Total files** |

---

## Future Usage

### WPF Sample App
```csharp
// Brinell.Samples.Wpf references Brinell.Samples.Shared
using Brinell.Samples.Shared.Commands;
using Brinell.Samples.Shared.ViewModels;

public class LoginViewModel : ViewModelBase
{
    public IAsyncRelayCommand LoginCommand { get; }
    
    public LoginViewModel()
    {
        LoginCommand = new AsyncRelayCommand(this, LoginAsync);
    }
}
```

### MAUI Sample App
```csharp
// Brinell.Samples.Maui references Brinell.Samples.Shared
using Brinell.Samples.Shared.Commands;
using Brinell.Samples.Shared.ViewModels;

public class LoginViewModel : ViewModelBase
{
    public IAsyncRelayCommand LoginCommand { get; }
    
    public LoginViewModel()
    {
        LoginCommand = new AsyncRelayCommand(this, LoginAsync);
    }
}
```

---

## Converters (Deferred)

The converters in `Oravey.Tools.Wpf\Infrastructure\Converters\` are WPF-specific (`IValueConverter`). 

**Recommendation**: Do NOT copy converters to shared project. Instead:
- WPF converters stay in WPF sample project
- MAUI converters created separately in MAUI sample project

MAUI uses the same `IValueConverter` interface but from `Microsoft.Maui.Controls` namespace.

---

## Success Criteria

- [ ] `Brinell.Samples.Shared` builds successfully
- [ ] No WPF or MAUI package references in shared project
- [ ] Both WPF and MAUI sample apps can reference shared project
- [ ] ViewModelBase works identically on both platforms
- [ ] Commands work identically on both platforms
