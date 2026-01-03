# PLAN-002: Core Update Plan

**Created:** January 3, 2026  
**Status:** Ready for Implementation  
**Depends On:** None (Core is first)  
**Blocks:** All platform update plans

---

## 1. Objectives

Update `Brinell.Core` to align with specifications v3.2, including:
- Add missing interfaces (`IContainerControl`, `IScrollableControl`)
- Remove obsolete adapter interfaces per AD-002
- Add platform extension methods
- Add configuration classes
- Add missing exception types

---

## 2. Pre-Implementation Checklist

- [ ] Build solution to verify current state
- [ ] Run existing Core tests (if any)
- [ ] Review interface dependencies in platform projects

---

## 3. Implementation Tasks

### Phase 1: Add Missing Interfaces

#### Task 1.1: Add IContainerControl Interface
**Priority:** High  
**Spec Reference:** FR-002.5, FR-002.6

```csharp
// File: src/Brinell.Core/Abstractions/Controls/IContainerControl.cs
namespace Brinell.Core.Abstractions.Controls;

/// <summary>
/// Interface for controls that can contain child elements.
/// Used for scoped element searching within a container.
/// </summary>
public interface IContainerControl : IControlObject
{
    /// <summary>
    /// Find a child control by automation ID within this container.
    /// </summary>
    TControl FindControl<TControl>(string automationId) where TControl : IControlObject;
    
    /// <summary>
    /// Check if a child control exists within this container.
    /// </summary>
    bool ContainsControl(string automationId);
}
```

- [ ] Create `IContainerControl.cs`
- [ ] Add XML documentation
- [ ] Verify no circular dependencies

#### Task 1.2: Add IScrollableControl Interface
**Priority:** High  
**Spec Reference:** FR-002.7

```csharp
// File: src/Brinell.Core/Abstractions/Controls/IScrollableControl.cs
namespace Brinell.Core.Abstractions.Controls;

/// <summary>
/// Interface for controls that support scrolling.
/// </summary>
public interface IScrollableControl : IControlObject
{
    /// <summary>
    /// Scroll until the element with the specified automation ID is visible.
    /// </summary>
    void ScrollToElement(string automationId);
    
    /// <summary>
    /// Scroll to the top of the content.
    /// </summary>
    void ScrollToTop();
    
    /// <summary>
    /// Scroll to the bottom of the content.
    /// </summary>
    void ScrollToBottom();
    
    /// <summary>
    /// Scroll up by the specified distance (platform-specific units).
    /// </summary>
    void ScrollUp(int distance = 100);
    
    /// <summary>
    /// Scroll down by the specified distance (platform-specific units).
    /// </summary>
    void ScrollDown(int distance = 100);
}
```

- [ ] Create `IScrollableControl.cs`
- [ ] Add XML documentation

#### Task 1.3: Add IScrollableControlAsync Interface
**Priority:** Medium  
**Spec Reference:** FR-002.7, AD-009 v3.2

```csharp
// File: src/Brinell.Core/Abstractions/Controls/IScrollableControlAsync.cs
namespace Brinell.Core.Abstractions.Controls;

/// <summary>
/// Async interface for controls that support scrolling.
/// For platforms with async-native drivers (Playwright).
/// </summary>
public interface IScrollableControlAsync : IControlObjectAsync
{
    ValueTask ScrollToElementAsync(string automationId, CancellationToken ct = default);
    ValueTask ScrollToTopAsync(CancellationToken ct = default);
    ValueTask ScrollToBottomAsync(CancellationToken ct = default);
    ValueTask ScrollUpAsync(int distance = 100, CancellationToken ct = default);
    ValueTask ScrollDownAsync(int distance = 100, CancellationToken ct = default);
}
```

- [ ] Create `IScrollableControlAsync.cs`
- [ ] Add XML documentation

---

### Phase 2: Remove Obsolete Interfaces

#### Task 2.1: Remove IDriverAdapter
**Priority:** High  
**Spec Reference:** AD-002

- [ ] Delete `src/Brinell.Core/Abstractions/IDriverAdapter.cs`
- [ ] Search for usages in platform projects
- [ ] Update platform projects to not implement this interface

#### Task 2.2: Remove IElementAdapter
**Priority:** High  
**Spec Reference:** AD-002

- [ ] Delete `src/Brinell.Core/Abstractions/IElementAdapter.cs`
- [ ] Search for usages in platform projects
- [ ] Update platform projects to not implement this interface

---

