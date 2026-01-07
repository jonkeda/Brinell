# ADR-005: Async Support Strategy

**Block:** 202 decision  
**Edition:** 🟡Ⅱ Core  
**Version:** 1.0  
**Created:** January 7, 2026

---

## decision ADR-005

- **title**: Platform-Determined Async Model
- **status**: accepted
- **date**: 2026-01-07
- **context**: Different automation drivers have different threading models; Playwright is async-native while Appium/Selenium are synchronous. Need a strategy that supports both without forcing unnecessary complexity.
- **decision**: Provide async interfaces parallel to sync interfaces; platform packages implement the model natural to their driver.
- **consequences**: Clean async support for async-native platforms, no deadlock risk, some interface duplication.

---

## 1. Context

The Brinell framework supports multiple automation drivers with different threading models:

| Platform | Driver | Threading Model |
|----------|--------|-----------------|
| MAUI | Appium | Synchronous |
| Blazor (Selenium) | Selenium WebDriver | Synchronous |
| Blazor (Playwright) | Playwright | **Async-native** |
| WPF | FlaUI / WinAppDriver | Synchronous |
| WinForms | FlaUI / WinAppDriver | Synchronous |
| Stride | Named Pipes | Synchronous |

### Problem

Playwright is designed async-first. Forcing sync wrappers causes:
- Performance degradation
- Potential deadlocks with `.Result` or `.Wait()`
- Loss of cancellation support
- Unnatural API usage

Conversely, forcing async on sync drivers adds unnecessary complexity and overhead for the majority of platforms.

### Requirements

From [FR-701 Async Support](../../100_requirements/120_functional/120_701_AsyncSupport.spx.md):
- Async method naming convention (`*Async`)
- Async interface hierarchy parallel to sync
- Platform-determined async model
- Cancellation token support
- No mixing of sync/async in same test

---

## 2. Decision

**Provide parallel async interfaces; each platform implements the model natural to its driver.**

### Async Interface Hierarchy

```
IAsyncControlObject                    # Base async control
├── IAsyncClickableControl             # Async click capability
├── IAsyncTextControl                  # Async text display
│   └── IAsyncEditableTextControl      # Async text input
├── IAsyncToggleControl                # Async toggle state
├── IAsyncSelectorControl              # Async selection
├── IAsyncRangeControl                 # Async numeric range
├── IAsyncContainerControl             # Async child scoping
└── IAsyncCollectionControl            # Async item enumeration
```

### Method Signatures

```csharp
// Sync interface (existing)
public interface IControlObject
{
    bool IsExists();
    bool WaitExists(bool exists = true, int? timeout = null);
    void AssertExists(string? message = null);
}

// Async interface (new)
public interface IAsyncControlObject
{
    Task<bool> IsExistsAsync(CancellationToken ct = default);
    Task<bool> WaitExistsAsync(bool exists = true, int? timeout = null, CancellationToken ct = default);
    Task AssertExistsAsync(string? message = null, CancellationToken ct = default);
}
```

### Platform Implementation

```
Platform Package: Brinell.Blazor.Playwright
  └── Implements: IAsyncControlObject, IAsyncClickableControl, etc.
  └── Uses: Playwright's async API natively

Platform Package: Brinell.MAUI
  └── Implements: IControlObject, IClickableControl, etc.
  └── Uses: Appium's sync API directly
```

---

## 3. Consequences

### Positive

| Benefit | Description |
|---------|-------------|
| **Natural APIs** | Each platform uses the API style natural to its driver |
| **No deadlocks** | No `.Result` or `.Wait()` required |
| **Cancellation support** | Async operations support cancellation tokens |
| **Performance** | No overhead from sync-over-async wrappers |
| **Type safety** | Async tests use async interfaces; compiler enforces consistency |

### Negative

| Trade-off | Mitigation |
|-----------|------------|
| **Interface duplication** | Code generation or T4 templates for async interfaces |
| **Learning curve** | Clear documentation on when to use which model |
| **Test portability** | Tests tied to sync OR async model; not portable between |

