# Running Brinell.Html Tests (Blazor)

This guide covers how to run and debug HTML/Selenium UI tests using the Brinell framework with Blazor applications.

## Prerequisites

1. **Operating System** - Windows, macOS, or Linux
2. **.NET 8.0/9.0/10.0 SDK** - Required for building and running
3. **Chrome Browser** - Latest version recommended
4. **ChromeDriver** - Matching Chrome version (auto-managed by Selenium)
5. **Visual Studio 2022** or **VS Code** with C# extension

## Project Structure

```
samples/
├── Brinell.Samples.Blazor.App/       # Blazor Server sample application
└── Brinell.Samples.Blazor.UITests/   # Selenium-based UI tests
    ├── PageObjects/       # Page objects
    ├── TestBase/          # Test base classes
    └── Tests/             # Test classes
```

## Building the Projects

```powershell
# Build the Html library
dotnet build src/Brinell.Html -c Debug

# Build the sample application
dotnet build samples/Brinell.Samples.Blazor.App -c Debug

# Build the UI tests
dotnet build samples/Brinell.Samples.Blazor.UITests -c Debug
```

## Running the Blazor Application

**Important:** The Blazor application must be running before executing tests.

```powershell
# Terminal 1: Start the Blazor app
cd samples/Brinell.Samples.Blazor.App
dotnet run --urls "http://localhost:5180"
```

Keep this terminal running while executing tests.

## Running Tests

### From Command Line (Terminal 2)

```powershell
# Set environment variables
$env:BLAZOR_APP_URL = "http://localhost:5180"
$env:HEADLESS = "false"  # Set to "true" for CI/CD

# Run all Blazor UI tests
dotnet test samples/Brinell.Samples.Blazor.UITests

# Run with detailed output
dotnet test samples/Brinell.Samples.Blazor.UITests --logger "console;verbosity=normal"

# Run specific test
dotnet test samples/Brinell.Samples.Blazor.UITests --filter "FullyQualifiedName~CounterTests"

# Run in headless mode
$env:HEADLESS = "true"
dotnet test samples/Brinell.Samples.Blazor.UITests
```

### From Visual Studio

1. Open `Brinell.sln`
2. Start the Blazor app first (set as startup project and run)
3. In Test Explorer, navigate to `Brinell.Samples.Blazor.UITests`
4. Right-click and select "Run Tests"

### From VS Code

1. Open the workspace folder
2. Start the Blazor app in a terminal
3. Use the Test Explorer extension
4. Navigate to Blazor tests and run

## Test Architecture

### Test Base Class

```csharp
using Brinell.Html.Testing;
using Xunit;
using Xunit.Abstractions;

[Collection("BlazorUITests")]
public class MyBlazorTests : BlazorSampleTestBase
{
    public MyBlazorTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void MyTest()
    {
        LaunchBrowser();
        NavigateToPage("/my-page");
        
        var page = new MyPage(Context!);
        page.WaitForDisplayed();
        
        // Test assertions
    }
}
```

### Page Objects

```csharp
using Brinell.Html.Controls;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

public class MyPage : PageBase
{
    public ButtonControl SubmitButton { get; }
    public TextInputControl EmailInput { get; }
    public TableControl DataTable { get; }

    public MyPage(SeleniumTestContext context) : base(context)
    {
        SubmitButton = new ButtonControl(context, this, "#submit-btn");
        EmailInput = new TextInputControl(context, this, "#email-input");
        DataTable = new TableControl(context, this, "#data-table");
    }

    public override string AutomationId => "#page-container";
}
```

## Available Controls

| Control | Purpose | HTML Elements |
|---------|---------|---------------|
| `ButtonControl` | Buttons | `<button>`, `<input type="button">` |
| `LabelControl` | Static text | `<span>`, `<p>`, `<div>`, `<h1>`-`<h6>` |
| `LinkControl` | Hyperlinks | `<a>` |
| `TextInputControl` | Text input | `<input type="text">`, `<input type="email">` |
| `TextAreaControl` | Multi-line text | `<textarea>` |
| `CheckBoxControl` | Checkboxes | `<input type="checkbox">` |
| `SelectControl` | Dropdowns | `<select>` |
| `RangeInputControl` | Sliders | `<input type="range">` |
| `ProgressControl` | Progress bars | `<progress>` |
| `TableControl` | Data tables | `<table>` |
| `ListControl` | Lists | `<ul>`, `<ol>` |
| `ScrollContainerControl` | Scrollable areas | `<div>` with overflow |

