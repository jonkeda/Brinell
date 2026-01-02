# 9. Page Object Pattern

**Parent:** [Documentation Index](21d0_UITestFramework_Index.md)  
**Code Examples:** [21d9_PageObjectPattern_CodeExamples.md](21d9_PageObjectPattern_CodeExamples.md)  
**Previous:** [IsBusy-Based State Tracking](21d8_IsBusyStateTracking.md)  
**Version:** 3.0 (Updated December 2025)

---

## 9.1 Overview

The Page Object pattern encapsulates page structure and behavior, providing a clean API for test interaction.

**Key Architecture (v3):**
- Core defines `IPageObject` interface only
- Each platform implements its own `PageBase` class
- Navigation methods return **void** (not target page)
- Tests create and manage page object instances directly

---

## 9.2 IPageObject Interface (Core)

```csharp
public interface IPageObject
{
    ITestContext Context { get; }
    string PageName { get; }
    bool IsDisplayed();
    void WaitForDisplayed(TimeSpan? timeout = null);
    void CheckDisplayed(TimeSpan? timeout = null);
    void AssertDisplayed(TimeSpan? timeout = null);
}
```

---

## 9.3 Platform-Specific PageBase Classes

Each platform implements its own page base class hierarchy:

### 9.3.1 WPF PageBase (FlaUI)

| Class | Purpose |
|-------|---------|
| `PageBase` | Base implementation of `IPageObject` |
| `BusyPageBase` | Adds IsBusy tracking with BusyIndicatorId |

### 9.3.2 MAUI PageBase (Appium)

| Class | Purpose |
|-------|---------|
| `PageBase` | Base implementation of `IPageObject` |
| `LoadingPageBase` | Adds loading indicator tracking |

### 9.3.3 HTML PageBase (Selenium)

| Class | Purpose |
|-------|---------|
| `PageBase` | Base implementation of `IPageObject` |
| `LoadingPageBase` | Adds AJAX loading tracking |

---

## 9.4 Page Object Guidelines

### 9.4.1 Structure

1. **One page object per view/page**
2. **Declare all controls as properties**
3. **Implement `IsDisplayed()` using a key control**
4. **Navigation methods return void**
5. **Use descriptive method names for actions**

### 9.4.2 Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Class | `{Page}Page` | `SettingsPage` |
| Controls | `{Name}{Type}` | `SaveButton`, `UsernameInput` |
| Navigation | `NavigateTo{Page}()` | `NavigateToSettings()` |
| Actions | Verb phrases | `SaveSettings()`, `Login()` |

---

## 9.5 Control Declaration

### 9.5.1 Pattern

```csharp
public class SettingsPage : BusyPageBase
{
    // Declare controls as public properties
    public ButtonControl SaveButton { get; }
    public TextBoxControl UsernameInput { get; }
    
    public SettingsPage(FlaUITestContext context) : base(context, "Settings")
    {
        // Initialize in constructor with native driver access
        SaveButton = new ButtonControl(context, this, "SaveButton");
        UsernameInput = new TextBoxControl(context, this, "UsernameInput");
    }
}
```

### 9.5.2 Control Parameters

| Parameter | Purpose |
|-----------|---------|
| `context` | Platform-specific test context |
| `this` | Parent page for logging |
| `automationId` | Element identifier |

---

## 9.6 Navigation Methods

### 9.6.1 Pattern (v3 - Returns Void)

```csharp
// In page object
public void NavigateToSettings()
{
    // 1. Log navigation
    Log("Navigating to Settings");
    
    // 2. Perform navigation action
    SettingsButton.Click();
}

// In test
[Test]
public void Test_Settings_Change()
{
    var home = new HomePage(Context);
    home.WaitForPageReady();
    
    // Navigation returns void
    home.NavigateToSettings();
    
    // Test creates target page object
    var settings = new SettingsPage(Context);
    settings.WaitForPageReady();
    
    settings.UsernameInput.EnterText("newname");
}
```

### 9.6.2 Why Navigation Returns Void

**Previous (v1-v2):** Navigation returned target page object
```csharp
var settings = home.NavigateToSettings(); // Returned SettingsPage
```

**Current (v3):** Navigation returns void, test creates target page
```csharp
home.NavigateToSettings();                // Returns void
var settings = new SettingsPage(Context); // Test creates page
settings.WaitForPageReady();
```

