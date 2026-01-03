# Running Brinell.WinForms Tests

This guide covers how to run and debug WinForms UI tests using the Brinell framework.

## Prerequisites

1. **Windows OS** - WinForms tests require Windows
2. **.NET 8.0/9.0/10.0 SDK** - Required for building and running
3. **Visual Studio 2022** or **VS Code** with C# extension
4. **WinForms Sample App** - The sample application to test

## Project Structure

```
samples/
├── Brinell.Samples.WinForms.App/     # WinForms sample application
└── Brinell.Samples.WinForms.UITests/ # WinForms UI tests
    ├── Fixtures/          # Test fixtures
    ├── Infrastructure/    # Test base classes
    ├── Pages/             # Page objects
    └── Tests/             # Test classes
```

## Building the Projects

```powershell
# Build the WinForms library
dotnet build src/Brinell.WinForms -c Debug

# Build the sample application
dotnet build samples/Brinell.Samples.WinForms.App -c Debug

# Build the UI tests
dotnet build samples/Brinell.Samples.WinForms.UITests -c Debug
```

## Running Tests

### From Command Line

```powershell
# Run all WinForms UI tests
dotnet test samples/Brinell.Samples.WinForms.UITests --verbosity minimal

# Run with detailed output
dotnet test samples/Brinell.Samples.WinForms.UITests --verbosity normal

# Run specific test
dotnet test samples/Brinell.Samples.WinForms.UITests --filter "FullyQualifiedName~TestName"

# Run tests in a specific class
dotnet test samples/Brinell.Samples.WinForms.UITests --filter "ClassName~ButtonTests"
```

### From Visual Studio

1. Open `Brinell.sln`
2. In Test Explorer, navigate to `Brinell.Samples.WinForms.UITests`
3. Right-click and select "Run Tests"

### From VS Code

1. Open the workspace folder
2. Use the Test Explorer extension
3. Navigate to WinForms tests and run

## WinForms Test Architecture

### Test Base Class

```csharp
using Brinell.WinForms.Testing;

public class MyWinFormsTests : WinFormsUITestBase
{
    protected override string ApplicationPath => 
        @"path\to\WinForms.App.exe";

    [Fact]
    public void MyTest()
    {
        LaunchApplication();
        // Test code here
    }
}
```

### Page Objects

```csharp
using Brinell.WinForms.Controls.Base;
using Brinell.WinForms.Infrastructure;

public class MainPage : PageBase
{
    public ButtonControl SubmitButton { get; }
    public TextBoxControl NameInput { get; }

    public MainPage(FlaUITestContext context) 
        : base(context, "MainForm")
    {
        SubmitButton = new ButtonControl(context, this, "submitButton");
        NameInput = new TextBoxControl(context, this, "nameTextBox");
    }

    public override bool IsDisplayed()
    {
        return SubmitButton.IsExists();
    }
}
```

### Control Examples

```csharp
// Button
var button = new ButtonControl(context, page, "myButton");
button.Click();
button.AssertEnabled();

// TextBox
var textBox = new TextBoxControl(context, page, "myTextBox");
textBox.SetText("Hello");
textBox.AssertTextEquals("Hello");

// CheckBox
var checkBox = new CheckBoxControl(context, page, "myCheckBox");
checkBox.Check();
checkBox.AssertChecked();

// ComboBox
var comboBox = new ComboBoxControl(context, page, "myComboBox");
comboBox.SelectByText("Option 1");
comboBox.AssertSelectedItem("Option 1");

// TrackBar
var trackBar = new TrackBarControl(context, page, "myTrackBar");
trackBar.SetValue(50);
trackBar.AssertValueEquals(50);
```

## Base Classes Available

| Base Class | Interface | Purpose |
|------------|-----------|---------|
| `ControlBase` | `IControlObject` | All controls base |
| `TextControlBase` | `IEditableTextControl` | Text input controls |
| `ContentControlBase` | `IContentControl` | Clickable content |
| `ToggleControlBase` | - | Checkboxes, radio buttons |
| `SelectorControlBase` | - | ComboBox, ListBox |
| `RangeControlBase` | `IRangeControl` | Sliders, progress bars |
| `ItemsControlBase` | `IItemsControl` | Data grids, list views |
| `PageBase` | `IPageObject` | Page objects |
| `BusyPageBase` | - | Pages with loading state |

## Busy Page Pattern

For pages with loading indicators:

```csharp
public class LoadingPage : BusyPageBase
{
    private ProgressBarControl LoadingIndicator { get; }

    public LoadingPage(FlaUITestContext context) 
        : base(context, "LoadingForm")
    {
        LoadingIndicator = new ProgressBarControl(context, this, "loadingProgress");
    }

    public override bool IsDisplayed() => LoadingIndicator.IsExists();

    public override bool IsBusy() => LoadingIndicator.IsVisible();
}

// Usage
var page = new LoadingPage(context);
page.WaitForNotBusy();  // Wait for loading to complete
page.AssertReady();     // Assert displayed and not busy
```

## Scroll Support

For scrollable containers:

```csharp
var scrollView = new ScrollViewControl(context, page, "scrollPanel");

// Navigation
scrollView.ScrollToTop();
scrollView.ScrollToBottom();
scrollView.ScrollDown(20);  // 20% scroll
scrollView.ScrollUp(20);

// Find element
scrollView.ScrollToElement("targetElementId");

// State
bool canScroll = scrollView.IsVerticallyScrollable();
double position = scrollView.GetVerticalScrollPercent();
```

## Troubleshooting

### Common Issues

1. **Application not found**
   - Verify `ApplicationPath` is correct
   - Ensure application is built before running tests

2. **Element not found**
   - Check AutomationId matches exactly
   - Use Inspect.exe or FlaUInspect to verify element properties
   - Add wait time with `WaitForDisplayed()`

3. **Click not working**
   - Ensure element is visible and enabled
   - Try using `element.Focus()` before click
   - Use `WaitForElementVisible()` before interaction

4. **Tests timing out**
   - Increase timeout: `context.DefaultTimeoutMs = 10000`
   - Check if application is responding
   - Look for modal dialogs blocking the UI

### Debugging Tips

1. **Take screenshots on failure**
   ```csharp
   page.TakeScreenshot("before_action");
   ```

2. **Enable verbose logging**
   ```csharp
   _context.Log("Debug message");
   ```

3. **Use FlaUInspect**
   - Download from FlaUI releases
   - Inspect element hierarchy and properties
   - Verify AutomationIds

4. **Check CSV logs**
   - Located in test output directory
   - Contains all actions and assertions

## Test Output

Tests generate CSV log files with:
- Test name
- Page and control interactions
- Assertions (pass/fail)
- Timing information

Screenshots are captured on failures and saved to the output directory.

---

*See also: [Brinell.WinForms Documentation](../../src/Brinell.WinForms/README.md)*