### Phase 3: Add Platform Extension Methods

#### Task 3.1: Add PlatformExtensions Class
**Priority:** Medium  
**Spec Reference:** FR-001.2

```csharp
// File: src/Brinell.Core/Abstractions/PlatformExtensions.cs
namespace Brinell.Core.Abstractions;

/// <summary>
/// Extension methods for Platform enum.
/// </summary>
public static class PlatformExtensions
{
    /// <summary>
    /// Returns true if the platform is a mobile platform (Android, iOS).
    /// </summary>
    public static bool IsMobile(this Platform platform) =>
        platform is Platform.Android or Platform.iOS;
    
    /// <summary>
    /// Returns true if the platform is a desktop platform (Windows, WindowsMaui).
    /// </summary>
    public static bool IsDesktop(this Platform platform) =>
        platform is Platform.Windows or Platform.WindowsMaui;
    
    /// <summary>
    /// Returns true if the platform is a web platform.
    /// </summary>
    public static bool IsWeb(this Platform platform) =>
        platform == Platform.Web;
    
    /// <summary>
    /// Returns true if the platform is a game engine platform.
    /// </summary>
    public static bool IsGameEngine(this Platform platform) =>
        platform == Platform.Stride;
    
    /// <summary>
    /// Returns true if the platform supports touch gestures.
    /// </summary>
    public static bool SupportsTouch(this Platform platform) =>
        platform.IsMobile();
    
    /// <summary>
    /// Returns true if the platform uses FlaUI for automation.
    /// </summary>
    public static bool UsesFlaUI(this Platform platform) =>
        platform == Platform.Windows;
    
    /// <summary>
    /// Returns true if the platform uses Appium for automation.
    /// </summary>
    public static bool UsesAppium(this Platform platform) =>
        platform is Platform.WindowsMaui or Platform.Android or Platform.iOS;
}
```

- [ ] Create `PlatformExtensions.cs`
- [ ] Add XML documentation
- [ ] Add unit tests

---

### Phase 4: Add Configuration Classes

#### Task 4.1: Add UITestConfiguration Class
**Priority:** Medium  
**Spec Reference:** SPEC-001 Section 7

```csharp
// File: src/Brinell.Core/Configuration/UITestConfiguration.cs
namespace Brinell.Core.Configuration;

/// <summary>
/// Configuration for UI tests.
/// </summary>
public class UITestConfiguration
{
    /// <summary>
    /// Platform-specific configurations.
    /// </summary>
    public Dictionary<string, PlatformConfiguration> Platforms { get; set; } = new();
    
    /// <summary>
    /// Default timeout in milliseconds for Wait operations.
    /// </summary>
    public int DefaultTimeoutMs { get; set; } = 10000;
    
    /// <summary>
    /// Short timeout in milliseconds for quick checks.
    /// </summary>
    public int ShortTimeoutMs { get; set; } = 3000;
    
    /// <summary>
    /// Polling interval in milliseconds for Wait operations.
    /// </summary>
    public int PollingIntervalMs { get; set; } = 250;
    
    /// <summary>
    /// Path for log output.
    /// </summary>
    public string LogOutputPath { get; set; } = "logs";
    
    /// <summary>
    /// Path for screenshot output.
    /// </summary>
    public string ScreenshotPath { get; set; } = "screenshots";
}

/// <summary>
/// Platform-specific configuration.
/// </summary>
public class PlatformConfiguration
{
    /// <summary>
    /// Path to the application executable or URL.
    /// </summary>
    public string? ApplicationPath { get; set; }
    
    /// <summary>
    /// Base URL for web applications.
    /// </summary>
    public string? BaseUrl { get; set; }
    
    /// <summary>
    /// Browser type for web testing.
    /// </summary>
    public string? BrowserType { get; set; }
    
    /// <summary>
    /// Additional platform-specific settings.
    /// </summary>
    public Dictionary<string, string> Settings { get; set; } = new();
}
```

- [ ] Create `Configuration/` folder if not exists
- [ ] Create `UITestConfiguration.cs`
- [ ] Create `PlatformConfiguration.cs`
- [ ] Add XML documentation

---

### Phase 5: Add Missing Exception Types

#### Task 5.1: Add TimeoutException
**Priority:** Medium  
**Spec Reference:** FR-010.2

