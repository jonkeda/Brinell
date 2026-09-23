# ShellContent Control Object

## Overview

The `ShellContent<TScope>` control object represents individual navigation items within a MAUI Shell container (typically TabBar items in a Shell).

**Location:** `srcnew/Brinell.Maui.Extensions/Controls/Navigation/ShellContent.cs`

## Features

- **Navigation**: Click/navigate to specific shell content
- **State Checking**: Check if a shell content item is selected
- **Selection Polling**: Wait for selection state changes
- **Assertions**: Assert selection state with automatic wait
- **Flexible Locators**: Support by AutomationId or Route attribute

## Basic Usage

### Page Object Setup

```csharp
using Brinell.Maui.Extensions.Controls.Navigation;
using ShellContentControl = Brinell.Maui.Extensions.Controls.Navigation.ShellContent;

public class AppShellPage : PageObjectBase<AppShellPage>
{
    public AppShellPage(IMauiTestContext context) : base(context)
    {
        // Create by AutomationId (primary - recommended)
        ButtonsTab = new ShellContentControl<AppShellPage>(this, "ButtonsShell", "Buttons");

        // Or by Route (alternative)
        // ButtonsTab = new ShellContentControl<AppShellPage>(this, "ButtonsPage");
    }

    public ITabControlObject<AppShellPage> ButtonsTab { get; }
}
```

### Navigation

```csharp
// Click to navigate
page.ButtonsTab.ClickAndNavigate();

// Navigate and wait for selection
page.ButtonsTab.NavigateTo(timeoutMs: 5000);
```

### State Checking

```csharp
// Check if selected
bool? isSelected = page.ButtonsTab.IsSelected();
// Returns: true = selected, false = not selected, null = element not found

// Wait for selection
bool waitResult = page.ButtonsTab.WaitSelected(expected: true, timeoutMs: 5000);
// Returns: true if condition met, false if timeout
```

### Assertions

```csharp
// Assert selected
page.ButtonsTab.AssertIsSelected();

// Assert not selected
page.ButtonsTab.AssertIsNotSelected();

// Assert with timeout
page.ButtonsTab.AssertSelected(true, timeoutMs: 10000);

// Assert with custom message
page.ButtonsTab.AssertSelected(true, message: "Buttons tab should be active after navigation");
```

## API Reference

### Constructors

#### Constructor 1: By AutomationId (Recommended)
```csharp
public ShellContent(IMauiScope<TScope> scope, string automationId, string title)
```

- **scope**: The page/scope providing element finding
- **automationId**: AutomationId of the ShellContent element (e.g., "ButtonsShell")
- **title**: Title for display/assertions (e.g., "Buttons")

**Preferred for Windows** where AutomationId is most reliable.

```csharp
var buttonsTab = new ShellContent<MyPage>(page, "ButtonsShell", "Buttons");
```

#### Constructor 2: By Route
```csharp
public ShellContent(IMauiScope<TScope> scope, string route)
```

- **scope**: The page/scope providing element finding
- **route**: Route attribute of ShellContent (e.g., "ButtonsPage")

**Title defaults to route** if not provided.

```csharp
var buttonsTab = new ShellContent<MyPage>(page, "ButtonsPage");
```

### Properties

#### Route
```csharp
public string Route { get; }
```
Gets the route identifier of this ShellContent.

#### Title
```csharp
public string Title { get; }
```
Gets the title (from ITabControlObject). Used in assertions and messages.

### Methods

#### ClickAndNavigate()
```csharp
public TScope ClickAndNavigate()
```

Clicks the ShellContent to navigate to it. Returns scope for chaining.

```csharp
page.ButtonsTab.ClickAndNavigate()
    .AssertIsSelected();
```

#### NavigateTo(int? timeoutMs)
```csharp
public TScope NavigateTo(int? timeoutMs = null)
```

Navigates to this ShellContent and waits for selection.

```csharp
page.ButtonsTab.NavigateTo(timeoutMs: 5000);
```

