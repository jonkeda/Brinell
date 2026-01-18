# Fix 023: Extract AppiumFixtureBase to Brinell.Maui

| Field | Value |
|-------|-------|
| Status | Open |
| Date Created | 2026-01-18 |
| Date Resolved | _Pending_ |
| Affected Version | 0.1.0 |
| Fixed Version | _Pending_ |

## Summary

The current `AppiumFixture` class in `testsnew/Brinell.Maui.UITests` contains reusable infrastructure code (platform configuration, environment variables, screenshot service setup) that should be extracted to a base class in `srcnew/Brinell.Maui`. This will allow other test projects to reuse the Appium setup logic without code duplication.

## Symptoms

1. Test projects must duplicate AppiumFixture code for Appium setup
2. Platform configuration logic (Windows, Android, iOS) is embedded in test project
3. Environment variable handling is not reusable
4. Screenshot service setup must be duplicated in each test project
5. Adding a new platform requires changes in multiple places

## Evidence

### Current State

The `AppiumFixture` in `testsnew/Brinell.Maui.UITests/AppiumFixture.cs` contains:

```csharp
// Reusable infrastructure (should be in Brinell.Maui):
- CreateTestContextOptions()
- ConfigureWindowsOptions()
- ConfigureAndroidOptions()
- ConfigureiOSOptions()
- GetDefaultAppPath()
- FindSolutionDirectory()
- GetScreenshotDirectory()
- Screenshot service setup

// Test-specific (should stay in test project):
- MainPage property
- ContainerDemoPage property
- AppShellPage property
- NavigateToContainerDemo()
```

### Steps to Reproduce

1. Create a new MAUI UI test project
2. Need to copy entire AppiumFixture code
3. Modify only the test-specific pages
4. Repeat for each new test project

## Root Cause

The `AppiumFixture` class was created directly in the test project without separating reusable infrastructure from test-specific concerns. This is a design issue, not a bug.

### Affected Components

- `testsnew/Brinell.Maui.UITests/AppiumFixture.cs` - Current monolithic fixture
- `srcnew/Brinell.Maui/Testing/` - Target location for base class

## Proposed Solution

### Approach

Extract a base class `MauiTestFixtureBase` that handles:
1. Appium driver lifecycle (MauiTestContext creation/disposal)
2. Platform configuration (Windows, Android, iOS)
3. Environment variable handling
4. Screenshot service setup
5. Solution directory discovery

Test projects will inherit from this base class and add only their specific pages.

### Class Design

```csharp
// In srcnew/Brinell.Maui/Testing/MauiTestFixtureBase.cs
public abstract class MauiTestFixtureBase : IDisposable
{
    protected MauiTestContext Context { get; }
    protected IScreenshotService ScreenshotService { get; }
    
    // Platform configuration methods (protected virtual for override)
    protected virtual MauiTestContextOptions CreateTestContextOptions() { ... }
    protected virtual void ConfigureWindowsOptions(AppiumOptions options, string appPath) { ... }
    protected virtual void ConfigureAndroidOptions(AppiumOptions options, string appPath) { ... }
    protected virtual void ConfigureiOSOptions(AppiumOptions options, string appPath) { ... }
    
    // Abstract method for app path (required override)
    protected abstract string GetDefaultAppPath(string platform);
    
    // Utility methods
    protected static string FindSolutionDirectory() { ... }
    protected static string GetScreenshotDirectory() { ... }
}

// In testsnew/Brinell.Maui.UITests/AppiumFixture.cs
public class AppiumFixture : MauiTestFixtureBase
{
    public MainPage MainPage { get; }
    public ContainerDemoPage ContainerDemoPage { get; }
    public AppShellPage AppShell { get; }
    
    protected override string GetDefaultAppPath(string platform)
    {
        // Return sample app path
    }
}
```

### Affected Files

Files that will need modification:

| File | Expected Change |
|------|-----------------|
| `srcnew/Brinell.Maui/Testing/MauiTestFixtureBase.cs` | **Create** - New base class with reusable infrastructure |
| `testsnew/Brinell.Maui.UITests/AppiumFixture.cs` | **Modify** - Inherit from MauiTestFixtureBase, remove duplicated code |
| `srcnew/Brinell.Maui/Testing/Placeholder.cs` | **Delete** - No longer needed |

## Files Modified

_To be completed during implementation (Phase 2)_

| File | Change |
|------|--------|
| | |

## Verification

_To be completed during implementation (Phase 2)_

- [ ] Original symptoms resolved
- [ ] No new issues introduced
- [ ] Tests pass
- [ ] AppiumFixture is simplified
- [ ] MauiTestFixtureBase is reusable

## Related

- [Fix 022: Add FlyoutItem Control](./022-add-flyoutitem-control.fix.spx.md) - Uses AppiumFixture

## Notes

- The `GetDefaultAppPath` method is made abstract because it's app-specific (different sample apps have different paths)
- Platform configuration methods are virtual to allow override for custom capabilities
- Environment variable handling remains in base class as it's consistent across all projects