```csharp
// File: src/Brinell.Core/Exceptions/UITestTimeoutException.cs
namespace Brinell.Core.Exceptions;

/// <summary>
/// Exception thrown when a UI test operation times out.
/// </summary>
public class UITestTimeoutException : Exception
{
    /// <summary>
    /// The automation ID of the element that timed out.
    /// </summary>
    public string? AutomationId { get; }
    
    /// <summary>
    /// The timeout value in milliseconds.
    /// </summary>
    public int TimeoutMs { get; }
    
    /// <summary>
    /// The operation that timed out.
    /// </summary>
    public string? Operation { get; }
    
    public UITestTimeoutException(string message) : base(message) { }
    
    public UITestTimeoutException(string message, string automationId, int timeoutMs, string? operation = null)
        : base(message)
    {
        AutomationId = automationId;
        TimeoutMs = timeoutMs;
        Operation = operation;
    }
    
    public UITestTimeoutException(string message, Exception innerException)
        : base(message, innerException) { }
}
```

- [ ] Create `UITestTimeoutException.cs`
- [ ] Add XML documentation

#### Task 5.2: Add InvalidStateException
**Priority:** Low  
**Spec Reference:** FR-010.2

```csharp
// File: src/Brinell.Core/Exceptions/InvalidStateException.cs
namespace Brinell.Core.Exceptions;

/// <summary>
/// Exception thrown when a control is in an invalid state for the requested operation.
/// </summary>
public class InvalidStateException : Exception
{
    /// <summary>
    /// The automation ID of the element.
    /// </summary>
    public string? AutomationId { get; }
    
    /// <summary>
    /// The current state of the element.
    /// </summary>
    public string? CurrentState { get; }
    
    /// <summary>
    /// The expected state for the operation.
    /// </summary>
    public string? ExpectedState { get; }
    
    public InvalidStateException(string message) : base(message) { }
    
    public InvalidStateException(string message, string automationId, string currentState, string expectedState)
        : base(message)
    {
        AutomationId = automationId;
        CurrentState = currentState;
        ExpectedState = expectedState;
    }
}
```

- [ ] Create `InvalidStateException.cs`
- [ ] Add XML documentation

---

## 4. Verification Tasks

### Build Verification
- [ ] Build Brinell.Core
- [ ] Build all platform projects (check for breaking changes)
- [ ] Build sample projects

### Test Verification
- [ ] Run Core unit tests
- [ ] Run platform unit tests
- [ ] Run sample project tests

---

## 5. Post-Implementation

### Documentation Updates
- [ ] Update SPEC-001 if needed
- [ ] Update interface documentation in specs/

### Code Review Checklist
- [ ] All new files have XML documentation
- [ ] No circular dependencies
- [ ] No breaking changes to existing interfaces
- [ ] Code follows C# naming conventions

---

## 6. Files to Create

| File | Description |
|------|-------------|
| `src/Brinell.Core/Abstractions/Controls/IContainerControl.cs` | Container control interface |
| `src/Brinell.Core/Abstractions/Controls/IScrollableControl.cs` | Scrollable control interface |
| `src/Brinell.Core/Abstractions/Controls/IScrollableControlAsync.cs` | Async scrollable interface |
| `src/Brinell.Core/Abstractions/PlatformExtensions.cs` | Platform extension methods |
| `src/Brinell.Core/Configuration/UITestConfiguration.cs` | Configuration classes |
| `src/Brinell.Core/Exceptions/UITestTimeoutException.cs` | Timeout exception |
| `src/Brinell.Core/Exceptions/InvalidStateException.cs` | Invalid state exception |

## 7. Files to Keep (AD-002 Clarification)

| File | Reason |
|------|--------|
| `src/Brinell.Core/Abstractions/IDriverAdapter.cs` | AD-002 allows interfaces, just no shared base classes |
| `src/Brinell.Core/Abstractions/IElementAdapter.cs` | AD-002 allows interfaces, just no shared base classes |

---

## 8. Estimated Effort

| Phase | Tasks | Estimated Time |
|-------|-------|----------------|
| Phase 1: Add Interfaces | 3 | 15 minutes |
| Phase 2: Platform Extensions | 1 | 5 minutes |
| Phase 3: Configuration | 1 | 10 minutes |
| Phase 4: Exceptions | 2 | 10 minutes |
| Verification | 6 | 15 minutes |
| **Total** | **13** | **~55 minutes** |

---

## 9. Rollback Plan

If issues arise:
1. Revert Core changes via Git
2. Platform projects should still build against old Core
3. No database or external dependencies to rollback

---

*Next: [PLAN-003: MAUI Update Plan](PLAN-003-MAUI-Update.md)*
