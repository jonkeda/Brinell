# Running WPF UI Tests

This guide explains how to run the Brinell.Samples.Wpf.UITests project.

## Quick Start

```powershell
# Build and run tests
cd samples/Brinell.Samples.Wpf.UITests
dotnet build
dotnet test
```

## Prerequisites

### 1. Windows OS
WPF UI tests require Windows 10/11. They use FlaUI with Windows UI Automation.

### 2. .NET 9 SDK
Install from https://dotnet.microsoft.com/download/dotnet/9.0

### 3. Build Configuration
Both the app and test projects must be built with the same configuration (Debug/Release).

## Project Structure

```
samples/
├── Brinell.Samples.Wpf.App/         # WPF application under test
│   └── bin/Debug/net9.0-windows/
│       └── Brinell.Samples.Wpf.App.exe
│
└── Brinell.Samples.Wpf.UITests/     # UI test project
    ├── TestBase/
    │   └── WpfSampleTestBase.cs     # Base class with app path
    ├── PageObjects/
    │   ├── ShellPage.cs
    │   ├── LoginPage.cs
    │   └── HomePage.cs
    └── Tests/
        ├── LoginTests.cs
        ├── NavigationTests.cs
        └── IsBusyTests.cs
```

## Building

```powershell
# From repo root
dotnet build samples/Brinell.Samples.Wpf.App
dotnet build samples/Brinell.Samples.Wpf.UITests
```

## Running Tests

### Run All Tests
```powershell
dotnet test samples/Brinell.Samples.Wpf.UITests
```

### Run Specific Test
```powershell
dotnet test samples/Brinell.Samples.Wpf.UITests --filter "Login_WithValidCredentials"
```

### Run with Verbose Output
```powershell
dotnet test samples/Brinell.Samples.Wpf.UITests --logger "console;verbosity=detailed"
```

## Architecture

### FlaUI Integration
- **FlaUI.Core**: Windows UI Automation library
- **FlaUI.UIA3**: UIA3 implementation (recommended for WPF)
- Direct access to UI elements via AutomationId

### Test Context
```csharp
public class FlaUITestContext : ITestContext
{
    public Window MainWindow { get; }          // Main app window
    public FlaUIDriverAdapter Driver { get; }  // Element access
    public ITestLogger? Logger { get; }        // CSV logging
}
```

### Page Objects
```csharp
public class LoginPage : PageBase
{
    public TextBoxControl UsernameTextBox { get; }
    public PasswordBoxControl PasswordBox { get; }
    public ButtonControl LoginButton { get; }
    
    public override bool IsDisplayed() =>
        UsernameTextBox.IsVisible();
}
```

### Control Base Classes
| Class | Purpose |
|-------|---------|
| `ControlBase` | Base for all controls, Is/Wait/Check/Assert pattern |
| `ContentControlBase` | Clickable controls (Button, Label) |
| `TextControlBase` | Text input (TextBox, PasswordBox) |
| `ToggleControlBase` | Toggle controls (CheckBox, RadioButton) |
| `SelectorControlBase` | Selection (ComboBox, ListBox) |
| `RangeControlBase` | Numeric range (Slider, ProgressBar) |
| `ItemsControlBase` | Collections (ListBox, DataGrid) |
| `ScrollViewControl` | Scrollable containers (ScrollViewer) |

## Scrolling Support

WPF tests support scrolling via `ScrollViewControl`:

```csharp
// Scroll to element
scrollView.ScrollToElement("TargetElementId");

// Scroll by direction
scrollView.ScrollDown(20);    // 20% scroll
scrollView.ScrollToBottom();
scrollView.ScrollToTop();
```

## BusyPageBase

For pages with loading indicators:

```csharp
public class DataPage : BusyPageBase
{
    public override bool IsBusy()
    {
        return LoadingIndicator.IsVisible();
    }
}

// Usage
dataPage.WaitForNotBusy();
dataPage.WaitForReady(); // Displayed AND not busy
```

## Troubleshooting

### Application Not Found
```
FileNotFoundException: Application not found at '...'
```
**Solution:** Build the WPF app first:
```powershell
dotnet build samples/Brinell.Samples.Wpf.App
```

### Element Not Found
```
Element 'ControlId' not found
```
**Causes:**
1. Wrong AutomationId
2. Element not visible (needs scroll)
3. Page not loaded yet

**Solution:** Use waits and scrolling:
```csharp
page.WaitForDisplayed();
scrollView.ScrollToElement("ControlId");
control.WaitVisible();
```

### Tests Running Slowly
- FlaUI polls at 100ms intervals by default
- Adjust timeout in test context if needed
- Ensure app responds to UI Automation

### Visual Studio Test Explorer
- Tests run in parallel by default
- WPF tests may conflict if multiple instances
- Use `[Collection("UITests")]` to serialize tests

## CSV Logging

Test results are logged to CSV:
```
TestResults/
└── {TestName}_{Timestamp}.csv
```

Log entries include:
- Actions (Click, Enter, etc.)
- Assertions (Pass/Fail)
- Waits (Success/Timeout)
- Errors

## Screenshots

Failure screenshots are saved to:
```
%TEMP%/OraveyUITests/
└── {TestName}_{Page}_{Timestamp}.png
```
