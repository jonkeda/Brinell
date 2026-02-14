# 220.003 Playwright

**Block Type:** EXT (External Dependency)  
**ID:** 220.003  
**Title:** Playwright Integration  
**Status:** Draft  
**Version:** 1.0

---

## 1. Overview

Playwright is an alternative automation driver that can be used by `Brinell.Blazor` for testing Blazor web applications. It offers modern browser automation with auto-waiting and improved reliability.

> **Note:** Playwright support is planned for future releases. Code snippets in this document are illustrative examples.

### Integration Identity

- **Package:** `Brinell.Blazor.Playwright` (future)
- **NuGet Dependency:** `Microsoft.Playwright`
- **Minimum Version:** 1.40.0
- **Namespace:** `Microsoft.Playwright`

---

## 2. Purpose

Playwright provides:

1. **Modern Browser Automation** — Chromium, Firefox, WebKit
2. **Auto-Waiting** — Automatically waits for elements to be actionable
3. **Network Interception** — Mock APIs and control network requests
4. **Tracing** — Record test execution for debugging
5. **Cross-Platform** — Windows, macOS, Linux

---

## 3. Key Differences from Selenium

| Feature | Selenium | Playwright |
|---------|----------|------------|
| Waiting | Manual explicit waits | Auto-wait built-in |
| Browser install | External driver required | Downloads browsers automatically |
| Network mocking | Limited | Full request interception |
| Tracing | Manual screenshots | Video, trace, HAR recording |
| API style | Synchronous by default | Async-first design |
| Shadow DOM | Complex selectors | Native support |

---

## 4. Key Types

### 4.1 Core Types

```csharp
// Core Playwright types
IPlaywright                // Entry point
IBrowser                   // Browser instance
IBrowserContext            // Isolated browser session
IPage                      // Tab/page instance
ILocator                   // Element locator (auto-waiting)
```

### 4.2 Locator Strategies

```csharp
// Playwright locator methods
page.Locator("css=button")              // CSS selector
page.Locator("xpath=//button")          // XPath
page.Locator("text=Submit")             // Text content
page.Locator("[data-testid='submit']")  // Data attribute
page.GetByRole(AriaRole.Button)         // ARIA role
page.GetByTestId("submit")              // Test ID attribute
page.GetByText("Submit")                // Visible text
page.GetByLabel("Username")             // Label association
page.GetByPlaceholder("Enter name")     // Placeholder text
```

---

## 5. Potential Brinell Integration

### 5.1 IPlaywrightTestContext (Future)

```csharp
public interface IPlaywrightTestContext : ITestContext
{
    // Playwright-specific access
    IPage Page { get; }
    IBrowserContext BrowserContext { get; }
    IBrowser Browser { get; }
    
    // Element finding
    ILocator FindElement(Locator locator);
    IReadOnlyList<ILocator> FindElements(Locator locator);
}
```

### 5.2 Locator Mapping (Proposed)

| Brinell Locator | Playwright Locator |
|-----------------|-------------------|
| `LocatorStrategy.AutomationId` | `page.GetByTestId()` |
| `LocatorStrategy.Id` | `page.Locator("#id")` |
| `LocatorStrategy.XPath` | `page.Locator("xpath=...")` |
| `LocatorStrategy.CssSelector` | `page.Locator("css=...")` |
| `LocatorStrategy.Text` | `page.GetByText()` |
| `LocatorStrategy.Role` | `page.GetByRole()` |

---

## 6. Usage Examples

### 6.1 Browser Setup

```csharp
// Install browsers (one-time)
// dotnet tool install Microsoft.Playwright.CLI
// playwright install

// Create Playwright instance
using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new()
{
    Headless = true
});
var context = await browser.NewContextAsync();
var page = await context.NewPageAsync();
```

### 6.2 Navigation

```csharp
await page.GotoAsync("https://example.com");
await page.GoBackAsync();
await page.GoForwardAsync();
await page.ReloadAsync();
```

### 6.3 Element Interaction

```csharp
// Click - auto-waits for element to be visible and enabled
await page.Locator("[data-testid='submit']").ClickAsync();

// Fill text - clears and types
await page.Locator("[data-testid='username']").FillAsync("testuser");

// Get text
var text = await page.Locator(".welcome-message").TextContentAsync();

// Check visibility
var isVisible = await page.Locator(".modal").IsVisibleAsync();
```

### 6.4 Waiting

```csharp
// Playwright auto-waits, but explicit waits available
await page.WaitForSelectorAsync("[data-testid='loaded']");
await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

// Wait for condition
await page.Locator(".spinner").WaitForAsync(new() { State = WaitForSelectorState.Hidden });
```

### 6.5 Network Interception

```csharp
// Mock API response
await page.RouteAsync("**/api/users", route =>
{
    route.FulfillAsync(new()
    {
        Status = 200,
        ContentType = "application/json",
        Body = "[{\"id\": 1, \"name\": \"Test User\"}]"
    });
});

// Abort requests
await page.RouteAsync("**/*.{png,jpg}", route => route.AbortAsync());
```

### 6.6 Tracing

```csharp
// Start tracing
await context.Tracing.StartAsync(new()
{
    Screenshots = true,
    Snapshots = true,
    Sources = true
});

// ... run tests ...

// Stop and save trace
await context.Tracing.StopAsync(new()
{
    Path = "trace.zip"
});

// View trace: npx playwright show-trace trace.zip
```

---

## 7. Blazor Considerations

### 7.1 Test ID Configuration

Configure Playwright's test ID attribute to match Blazor conventions:

```csharp
// Use data-automation-id instead of data-testid
playwright.Selectors.SetTestIdAttribute("data-automation-id");

// Now GetByTestId uses data-automation-id
await page.GetByTestId("username").FillAsync("test");
```

### 7.2 Blazor Server SignalR

```csharp
// Wait for SignalR connection
await page.WaitForFunctionAsync("() => window.Blazor && window.Blazor._internal");
```

---

## 8. Comparison: When to Use

### Use Selenium When:
- Existing Selenium infrastructure
- Need Safari support on Windows
- Team familiarity with Selenium
- Specific browser driver requirements

### Use Playwright When:
- Starting new project
- Need better auto-waiting
- Want built-in tracing/debugging
- Need network mocking
- Modern async API preferred

---

## 9. Implementation Status

| Feature | Status |
|---------|--------|
| Core integration | 🔮 Planned |
| IPlaywrightTestContext | 🔮 Planned |
| Control implementations | 🔮 Planned |
| Network mocking helpers | 🔮 Planned |
| Tracing integration | 🔮 Planned |

---

## Related Documents

- [220.002 Selenium](220_002_Selenium.spx.md) — Primary browser automation
- [211.004 Page/Context Module](../211_Modules/211_004_PageContext.spx.md) — Context implementations