**Benefits:**
- Clearer test ownership of page object lifecycle
- No hidden page creation inside navigation methods
- Better control over wait strategies in tests
- Simpler page object implementation
```

---

## 9.7 Action Methods

### 9.7.1 Simple Actions

```csharp
public void Login(string username, string password)
{
    Log($"Logging in as {username}");
    
    UsernameInput.EnterText(username);
    PasswordInput.EnterText(password);
    LoginButton.Click();
}
```

### 9.7.2 Actions with State Changes

```csharp
public void SaveAndWait()
{
    Log("Saving settings");
    
    SaveButton.Click();
    
    // Wait for save to complete
    WaitForNotBusy();
    
    Log("Settings saved");
}
```

### 9.7.3 Actions with Return Values

```csharp
public string GetWelcomeMessage()
{
    return WelcomeLabel.GetText();
}

public bool IsLoggedIn()
{
    return UserProfileButton.IsVisible();
}
```

---

## 9.8 Page Hierarchy (Platform-Specific)

### 9.8.1 WPF Base Classes

```
PageBase : IPageObject (abstract)
│   - Context (FlaUITestContext)
│   - PageName, Logger
│   - IsDisplayed(), WaitForDisplayed()
│
└── BusyPageBase (abstract)
        - BusyIndicatorId
        - IsBusy(), WaitForNotBusy()
        - WaitForPageReady()
```

### 9.8.2 When to Extend Which

| Base Class | Use When |
|------------|----------|
| `PageBase` | Simple pages without loading states |
| `BusyPageBase` / `LoadingPageBase` | Pages with async loading |

---

## 9.9 Dialog and Modal Handling

### 9.9.1 Modal Dialog Pattern

```csharp
public class ConfirmDialog : PageBase
{
    public LabelControl MessageLabel { get; }
    public ButtonControl ConfirmButton { get; }
    public ButtonControl CancelButton { get; }
    
    public ConfirmDialog(FlaUITestContext context) : base(context, "ConfirmDialog")
    {
        MessageLabel = new LabelControl(context, this, "DialogMessage");
        ConfirmButton = new ButtonControl(context, this, "ConfirmButton");
        CancelButton = new ButtonControl(context, this, "CancelButton");
    }
    
    public override bool IsDisplayed()
    {
        return MessageLabel.IsVisible();
    }
    
    public void Confirm()
    {
        ConfirmButton.Click();
        // Wait for dialog to close
        Context.WaitFor(() => !IsDisplayed());
    }
    
    public void Cancel()
    {
        CancelButton.Click();
        Context.WaitFor(() => !IsDisplayed());
    }
}
```

### 9.9.2 Using Dialogs in Tests

```csharp
[Test]
public void Test_Delete_With_Confirm()
{
    var settings = new SettingsPage(Context);
    settings.WaitForPageReady();
    
    settings.DeleteButton.Click();
    
    var confirmDialog = new ConfirmDialog(Context);
    confirmDialog.WaitForDisplayed();
    confirmDialog.Confirm();
}
```
```

---

## 9.10 Composite Pages

### 9.10.1 Pages with Regions

```csharp
public class DashboardPage : BusyPageBase
{
    // Regions as nested page objects
    public HeaderRegion Header { get; }
    public SidebarRegion Sidebar { get; }
    public ContentRegion Content { get; }
    
    public DashboardPage(FlaUITestContext context) : base(context, "Dashboard")
    {
        Header = new HeaderRegion(context);
        Sidebar = new SidebarRegion(context);
        Content = new ContentRegion(context);
    }
    
    public override bool IsDisplayed()
    {
        return Header.IsDisplayed() && Content.IsDisplayed();
    }
}
```

### 9.10.2 Region Base Class

```csharp
public abstract class RegionBase
{
    protected FlaUITestContext Context { get; }
    protected string RegionName { get; }
    
    protected RegionBase(FlaUITestContext context, string regionName)
    {
        Context = context;
        RegionName = regionName;
    }
    
    public abstract bool IsDisplayed();
}
```

---

## 9.11 Best Practices

### 9.11.1 DO

- ✅ Keep page objects focused on structure and navigation
- ✅ Use meaningful method names
- ✅ Navigation methods return void (test creates target page)
- ✅ Wait for page ready after navigation in tests
- ✅ Log significant actions
- ✅ Use platform-specific context type in constructor

### 9.11.2 DON'T

- ❌ Put assertions in page objects
- ❌ Expose raw automation IDs publicly
- ❌ Return page objects from navigation methods
- ❌ Skip IsBusy checks
- ❌ Use generic `ITestContext` - use platform-specific context

---

*Next: [WireMock API Mocking](21d10_WireMockApiMocking.md)*