#### IsSelected()
```csharp
public bool? IsSelected()
```

Checks current selection state without waiting.

```csharp
if (page.ButtonsTab.IsSelected() == true)
{
    // Buttons tab is selected
}
```

#### WaitSelected(bool? expected, int? timeoutMs)
```csharp
public bool WaitSelected(bool? expected, int? timeoutMs = null)
```

Polls for a specific selection state. Returns true if condition met within timeout.

```csharp
bool loaded = page.ButtonsTab.WaitSelected(expected: true, timeoutMs: 5000);
```

#### AssertSelected(bool? expected, string? message, int? timeoutMs)
```csharp
public TScope AssertSelected(bool? expected, string? message = null, int? timeoutMs = null)
```

Asserts selection state with automatic wait and optional custom message.

```csharp
page.ButtonsTab.AssertSelected(true, message: "Buttons should be selected");
```

#### AssertIsSelected(int? timeoutMs)
```csharp
public TScope AssertIsSelected(int? timeoutMs = null)
```

Convenience method to assert selected state.

```csharp
page.ButtonsTab.AssertIsSelected(timeoutMs: 5000);
```

#### AssertIsNotSelected(int? timeoutMs)
```csharp
public TScope AssertIsNotSelected(int? timeoutMs = null)
```

Convenience method to assert not selected state.

```csharp
page.ButtonsTab.AssertIsNotSelected();
```

## Example Test

```csharp
[Fact]
public void TestShellContentNavigation()
{
    var page = new AppShellPage(_context);

    // Navigate to Buttons
    page.ButtonsTab
        .NavigateTo()
        .AssertIsSelected()

        // Navigate to DateTime
        .DateTimeTab
        .NavigateTo()
        .AssertIsSelected()
        .ButtonsTab
        .AssertIsNotSelected()

        // Navigate back
        .ButtonsTab
        .ClickAndNavigate()
        .AssertIsSelected();
}
```

## XAML Configuration

Ensure your AppShell.xaml has AutomationIds on ShellContent elements:

```xaml
<Shell x:Class="Brinell.Samples.Maui.App.AppShell"
       xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
       xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
       AutomationId="AppShell">

    <TabBar>
        <ShellContent
            Title="Buttons"
            AutomationId="ButtonsShell"
            Route="ButtonsPage"
            ContentTemplate="{DataTemplate local:ButtonsPage}" />

        <ShellContent
            Title="DateTime"
            AutomationId="DateTimeShell"
            Route="DateTimePage"
            ContentTemplate="{DataTemplate local:DateTimePage}" />

        <!-- ... more ShellContent items ... -->
    </TabBar>

</Shell>
```

## Locator Strategy

| Locator Type | Usage | Priority | Locator Expression |
|---|---|---|---|
| **AutomationId** | Primary (recommended) | 1 | Direct by AutomationId |
| **Route (XPath)** | Fallback | 2 | `//ShellContent[@Route='ButtonsPage']` |

**Recommendation for Windows**: Use AutomationId constructor for maximum reliability.

## Selection State Detection

The control checks selection via (in order):
1. `Selected` attribute
2. `IsSelected` attribute
3. `aria-selected` attribute
4. `selected` attribute
5. `class` attribute containing "selected" (fallback)

## Related Controls

- **Shell\<TScope\>** - Container for managing multiple ShellContent items
- **Tab\<TScope\>** - Tab control for Shell TabBar (XPath-based)
- **TabViewControl\<TScope\>** - Tab control for CommunityToolkit.Maui TabView
- **ITabControlObject\<TScope\>** - Interface implemented by all tab/shell controls

## Notes

- ShellContent inherits from `ClickableControlBase<TScope>` + `ITabControlObject<TScope>`
- All methods support fluent chaining for readable, expressive tests
- Uses nullable bool (`bool?`) for selection state (null = element not found)
- Selection polling is automatic in `WaitSelected()` and `AssertSelected()`
- Typical usage: one ShellContent per tab/screen in a Shell TabBar
