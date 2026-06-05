# Test Writing Guide

Quick reference for writing UI tests with common patterns and examples.

---

## Test Structure Template

```csharp
public class FeatureTests : WpfUITestBase  // or MauiUITestBase, HtmlUITestBase
{
    [Fact]
    public void Action_Scenario_ExpectedResult()
    {
        // Arrange
        var page = LaunchApp<MainPage>();
        page.WaitForPageReady();
        
        // Act
        page.PerformAction();
        
        // Assert
        page.ResultControl.AssertText("Expected");
    }
}
```

---

## Composition Template

```csharp
[TestModuleScan(typeof(AppFixture), NamespacePrefix = "MyApp.UITests")]
public sealed class AppFixture : IDisposable
{
    public AppFixture()
    {
        Context = CreateContext();

        Composition = TestComposition.ForFixture(this, services =>
            services.AddSingleton<IMauiTestContext>(Context));
    }

    public IMauiTestContext Context { get; }

    public TestComposition Composition { get; }

    public void Dispose() => Context.Dispose();
}

[TestPage("Settings")]
public sealed class SettingsPage : PageObjectBase<SettingsPage>
{
    public SettingsPage(IMauiTestContext context)
        : base(context)
    {
    }

    public Entry<SettingsPage> UsernameInput => new(this, "UsernameInput");

    public Button<SettingsPage> SaveButton => new(this, "SaveButton");

    public Label<SettingsPage> StatusLabel => new(this, "StatusLabel");
}

[TestScenarioService]
public sealed class SettingsFlow : TestScenarioServiceBase
{
    private readonly SettingsPage _settings;

    public SettingsFlow(SettingsPage settings)
    {
        _settings = settings;
    }

    public void SaveUsername(string username)
    {
        _settings.UsernameInput.SetText(username);
        _settings.SaveButton.Click();
    }
}

public sealed class SettingsTests(AppFixture fixture) : IClassFixture<AppFixture>
{
    [Fact]
    public void SaveUsername_ShowsSavedStatus()
    {
        using var scope = fixture.Composition.CreateScope();
        var page = scope.ServiceProvider.GetRequiredService<SettingsPage>();
        var flow = scope.ServiceProvider.GetRequiredService<SettingsFlow>();

        flow.SaveUsername("newuser");

        page.StatusLabel.AssertTextContains("Saved");
    }
}
```

---

## Page Object Template

```csharp
using Oravey.UITestFramework.Wpf;

public class SettingsPage : BusyPageBase
{
    // Busy indicator ID (required for BusyPageBase)
    protected override string BusyIndicatorId => "SettingsPageBusyIndicator";
    
    // Declare all controls as properties
    public TextBoxControl UsernameInput { get; }
    public TextBoxControl EmailInput { get; }
    public ButtonControl SaveButton { get; }
    public ButtonControl CancelButton { get; }
    public LabelControl StatusLabel { get; }
    
    // Constructor: Initialize with context
    public SettingsPage(FlaUITestContext context) 
        : base(context, "Settings")
    {
        UsernameInput = new TextBoxControl(context, this, "UsernameInput");
        EmailInput = new TextBoxControl(context, this, "EmailInput");
        SaveButton = new ButtonControl(context, this, "SaveButton");
        CancelButton = new ButtonControl(context, this, "CancelButton");
        StatusLabel = new LabelControl(context, this, "StatusLabel");
    }
    
    // Key control for IsDisplayed check
    public override bool IsDisplayed() => SaveButton.IsVisible();
    
    // Action methods (fluent pattern optional)
    public void UpdateSettings(string username, string email)
    {
        Log($"Updating settings: {username}, {email}");
        UsernameInput.EnterText(username);
        EmailInput.EnterText(email);
    }
    
    public void Save()
    {
        SaveButton.Click();
        WaitForNotBusy();  // Wait for save operation
    }
    
    public void NavigateToProfile()
    {
        ProfileButton.Click();
    }
}
```

---

## Control Reference

### Common Controls

