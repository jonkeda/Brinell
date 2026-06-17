# Shell Control Object

## Overview

The `Shell<TScope>` control object provides a high-level abstraction for interacting with MAUI Shell containers, particularly those using TabBar navigation.

**Location:** `srcnew/Brinell.Maui.Extensions/Controls/Navigation/Shell.cs`

## Features

- **Tab Management**: Get and cache Tab control objects by title
- **Navigation**: Navigate between tabs with fluent API
- **State Checking**: Check if specific tabs are selected
- **Assertions**: Assert shell and tab states
- **Shell Visibility**: Verify shell is loaded/visible

## Basic Usage

### Page Object Setup

```csharp
using Brinell.Maui.Extensions.Controls.Navigation;

public class AppShellPage : PageObjectBase<AppShellPage>
{
    private readonly Shell<AppShellPage> _shell;

    public AppShellPage(IMauiTestContext context) : base(context)
    {
        // Create shell with default AutomationId "AppShell"
        _shell = new Shell<AppShellPage>(this);
    }

    public override string Name => "AppShell";
    public override bool IsLoaded(int? timeoutMs = null) => _shell.IsLoaded();
}
```

### Navigation

```csharp
// Navigate to a tab
page.NavigateTo("Buttons");

// Or use semantic methods
page.GoToContainersTab();
```

### Tab State Checking

```csharp
// Check if a tab is selected
bool? isSelected = page.ShellControl.IsTabSelected("Buttons");

// Wait for a tab to be selected
var loaded = page.ShellControl.WaitTabSelected("Buttons", expected: true, timeoutMs: 5000);
```

### Assertions

```csharp
// Assert tab is selected
page.AssertTabIsSelected("Buttons");

// Assert tab is not selected
page.AssertTabIsNotSelected("DateTime");

// Assert shell is loaded
page.AssertShellLoaded();
```

## API Reference

### Constructor

```csharp
public Shell(IMauiScope<TScope> scope, string automationId = "AppShell")
```

- **scope**: The page/scope providing element finding
- **automationId**: AutomationId of the Shell element (default: "AppShell")

### Tab Access

#### GetTab(string title)
Returns a Tab control object by its title. Tabs are cached for reuse.

```csharp
var buttonsTab = shell.GetTab("Buttons");
buttonsTab.Click();
```

#### GetSelectedTab()
Returns the currently selected tab (returns null if no tab is selected or shell not found).

```csharp
var selectedTab = shell.GetSelectedTab();
```

### Navigation

#### NavigateTo(string title)
Navigates to a tab by clicking it. Returns the containing scope for chaining.

```csharp
page.NavigateTo("Containers")
    .AssertTabIsSelected("Containers");
```

### State Checking

#### IsTabSelected(string title)
Checks if a specific tab is currently selected.

```csharp
if (shell.IsTabSelected("Buttons") == true)
{
    // Buttons tab is selected
}
```

#### WaitTabSelected(string title, bool? expected, int? timeoutMs)
Waits for a tab to reach a specific selection state.

```csharp
bool waited = shell.WaitTabSelected("DateTime", expected: true, timeoutMs: 5000);
```

#### AssertTabSelected(string title, bool? expected, string? message, int? timeoutMs)
Asserts a tab's selection state with optional custom error message.

```csharp
page.AssertTabSelected("Basics", expected: true, 
    message: "Basics tab should be selected after navigation");
```

### Shell State

#### IsLoaded()
Checks if the shell is present in the DOM.

```csharp
if (shell.IsLoaded())
{
    // Shell is visible and ready
}
```

#### WaitLoaded(int? timeoutMs)
Waits for the shell to appear.

```csharp
shell.WaitLoaded(timeoutMs: 10000);
```

#### AssertLoaded(string? message, int? timeoutMs)
Asserts the shell is loaded with optional timeout.

```csharp
page.AssertShellLoaded();
```

## Example Test

```csharp
[Fact]
public void TestShellTabNavigation()
{
    var page = new AppShellPage(_context);

    // Wait for shell to load
    page.AssertShellLoaded()

        // Navigate to Containers tab
        .GoToContainersTab()
        .AssertTabIsSelected("Containers")

        // Navigate to Forms tab
        .GoToFormsTab()
        .AssertTabIsSelected("Forms")
        .AssertTabIsNotSelected("Containers")

        // Navigate back to Buttons
        .PressTab(Keys.Shift) // Or use shell directly
        .AssertTabIsSelected("Buttons");
}
```

## XAML Configuration

For the Shell control to work properly, ensure your AppShell.xaml has the correct AutomationId:

```xaml
<Shell x:Class="Brinell.Samples.Maui.App.AppShell"
       xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
       xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
       AutomationId="AppShell">

    <TabBar>
        <ShellContent Title="Buttons" AutomationId="ButtonsTab" ... />
        <ShellContent Title="DateTime" AutomationId="DateTimeTab" ... />
        <!-- ... more tabs ... -->
    </TabBar>

</Shell>
```

## Related Controls

- **Tab\<TScope\>** - Individual tab control for Shell TabBar navigation
- **TabViewControl\<TScope\>** - Individual tab control for CommunityToolkit TabView
- **ITabControlObject\<TScope\>** - Interface implemented by all tab controls

## Notes

- Tab objects are **cached** by title to avoid redundant object creation
- The nullable skip pattern (`null` skips checks) is used throughout for flexibility
- All methods support fluent chaining for readable, expressive tests
- Shell assumes tabs are located via XPath on their Title attribute (as per `Tab<TScope>` implementation)