## Control Examples

### Button
```csharp
var button = new ButtonControl(context, page, "#my-button");
button.Click();
button.AssertEnabled();
button.AssertVisible();
```

### Text Input
```csharp
var input = new TextInputControl(context, page, "#email");
input.SetText("test@example.com");
input.AssertTextEquals("test@example.com");
input.Clear();
input.AssertTextEmpty();
```

### Table
```csharp
var table = new TableControl(context, page, "#data-table");
int rowCount = table.GetRowCount();
var headers = table.GetHeaders();  // ["Name", "Email", "Status"]
var cellText = table.GetCellText(0, 1);  // Row 0, Column 1
table.ClickRow(2);
table.AssertRowCount(5);
```

### Checkbox
```csharp
var checkbox = new CheckBoxControl(context, page, "#agree-terms");
checkbox.Check();
checkbox.AssertChecked();
checkbox.Uncheck();
checkbox.AssertUnchecked();
```

### Select (Dropdown)
```csharp
var select = new SelectControl(context, page, "#country");
select.SelectByText("United States");
select.AssertSelectedValue("us");
var options = select.GetOptions();
```

## Base Classes

| Base Class | Interface | Purpose |
|------------|-----------|---------|
| `ControlBase` | `IControlObject` | All controls |
| `PageBase` | `IPageObject` | Page objects |
| `LoadingPageBase` | - | Pages with loading indicators |
| `BusyPageBase` | - | Alias for LoadingPageBase |
| `ContentControlBase` | `IContentControl` | Clickable content |
| `TextControlBase` | `IEditableTextControl` | Text inputs |
| `ToggleControlBase` | - | Checkboxes |
| `SelectorControlBase` | - | Dropdowns |
| `RangeControlBase` | `IRangeControl` | Sliders |
| `ItemsControlBase` | `IItemsControl` | Lists, tables |
| `ScrollableControlBase` | `IScrollableControl` | Scroll containers |

## Blazor-Specific Considerations

### Wait for Blazor Ready

The test base includes helpers for Blazor:

```csharp
// Automatically called by NavigateToPage()
WaitForBlazorReady();
WaitForDocumentReady();
WaitForBlazorConnection();
```

### Element Identification

Use CSS selectors or `id` attributes:
```csharp
// By ID
new ButtonControl(context, page, "#submit-btn");

// By CSS selector
new LabelControl(context, page, ".alert-message");

// By data-testid
new TextInputControl(context, page, "[data-testid='email-input']");
```

### Blazor Rendering Delays

Blazor's async rendering may cause timing issues. Use waits:

```csharp
// Wait for text to update
label.WaitTextEquals("Expected Value");

// Wait for element visibility
button.WaitVisible(expected: true);

// Wait for table data
table.WaitItemCountAtLeast(3);
```

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `BLAZOR_APP_URL` | `http://localhost:5180` | Base URL of Blazor app |
| `HEADLESS` | `false` | Run browser without UI |

## Troubleshooting

### Common Issues

1. **Connection Refused**
   - Verify Blazor app is running
   - Check `BLAZOR_APP_URL` matches app's URL
   - Ensure correct port (default: 5180)

2. **Element Not Found**
   - Verify CSS selector is correct
   - Use browser DevTools to inspect elements
   - Add `WaitForDisplayed()` before interaction

3. **Stale Element Reference**
   - Blazor re-rendered the DOM
   - Use waits before accessing elements
   - Don't cache element references across navigations

4. **Tests Timing Out**
   - Increase default timeout
   - Check for Blazor connection issues
   - Look for JavaScript errors in browser console

### Debugging Tips

1. **Run in visible mode**
   ```powershell
   $env:HEADLESS = "false"
   ```

2. **Take screenshots**
   ```csharp
   page.TakeScreenshot("before_click");
   ```

3. **Check logs**
   ```csharp
   _context.Log("Debug: current state");
   ```

4. **Use browser DevTools**
   - Run in visible mode
   - Pause test with debugger
   - Inspect element hierarchy

## Test Output

Tests generate:
- Console logs with timestamps
- CSV action logs (when logger configured)
- Screenshots on failure

---

*See also: [Brinell.Html Documentation](../../src/Brinell.Html/README.md)*