### Neutral

| Aspect | Notes |
|--------|-------|
| **Core package size** | Doubles interface count; still minimal |
| **Platform package choice** | Users choose package based on driver model |

---

## 4. Alternatives Considered

### Alternative 1: Async-Only

```csharp
// NOT CHOSEN - All methods async
public interface IControlObject
{
    Task<bool> IsExistsAsync();
    Task ClickAsync();
}
```

**Rejected because:**
- Forces async overhead on sync platforms (majority)
- Appium/Selenium tests become verbose
- Risk of deadlocks if users block on async
- Unnatural for sync-native drivers

### Alternative 2: Sync-Only with Async Helpers

```csharp
// NOT CHOSEN - Sync with Task.Run wrappers
public interface IControlObject
{
    bool IsExists();
}

// Helper extension
public static Task<bool> IsExistsAsync(this IControlObject c) 
    => Task.Run(() => c.IsExists());
```

**Rejected because:**
- Wasted thread pool threads
- Doesn't work with Playwright (truly async)
- No cancellation support
- Defeats purpose of async

### Alternative 3: Single Interface with ConfigureAwait

```csharp
// NOT CHOSEN - Sync methods that internally use async
public bool IsExists()
{
    return IsExistsInternalAsync().ConfigureAwait(false).GetAwaiter().GetResult();
}
```

**Rejected because:**
- Deadlock risk in certain contexts
- Hides the async nature from callers
- Complex internal implementation
- Performance overhead

---

## 5. Design Rules

### Rule 1: Async Interfaces Mirror Sync

Every sync interface has an async counterpart:

```csharp
// ✅ CORRECT - Matching structure
interface IClickableControl : IControlObject
{
    void Click();
}

interface IAsyncClickableControl : IAsyncControlObject
{
    Task ClickAsync(CancellationToken ct = default);
}
```

### Rule 2: Async Methods Have Async Suffix

```csharp
// ✅ CORRECT
Task ClickAsync();
Task<string> GetTextAsync();
Task WaitVisibleAsync();

// ❌ WRONG
Task Click();
Task<string> GetText();
```

### Rule 3: CancellationToken Is Optional Parameter

```csharp
// ✅ CORRECT - Optional with default
Task ClickAsync(CancellationToken ct = default);
Task<bool> WaitExistsAsync(int? timeout = null, CancellationToken ct = default);

// ❌ WRONG - Required parameter
Task ClickAsync(CancellationToken ct);
```

### Rule 4: No Mixing in Tests

```csharp
// ✅ CORRECT - All async
[Fact]
public async Task LoginTest()
{
    await _page.UsernameAsync.EnterAsync("user");
    await _page.PasswordAsync.EnterAsync("pass");
    await _page.LoginButtonAsync.ClickAsync();
}

// ✅ CORRECT - All sync
[Fact]
public void LoginTest()
{
    _page.Username.Enter("user");
    _page.Password.Enter("pass");
    _page.LoginButton.Click();
}

// ❌ WRONG - Mixed
[Fact]
public async Task LoginTest()
{
    _page.Username.Enter("user");  // Sync
    await _page.LoginButtonAsync.ClickAsync();  // Async
}
```

### Rule 5: Platform Package Determines Model

```xml
<!-- Sync platform - use sync interfaces -->
<PackageReference Include="Brinell.MAUI" />

<!-- Async platform - use async interfaces -->
<PackageReference Include="Brinell.Blazor.Playwright" />
```

---

## 6. Interface Definitions

### IAsyncControlObject

