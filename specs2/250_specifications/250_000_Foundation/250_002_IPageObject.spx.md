# 250.002 IPageObject Specification

**Block Type:** SPC (Specification)  
**ID:** 250.002  
**Title:** IPageObject Interface Specification  
**Status:** Draft  
**Version:** 1.0  
**Level:** 0 - Foundation

---

## 1. Overview

`IPageObject` represents a page, screen, or view in the application under test. Page objects organize controls by screen and provide page-level operations like verifying the page is loaded and taking screenshots.

### Interface Identity

- **Package:** `Brinell.Core`
- **Namespace:** `Brinell.Core.Interfaces`
- **Dependencies:** `Locator`, `IControlObject`
- **Implementors:** `PageObjectBase`, platform-specific page bases

---

## 2. Behavior

### 2.1 Identity Properties

```csharp
public interface IPageObject
{
    /// <summary>
    /// The name of this page for logging and identification.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// The default locator strategy for controls on this page.
    /// Controls using string constructors will use this strategy.
    /// </summary>
    LocatorStrategy DefaultLocatorStrategy { get; }
}
```

**Behavior:**
- `Name` is set at construction and never changes
- `Name` is used in logging and exception messages
- `DefaultLocatorStrategy` defaults to `AutomationId` but can be overridden

### 2.2 Page State Methods

```csharp
/// <summary>
/// Check if the page is currently loaded and ready.
/// </summary>
/// <param name="timeoutMs">Timeout to wait for elements. Null = use default.</param>
/// <returns>True if page is loaded, false otherwise.</returns>
bool IsLoaded(int? timeoutMs = null);

/// <summary>
/// Wait until page loaded state matches expected value.
/// </summary>
/// <param name="expected">Expected loaded state. Null = skip operation.</param>
/// <param name="timeoutMs">Timeout in milliseconds. Null = use default.</param>
/// <returns>True if condition met, false if timeout.</returns>
bool WaitLoaded(bool? expected, int? timeoutMs = null);

/// <summary>
/// Assert page loaded state matches expected value.
/// </summary>
/// <param name="expected">Expected loaded state. Null = skip operation.</param>
/// <param name="message">Custom failure message. Null = use default.</param>
/// <param name="timeoutMs">Timeout in milliseconds. Null = use default.</param>
/// <exception cref="AssertionException">Thrown if assertion fails.</exception>
void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null);
```

**Behavior:**
- `IsLoaded()` implementation is page-specific (typically checks key control existence)
- **Nullable Skip Pattern:** If `expected` is null, return true/void immediately
- Page considers itself loaded when key identifying controls exist

### 2.3 Title Methods

```csharp
/// <summary>
/// Get the page title (for web) or screen title (for native).
/// </summary>
/// <param name="timeoutMs">Timeout to wait for title. Null = use default.</param>
/// <returns>Page title, or null if not available.</returns>
string? GetTitle(int? timeoutMs = null);

/// <summary>
/// Wait until page title matches expected value.
/// </summary>
/// <param name="expected">Expected title. Null = skip operation.</param>
/// <param name="timeoutMs">Timeout in milliseconds. Null = use default.</param>
/// <returns>True if condition met, false if timeout.</returns>
bool WaitTitle(string? expected, int? timeoutMs = null);

/// <summary>
/// Assert page title matches expected value.
/// </summary>
/// <param name="expected">Expected title. Null = skip operation.</param>
/// <param name="message">Custom failure message. Null = use default.</param>
/// <param name="timeoutMs">Timeout in milliseconds. Null = use default.</param>
void AssertTitle(string? expected, string? message = null, int? timeoutMs = null);
```

**Behavior:**
- For Blazor: Returns `document.title`
- For MAUI/WPF: Returns navigation title or window title
- Returns empty string (not null) if title not available

### 2.4 Page Operations

```csharp
/// <summary>
/// Take a screenshot of the current page.
/// </summary>
/// <param name="filename">Filename for screenshot. Null = auto-generate.</param>
/// <param name="timeoutMs">Timeout to wait for page ready. Null = use default.</param>
void TakeScreenshot(string? filename = null, int? timeoutMs = null);
```

**Behavior:**
- `TakeScreenshot()` saves to configured screenshot directory
- Auto-generated filename includes page name and timestamp

---

## 3. Boundary

### 3.1 Page Not Loaded

| Scenario | Behavior |
|----------|----------|
| `IsLoaded()` when page not loaded | Returns false |
| `WaitLoaded(true, ...)` when page never loads | Returns false after timeout |
| `AssertLoaded(true, ...)` when page not loaded | Throws AssertionException |
| `GetTitle()` when page not loaded | Returns empty string or throws (platform-specific) |