| Control Type | Interface | Key Methods |
|-------------|-----------|-------------|
| **Button** | `IContentControl` | `Click()`, `DoubleClick()` |
| **Label** | `IControlObject` | `GetText()`, `AssertText()` |
| **TextBox** | `ITextControl` | `EnterText()`, `Clear()`, `GetText()` |
| **CheckBox** | `IToggleControl` | `Toggle()`, `SetChecked()`, `IsChecked()` |
| **ComboBox** | `ISelectorControl` | `SelectItem()`, `GetSelectedItem()` |
| **ListBox** | `IItemsControl` | `GetItems()`, `GetItemCount()`, `SelectItem()` |
| **Slider** | `IRangeControl` | `SetValue()`, `GetValue()`, `GetMinimum()`, `GetMaximum()` |

### Control Creation in Page Objects

```csharp
// In constructor of PageBase-derived class
public SettingsPage(FlaUITestContext context) : base(context, "Settings")
{
    // Button
    SaveButton = new ButtonControl(context, this, "SaveButton");
    
    // TextBox
    UsernameInput = new TextBoxControl(context, this, "UsernameInput");
    
    // Label
    StatusLabel = new LabelControl(context, this, "StatusLabel");
    
    // CheckBox
    EnabledCheckbox = new CheckBoxControl(context, this, "EnabledCheckbox");
    
    // ComboBox
    ThemeSelector = new ComboBoxControl(context, this, "ThemeSelector");
    
    // ListBox
    ItemsList = new ListBoxControl(context, this, "ItemsList");
    
    // Slider
    VolumeSlider = new SliderControl(context, this, "VolumeSlider");
}
```

---

## Common Patterns

### Pattern 1: Navigation

```csharp
// In source page object
public void NavigateToSettings()
{
    Log("Navigating to Settings");
    SettingsButton.Click();
}

// In test
[Fact]
public void Test_Navigation_To_Settings()
{
    var home = LaunchApp<HomePage>();
    home.WaitForPageReady();
    
    // Navigate (returns void)
    home.NavigateToSettings();
    
    // Create target page and wait
    var settings = new SettingsPage(Context);
    settings.WaitForPageReady();
    
    // Assert navigation succeeded
    settings.AssertDisplayed();
}
```

### Pattern 2: Form Input

```csharp
[Fact]
public void Test_Update_User_Info()
{
    var settings = LaunchApp<SettingsPage>();
    settings.WaitForPageReady();
    
    // Enter values
    settings.UsernameInput.EnterText("newuser");
    settings.EmailInput.EnterText("new@example.com");
    
    // Save
    settings.SaveButton.Click();
    settings.WaitForNotBusy();
    
    // Verify saved
    settings.StatusLabel.AssertText("Saved successfully");
}
```

### Pattern 3: Data-Driven Tests

```csharp
[Theory]
[InlineData("", "email@test.com", "Username required")]
[InlineData("user", "", "Email required")]
[InlineData("user", "invalid", "Invalid email format")]
public void Test_Validation_Messages(string username, string email, string expectedError)
{
    var settings = LaunchApp<SettingsPage>();
    settings.WaitForPageReady();
    
    settings.UsernameInput.EnterText(username);
    settings.EmailInput.EnterText(email);
    settings.SaveButton.Click();
    
    settings.ErrorLabel.AssertText(expectedError);
}
```

### Pattern 4: Dialog Handling

```csharp
// Create dialog page object
public class ConfirmDialog : PageBase
{
    public LabelControl MessageLabel { get; }
    public ButtonControl YesButton { get; }
    public ButtonControl NoButton { get; }
    
    public ConfirmDialog(FlaUITestContext context) : base(context, "ConfirmDialog")
    {
        MessageLabel = new LabelControl(context, this, "DialogMessage");
        YesButton = new ButtonControl(context, this, "YesButton");
        NoButton = new ButtonControl(context, this, "NoButton");
    }
    
    public override bool IsDisplayed() => MessageLabel.IsVisible();
    
    public void Confirm()
    {
        YesButton.Click();
        Context.WaitFor(() => !IsDisplayed(), Context.DefaultTimeoutMs, "Dialog closed");
    }
    
    public void Cancel()
    {
        NoButton.Click();
        Context.WaitFor(() => !IsDisplayed(), Context.DefaultTimeoutMs, "Dialog closed");
    }
}

// In test
[Fact]
public void Test_Delete_With_Confirmation()
{
    var page = LaunchApp<DataPage>();
    page.WaitForPageReady();
    
    page.DeleteButton.Click();
    
    var confirmDialog = new ConfirmDialog(Context);
    confirmDialog.WaitForDisplayed();
    confirmDialog.MessageLabel.AssertText("Are you sure?");
    confirmDialog.Confirm();
    
    page.StatusLabel.AssertText("Deleted");
}
```