```csharp
public interface IAsyncControlObject
{
    // Identity
    string AutomationId { get; }
    IAsyncPageObject? Page { get; }
    
    // State (async)
    Task<bool> IsExistsAsync(CancellationToken ct = default);
    Task<bool> IsVisibleAsync(CancellationToken ct = default);
    Task<bool> IsEnabledAsync(CancellationToken ct = default);
    
    // Waiting (async)
    Task<bool> WaitExistsAsync(bool exists = true, int? timeout = null, CancellationToken ct = default);
    Task<bool> WaitVisibleAsync(bool visible = true, int? timeout = null, CancellationToken ct = default);
    Task<bool> WaitEnabledAsync(bool enabled = true, int? timeout = null, CancellationToken ct = default);
    
    // Assertions (async)
    Task AssertExistsAsync(string? message = null, CancellationToken ct = default);
    Task AssertNotExistsAsync(string? message = null, CancellationToken ct = default);
    Task AssertVisibleAsync(string? message = null, CancellationToken ct = default);
    Task AssertEnabledAsync(string? message = null, CancellationToken ct = default);
}
```

### IAsyncClickableControl

```csharp
public interface IAsyncClickableControl : IAsyncControlObject
{
    Task ClickAsync(CancellationToken ct = default);
    Task TapAsync(CancellationToken ct = default);
    Task DoubleClickAsync(CancellationToken ct = default);
    Task LongPressAsync(int durationMs = 1000, CancellationToken ct = default);
}
```

### IAsyncEditableTextControl

```csharp
public interface IAsyncTextControl : IAsyncControlObject
{
    Task<string> GetTextAsync(CancellationToken ct = default);
    Task AssertTextEqualsAsync(string expected, string? message = null, CancellationToken ct = default);
}

public interface IAsyncEditableTextControl : IAsyncTextControl
{
    Task EnterAsync(string text, CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);
    Task SetTextAsync(string text, CancellationToken ct = default);
}
```

---

## 7. Base Class Mapping

| Sync Base Class | Async Base Class |
|-----------------|------------------|
| ControlBase | AsyncControlBase |
| ClickableControlBase | AsyncClickableControlBase |
| TextControlBase | AsyncTextControlBase |
| EditableTextControlBase | AsyncEditableTextControlBase |
| ToggleControlBase | AsyncToggleControlBase |
| SelectorControlBase | AsyncSelectorControlBase |
| RangeControlBase | AsyncRangeControlBase |
| ContainerControlBase | AsyncContainerControlBase |
| CollectionControlBase | AsyncCollectionControlBase |

---

## 8. Platform Support Matrix

| Platform Package | Model | Interfaces Implemented |
|------------------|-------|----------------------|
| Brinell.MAUI | Sync | IControlObject, IClickableControl, etc. |
| Brinell.Blazor | Sync | IControlObject, IClickableControl, etc. |
| Brinell.Blazor.Playwright | **Async** | IAsyncControlObject, IAsyncClickableControl, etc. |
| Brinell.WPF | Sync | IControlObject, IClickableControl, etc. |
| Brinell.WinForms | Sync | IControlObject, IClickableControl, etc. |
| Brinell.Stride | Sync | IControlObject, IClickableControl, etc. |

---

## 9. Validation

This decision is validated when:

- [ ] Async interfaces defined in Core parallel to sync interfaces
- [ ] Brinell.Blazor.Playwright implements async interfaces
- [ ] Playwright tests use async/await naturally
- [ ] Sync platforms (MAUI, WPF) continue using sync interfaces
- [ ] No deadlocks in either model
- [ ] Cancellation tokens work in async tests

---

## Related Decisions

- [ADR-001: Clean Architecture](202_001_CleanArchitecture.spx.md)
- [ADR-002: Interface-First Design](202_002_InterfaceFirst.spx.md)
- [ADR-003: Platform Separation](202_003_PlatformSeparation.spx.md)
- [ADR-004: Control Interface Hierarchy](202_004_ControlHierarchy.spx.md)

---

## Related Documents

- [FR-701 Async Support](../../100_requirements/120_functional/120_701_AsyncSupport.spx.md) — Requirements
- [200_000_Overview.spx.md](../200_000_Overview.spx.md) — Architecture overview