### 3.2 Screenshot Failures

| Scenario | Behavior |
|----------|----------|
| Invalid filename characters | Sanitizes filename |
| Directory doesn't exist | Creates directory |
| Disk full or permission denied | Throws IOException |

---

## 4. Acceptance Criteria

### ACC-001: Page Loaded Detection

```gherkin
Given a LoginPage with UsernameField as key control
When the page is navigated to and UsernameField exists
Then IsLoaded() returns true

Given a LoginPage
When navigating to a different page
Then IsLoaded() returns false
```

### ACC-002: Wait for Page Loaded

```gherkin
Given a slow-loading page that takes 2 seconds
And a timeout of 5 seconds
When WaitLoaded(true, 5000) is called
Then it returns true after approximately 2 seconds

Given a page that never loads
And a timeout of 1 second
When WaitLoaded(true, 1000) is called
Then it returns false after approximately 1 second
```

### ACC-003: Page Title Verification

```gherkin
Given a Blazor page with title "Login - MyApp"
When GetTitle() is called
Then it returns "Login - MyApp"

Given a MAUI page with navigation title "Login"
When GetTitle() is called
Then it returns "Login"
```

### ACC-004: Screenshot Capture

```gherkin
Given a visible page
When TakeScreenshot("login_test") is called
Then a file "login_test.png" is created in the screenshot directory

Given a visible page
When TakeScreenshot(null) is called
Then a file with auto-generated name is created
And the filename contains the page name
And the filename contains a timestamp
```

---

## 5. Assumptions

- **ASM-001:** Test context is initialized with screenshot directory configuration
- **ASM-002:** Page load detection relies on key control existence
- **ASM-003:** Title retrieval is platform-dependent
- **ASM-004:** Scroll functionality depends on platform scroll support
- **ASM-005:** Controls on the page are created via `new` pattern in constructor

---

## 6. Exclusions

- **EXC-001:** Navigation between pages — handled by test context or test code
- **EXC-002:** Page caching or singleton pattern — pages are created fresh
- **EXC-003:** Automatic page detection — tests create page objects explicitly
- **EXC-004:** Page-level input (keyboard shortcuts) — use control-level input
- **EXC-005:** URL manipulation for web — handled by test context

---

## 7. Complete Interface Definition

```csharp
namespace Brinell.Core.Interfaces
{
    /// <summary>
    /// Represents a page, screen, or view in the application under test.
    /// Page objects organize controls and provide page-level operations.
    /// </summary>
    public interface IPageObject
    {
        // Identity
        string Name { get; }
        LocatorStrategy DefaultLocatorStrategy { get; }
        
        // Page state
        bool IsLoaded(int? timeoutMs = null);
        bool WaitLoaded(bool? expected, int? timeoutMs = null);
        void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null);
        
        // Title
        string? GetTitle(int? timeoutMs = null);
        bool WaitTitle(string? expected, int? timeoutMs = null);
        void AssertTitle(string? expected, string? message = null, int? timeoutMs = null);
        
        // Page operations
        void TakeScreenshot(string? filename = null, int? timeoutMs = null);
    }
}
```

---

## 8. PageObjectBase Implementation Pattern

Concrete page objects extend a base class that implements `IPageObject`:

```csharp
public abstract class PageObjectBase : IPageObject
{
    protected readonly ITestContext _context;
    
    protected PageObjectBase(ITestContext context, string name)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
    
    public string Name { get; }
    
    public virtual LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;
    
    // Default implementation checks if page-specific key control exists
    public virtual bool IsLoaded(int? timeoutMs = null)
    {
        // Derived classes override to check their key control
        return true;
    }
    
    public bool WaitLoaded(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? _context.Timeouts.PageLoad;
        return WaitHelper.WaitFor(() => IsLoaded() == expected.Value, timeout);
    }
    
    public void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        WaitLoaded(expected, timeoutMs);
        if (IsLoaded() != expected.Value)
            throw new AssertionException(
                message ?? $"Page '{Name}' loaded={IsLoaded()}, expected={expected.Value}");
    }
    
    // ... other methods
}
```

---

## Related Documents

- [IControlObject Specification](250_001_IControlObject.spx.md)
- [Page Object Pattern](../../200_architecture/231_Patterns/231_002_PageObjectPattern.spx.md)
- [PageContext Module](../../200_architecture/211_Modules/211_004_PageContext.spx.md)
- [TestContext Specification](250_004_TestContext.spx.md)