### Pattern 5: List/Collection Handling

```csharp
[Fact]
public void Test_List_Contains_Items()
{
    var page = LaunchApp<ListPage>();
    page.WaitForPageReady();
    
    // Assert count
    page.ItemsList.AssertItemCount(5);
    
    // Assert specific item exists
    page.ItemsList.AssertItemExists("Item 1");
    
    // Select item
    page.ItemsList.SelectItem("Item 2");
    
    // Verify selection
    page.SelectedItemLabel.AssertText("Item 2");
}
```

### Pattern 6: Async Operations

```csharp
[Fact]
public void Test_Async_Operation()
{
    var page = LaunchApp<DashboardPage>();
    page.WaitForPageReady();
    
    // Trigger async operation
    page.RefreshButton.Click();
    
    // Wait for loading to complete
    page.WaitForNotBusy();
    
    // Verify results
    page.DataGrid.AssertItemCount(10);
}
```

---

## Assertion Patterns

### Visibility Assertions

```csharp
// Element is visible
control.AssertVisible();

// Element is not visible
control.AssertNotVisible();

// Page is displayed
page.AssertDisplayed();
```

### State Assertions

```csharp
// Button is enabled
button.AssertEnabled();

// Button is disabled
button.AssertDisabled();

// Checkbox is checked
checkbox.AssertChecked();

// Checkbox is unchecked
checkbox.AssertUnchecked();
```

### Value Assertions

```csharp
// Exact text match
label.AssertText("Expected Text");

// Text contains substring
label.AssertTextContains("partial");

// Selected item in dropdown
comboBox.AssertSelectedItem("Option 1");

// Slider value (with tolerance)
slider.AssertValue(50.0, tolerance: 0.1);

// List item count
listBox.AssertItemCount(10);
```

---

## Wait Patterns

### Wait for Element State

```csharp
// Wait for visible
element.WaitVisible(true);

// Wait for hidden
element.WaitVisible(false);

// Wait for enabled
element.WaitEnabled(true);

// Wait for specific text
element.WaitText("Expected");
```

### Wait for Page State

```csharp
// Wait for page displayed
page.WaitForDisplayed();

// Wait for page hidden
page.WaitForHidden();

// Wait for page ready (displayed + not busy)
page.WaitForPageReady();

// Wait for not busy
page.WaitForNotBusy();
```

### Wait for Custom Conditions

```csharp
// Wait with custom condition
Context.WaitFor(
    () => listBox.GetItemCount() > 0,
    timeoutMs: 5000,
    description: "List populated"
);

// Wait for complex state
Context.WaitFor(
    () => button.IsEnabled() && !page.IsBusy(),
    timeoutMs: 10000,
    description: "Button ready"
);
```

---

## Test Naming Conventions

### Test Method Names

Format: `Action_Scenario_ExpectedResult`

```csharp
[Fact]
public void Login_WithValidCredentials_NavigatesToMainPage() { }

[Fact]
public void Save_WithInvalidData_ShowsValidationErrors() { }

[Fact]
public void Delete_WhenConfirmed_RemovesItem() { }

[Fact]
public void Navigation_ToSettings_DisplaysSettingsPage() { }
```

### Page Object Names

Format: `{Feature}Page` or `{Feature}Dialog`

```csharp
public class MainWindowPage : BusyPageBase { }
public class SettingsPage : BusyPageBase { }
public class LoginDialog : PageBase { }
public class ConfirmDeleteDialog : PageBase { }
```

### Control Property Names

Format: `{Name}{ControlType}`

```csharp
public ButtonControl SaveButton { get; }
public TextBoxControl UsernameInput { get; }
public LabelControl StatusLabel { get; }
public CheckBoxControl EnabledCheckbox { get; }
public ComboBoxControl ThemeSelector { get; }
```

---

## Test Organization

### Organize by Feature

```
Tests/
├── NavigationTests.cs      # Navigation between views
├── SettingsTests.cs        # Settings functionality
├── DataEntryTests.cs       # Data input and validation
└── SearchTests.cs          # Search functionality
```

### Use Test Categories

```csharp
[Trait("Category", "Smoke")]
[Fact]
public void Application_Launches_Successfully() { }

[Trait("Category", "Navigation")]
[Fact]
public void Navigation_To_Settings_Succeeds() { }

[Trait("Category", "Regression")]
[Fact]
public void Save_With_Valid_Data_Succeeds() { }
```

### Run Specific Categories

```bash
# Run only smoke tests
dotnet test --filter "Category=Smoke"

# Run navigation tests
dotnet test --filter "Category=Navigation"

# Exclude slow tests
dotnet test --filter "Category!=Slow"
```

---

## Best Practices Checklist

### DO ✅

- Use descriptive test and page names
- Use `TestComposition` for page, flow, and scenario service construction
- Wait for page ready after navigation
- Use page object actions for complex interactions
- Assert one behavior per test
- Use data-driven tests for validation scenarios
- Clean up test data in Dispose
- Take screenshots on failure
- Use configuration for timeouts
- Log significant actions

### DON'T ❌

- Use `Thread.Sleep()` - use waits instead
- Hardcode timeouts - use configuration
- Put assertions in page objects - keep in tests
- Access controls directly in tests - use page methods
- Share state between tests
- Ignore busy indicators
- Skip page readiness checks
- Use magic strings - define constants
- Create page catalog classes or fixture-owned page properties for new tests

---

## Quick Reference Card

```
┌─────────────────────────────────────────────────────────────┐
│ CONTROL ACTIONS                                             │
├─────────────────────────────────────────────────────────────┤
│ button.Click()                  textBox.EnterText("value")  │
│ checkbox.Check()                comboBox.SelectItem("name") │
│ listBox.SelectItem(0)           slider.SetValue(50.0)       │
│ textBox.Clear()                 checkbox.Toggle()           │
├─────────────────────────────────────────────────────────────┤
│ ASSERTIONS                                                  │
├─────────────────────────────────────────────────────────────┤
│ .AssertVisible()                .AssertNotVisible()         │
│ .AssertEnabled()                .AssertDisabled()           │
│ .AssertText("expected")         .AssertTextContains("part") │
│ .AssertChecked()                .AssertUnchecked()          │
│ .AssertSelectedItem("item")     .AssertItemCount(n)         │
├─────────────────────────────────────────────────────────────┤
│ WAITS                                                       │
├─────────────────────────────────────────────────────────────┤
│ .WaitVisible(true)              .WaitEnabled(true)          │
│ .WaitText("expected")           .WaitClickable()            │
│ page.WaitForPageReady()         page.WaitForNotBusy()       │
├─────────────────────────────────────────────────────────────┤
│ NAVIGATION                                                  │
├─────────────────────────────────────────────────────────────┤
│ page.NavigateToTarget()         // Returns void            │
│ var target = new TargetPage(Context);                      │
│ target.WaitForPageReady();      // Explicit wait           │
└─────────────────────────────────────────────────────────────┘
```

---

## Common Issues and Solutions

| Issue | Solution |
|-------|----------|
| Element not found | Verify AutomationId, add `WaitForVisible()` |
| Intermittent failures | Use `WaitForPageReady()`, check IsBusy |
| Click not working | Ensure element is clickable with `CheckClickable()` |
| Wrong values | Wait for page ready before reading |
| Tests fail in CI | Use consistent timeouts, check environment |

---

*See also: [Best Practices](12-best-practices.md) | [Troubleshooting](13-troubleshooting.md)*
