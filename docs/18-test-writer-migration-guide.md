# Test Writer Migration Guide

**Document Version:** 1.0  
**Status:** Phase 1, Task 5, Step 6 - Documentation  
**Last Updated:** January 3, 2026  
**Audience:** Test Engineers & QA Automation Specialists

---

## Table of Contents

1. [What's Changed](#1-whats-changed)
2. [Before & After Examples](#2-before--after-examples)
3. [Migration Checklist](#3-migration-checklist)
4. [Common Migration Scenarios](#4-common-migration-scenarios)
5. [Breaking Changes & Deprecated Patterns](#5-breaking-changes--deprecated-patterns)
6. [Backward Compatibility](#6-backward-compatibility)
7. [FAQ for Test Writers](#7-faq-for-test-writers)

---

## 1. What's Changed

### The Evolution

**Before (Platform-Specific Code):**
```csharp
// WPF-specific test code
var button = page.GetControl<WpfButtonControl>("submitButton");
button.InvokePattern();  // WPF-specific method

// Cannot run on other platforms without rewriting
```

**After (Platform-Agnostic Code):**
```csharp
// Works on ALL platforms: MAUI, WPF, WinForms, Html, Playwright, Stride
var button = page.GetControl<IClickable>("submitButton");
button.Click();  // Same method on all platforms
```

### Key Changes

| Aspect | Before | After |
|--------|--------|-------|
| **Control Types** | Platform-specific (WpfButtonControl, MauiButtonControl) | Unified interfaces (IClickable, ITextInputControl) |
| **Method Names** | Varied (InvokePattern, LeftClick, SendKeys) | Standardized (Click, Enter, Copy) |
| **Test Reusability** | Platform-bound (code rewrite required) | Platform-independent (100% reusable) |
| **Interface Coverage** | Limited (basic operations only) | Comprehensive (49 unified interfaces) |
| **Base Classes** | Basic platform wrappers | Enhanced with all interface implementations |
| **Keyboard Ops** | Platform-specific methods | Unified methods (Copy, Cut, Paste, Undo, Redo) |

### What You Benefit From

✅ **Write Once, Run Everywhere:** Same test code on 6 platforms  
✅ **Consistent Method Signatures:** Predictable, documented patterns  
✅ **Better IDE Support:** Strong typing with interface definitions  
✅ **Easier Maintenance:** Single test implementation vs. 6 versions  
✅ **Cleaner Code:** Less conditional platform logic  
✅ **Future Compatibility:** New platforms use same interface pattern  

---

## 2. Before & After Examples

### Example 1: Text Input

#### Before (WPF)
```csharp
// WPF-specific control
var usernameField = page.GetControl<WpfTextControl>("username");
usernameField.SetText("john.doe@example.com");  // WPF method
var text = usernameField.GetValue();  // WPF-specific getter
```

#### Before (Html/Selenium)
```csharp
// Selenium-specific control
var usernameField = page.GetControl<HtmlTextControl>("username");
usernameField.Clear();  // Selenium prerequisite
usernameField.SendKeys("john.doe@example.com");  // Selenium method
var text = usernameField.GetAttribute("value");  // Selenium-specific
```

#### After (All Platforms)
```csharp
// UNIFIED - Works on WPF, Selenium, MAUI, Playwright, WinForms, Stride
var usernameField = page.GetControl<ITextInputControl>("username");
usernameField.Enter("john.doe@example.com");  // Same everywhere
var text = usernameField.GetText();  // Same everywhere
```

### Example 2: Button Click

#### Before (MAUI)
```csharp
// MAUI/Appium-specific
var submitButton = page.GetControl<MauiButtonControl>("submit");
submitButton.TapAsync().Wait();  // MAUI async pattern
```

#### Before (WPF)
```csharp
// WPF/FlaUI-specific
var submitButton = page.GetControl<WpfButtonControl>("submit");
submitButton.Invoke();  // WPF pattern
```

#### Before (Html/Selenium)
```csharp
// Selenium-specific
var submitButton = page.GetControl<HtmlButtonControl>("submit");
submitButton.Click();  // Selenium method
```

#### After (All Platforms)
```csharp
// UNIFIED - Same code works everywhere
var submitButton = page.GetControl<IClickable>("submit");
submitButton.Click();  // Identical everywhere
```

### Example 3: Dropdown Selection

#### Before (WPF)
```csharp
var dropdown = page.GetControl<WpfComboBoxControl>("country");
dropdown.SelectedItem = "USA";  // WPF property
dropdown.Select("USA");  // Different method
```

#### Before (MAUI)
```csharp
var dropdown = page.GetControl<MauiPickerControl>("country");
dropdown.SelectItemAsync("USA").Wait();  // Async pattern
```

#### Before (Html/Selenium)
```csharp
var dropdown = page.GetControl<HtmlSelectControl>("country");
var selectElement = new SelectElement(dropdown.Element);
selectElement.SelectByText("USA");  // SelectElement wrapper pattern
```

#### After (All Platforms)
```csharp
var dropdown = page.GetControl<ISingleSelectControl>("country");
dropdown.SelectByText("USA");  // Identical everywhere
dropdown.AssertSelectedEquals("USA");  // Unified assertion
```

### Example 4: Keyboard Operations

#### Before (WPF)
```csharp
var textField = page.GetControl<WpfTextControl>("notes");
textField.SetText("original");
// WPF keyboard operations are complex
Keyboard.Type("test");  // Global keyboard
Keyboard.TypeVirtualKeyCode(VirtualKeyCode.ControlC);  // Copy equivalent
```

#### Before (Html/Selenium)
```csharp
var textField = page.GetControl<HtmlInputControl>("notes");
textField.SendKeys("original");
textField.SendKeys(Keys.Control + "c");  // Copy with Keys enum
// Mix of different styles
```

#### After (All Platforms)
```csharp
var textField = page.GetControl<IEditableTextControl>("notes");
textField.Enter("original");
textField.SelectAll();    // Ctrl+A
textField.Copy();         // Ctrl+C
textField.Paste();        // Ctrl+V
textField.Cut();          // Ctrl+X
textField.Undo();         // Ctrl+Z
textField.Redo();         // Ctrl+Y
// Clean, consistent, platform-agnostic
```

### Example 5: Collection/List Operations

#### Before (WPF)
```csharp
var listBox = page.GetControl<WpfListBoxControl>("items");
listBox.ItemsControl.Items.Count;  // Property access
listBox.Select(0);  // Index-based selection
```

#### Before (Html/Selenium)
```csharp
var list = page.GetControl<HtmlListControl>("items");
var items = driver.FindElements(By.CssSelector("#items li"));
items.Count;  // Manual element collection
var targetItem = items.FirstOrDefault(el => el.Text == "Item 1");
targetItem.Click();  // Manual search and click
```

#### After (All Platforms)
```csharp
var list = page.GetControl<IScrollableCollectionControl>("items");
list.GetItemCount();           // Unified method
list.ClickItem("Item 1");      // Search and click in one call
list.ScrollToItem("Item 50");  // Scroll support built-in
list.IsItemVisible("Item 50"); // Visibility check
list.AssertItemCount(100);     // Unified assertion
```

### Example 6: Complex Form Fill

#### Before (Mixed Platform Code)

```csharp
// Complex test mixing platform-specific approaches
public void FillComplexForm_WpfVersion(WpfPageObject page)
{
    // Text inputs (WPF style)
    var nameField = page.GetControl<WpfTextControl>("fullName");
    nameField.SetText("John Doe");

    var emailField = page.GetControl<WpfTextControl>("email");
    emailField.SetText("john@example.com");

    // Dropdown (WPF style)
    var countryDropdown = page.GetControl<WpfComboBoxControl>("country");
    countryDropdown.SelectedItem = "USA";

    // Checkbox (WPF style)
    var agreeCheckbox = page.GetControl<WpfCheckBoxControl>("agreedTerms");
    agreeCheckbox.IsChecked = true;

    // Button (WPF style)
    var submitButton = page.GetControl<WpfButtonControl>("submit");
    submitButton.Invoke();
}

// DIFFERENT test for Html/Selenium
public void FillComplexForm_SeleniumVersion(SeleniumPageObject page)
{
    // Text inputs (Selenium style)
    var nameField = page.GetControl<HtmlInputControl>("fullName");
    nameField.Clear();
    nameField.SendKeys("John Doe");

    var emailField = page.GetControl<HtmlInputControl>("email");
    emailField.Clear();
    emailField.SendKeys("john@example.com");

    // Dropdown (Selenium style)
    var countryDropdown = page.GetControl<HtmlSelectControl>("country");
    var select = new SelectElement(countryDropdown.Element);
    select.SelectByText("USA");

    // Checkbox (Selenium style)
    var agreeCheckbox = page.GetControl<HtmlCheckboxControl>("agreedTerms");
    if (!agreeCheckbox.Selected)
        agreeCheckbox.Click();

    // Button (Selenium style)
    var submitButton = page.GetControl<HtmlButtonControl>("submit");
    submitButton.Click();
}

// STILL DIFFERENT for MAUI
public async Task FillComplexForm_MauiVersion(MauiPageObject page)
{
    // Everything is async-based
    var nameField = page.GetControl<MauiEntryControl>("fullName");
    await nameField.EnterAsync("John Doe");

    var emailField = page.GetControl<MauiEntryControl>("email");
    await emailField.EnterAsync("john@example.com");

    // Different dropdown approach
    var countryPicker = page.GetControl<MauiPickerControl>("country");
    await countryPicker.SelectAsync("USA");

    // Different checkbox approach
    var agreeSwitch = page.GetControl<MauiSwitchControl>("agreedTerms");
    await agreeSwitch.SetOnAsync();

    // Different button approach
    var submitButton = page.GetControl<MauiButtonControl>("submit");
    await submitButton.TapAsync();
}
```

#### After (UNIFIED - Works Everywhere)

```csharp
// ONE test code for ALL 6 platforms!
public void FillComplexForm(IPageObject page)
{
    // Text inputs (Unified)
    page.GetControl<ITextInputControl>("fullName").Enter("John Doe");
    page.GetControl<ITextInputControl>("email").Enter("john@example.com");

    // Dropdown (Unified)
    page.GetControl<ISingleSelectControl>("country").SelectByText("USA");

    // Checkbox (Unified)
    page.GetControl<ICheckableControl>("agreedTerms").Check();

    // Button (Unified)
    page.GetControl<IClickable>("submit").Click();
}

// Call with ANY page implementation:
// - FillComplexForm(new WpfPageObject(...));       ✅ Works
// - FillComplexForm(new MauiPageObject(...));      ✅ Works
// - FillComplexForm(new SeleniumPageObject(...));  ✅ Works
// - FillComplexForm(new PlaywrightPageObject(...));✅ Works
// - FillComplexForm(new WinFormsPageObject(...));  ✅ Works
// - FillComplexForm(new StridePageObject(...));    ✅ Works
```

**Result:** 
- **Before:** 6 separate test implementations (one per platform)
- **After:** 1 test implementation (works on all platforms)
- **Savings:** 5 less files to maintain, 5 less bugs to fix

---

## 3. Migration Checklist

### Phase 1: Understanding (15 minutes)

- [ ] Read [16-interface-usage-guide.md](16-interface-usage-guide.md) - understand the 49 interfaces
- [ ] Review [17-platform-specific-implementation-guides.md](17-platform-specific-implementation-guides.md) - see how platforms implement them
- [ ] Run existing tests to baseline current behavior

### Phase 2: Simple Migration (per test, 10-30 minutes)

For each test file:

#### Step 1: Identify Platform-Specific Code
```csharp
// OLD CODE (before)
var button = page.GetControl<WpfButtonControl>("submit");

// What to look for:
// - Platform-specific control types (WpfButtonControl, MauiButtonControl, etc.)
// - Platform-specific method names (Invoke, TapAsync, SelectedItem, etc.)
```

#### Step 2: Replace with Interface
```csharp
// NEW CODE (after)
var button = page.GetControl<IClickable>("submit");
```

#### Step 3: Verify Methods Match
```csharp
// OLD: button.Invoke();
// NEW: button.Click();  // Check interface method signature
```

#### Step 4: Update Method Calls
```csharp
// Before
button.Invoke();
button.DoubleInvoke();

// After
button.Click();
button.DoubleClick();
```

#### Step 5: Run Tests
```bash
# Test should still pass on current platform
dotnet test
```

### Phase 3: Assertion Migration (10-20 minutes per test)

Convert platform-specific assertions to unified assertions:

```csharp
// Before (WPF-specific)
Assert.Equal("Expected", textField.Value);

// After (Unified)
textField.AssertTextEquals("Expected");

// Before (Selenium-specific)
Assert.True(element.Displayed);

// After (Unified)
var control = page.GetControl<IVisualElement>("element");
control.AssertVisible();
```

### Phase 4: Remove Platform Conditionals (10-20 minutes per test)

```csharp
// Before - lots of platform checks
if (page is WpfPageObject wpfPage)
{
    wpfPage.GetControl<WpfButtonControl>("btn").Invoke();
}
else if (page is SeleniumPageObject seleniumPage)
{
    seleniumPage.GetControl<HtmlButtonControl>("btn").Click();
}

// After - no platform checks needed
page.GetControl<IClickable>("btn").Click();
```

### Phase 5: Cross-Platform Testing (5-30 minutes per test)

Run migrated tests on other platforms:

```csharp
// Previously WPF-only test
[Fact]
public void LoginTest()
{
    // Now run on:
    // ✅ WPF         (original platform)
    // ✅ MAUI        (new platform)
    // ✅ Html/Selenium (new platform)
    // ✅ Html.Playwright (new platform)
    // ✅ WinForms    (new platform)
    // ✅ Stride      (new platform)
}
```

---

## 4. Common Migration Scenarios

### Scenario 1: Text Input Tests

```csharp
// BEFORE (WPF-specific)
public void TestPasswordReset_Wpf(WpfPageObject page)
{
    var emailField = page.GetControl<WpfTextControl>("email");
    emailField.SetText("user@example.com");
    emailField.SetText("user@example.com");  // Value property
    
    var newPasswordField = page.GetControl<WpfPasswordBoxControl>("newPassword");
    newPasswordField.SetPassword("NewPass123!");
    
    var confirmField = page.GetControl<WpfPasswordBoxControl>("confirmPassword");
    confirmField.SetPassword("NewPass123!");
    
    Assert.Equal("NewPass123!", newPasswordField.GetPassword());
}

// AFTER (Unified - works on all platforms)
public void TestPasswordReset(IPageObject page)
{
    var emailField = page.GetControl<ITextInputControl>("email");
    emailField.Enter("user@example.com");
    
    var newPasswordField = page.GetControl<ITextInputControl>("newPassword");
    newPasswordField.Enter("NewPass123!");
    
    var confirmField = page.GetControl<ITextInputControl>("confirmPassword");
    confirmField.Enter("NewPass123!");
    
    newPasswordField.AssertTextEquals("NewPass123!");
}
```

### Scenario 2: Dropdown/Selection Tests

```csharp
// BEFORE (Multiple platform versions)
public void TestCountrySelection_Wpf(WpfPageObject page)
{
    var dropdown = page.GetControl<WpfComboBoxControl>("country");
    dropdown.SelectedValue = "US";  // Property assignment
    Assert.Equal("US", dropdown.SelectedValue);
}

public void TestCountrySelection_Selenium(SeleniumPageObject page)
{
    var dropdown = page.GetControl<HtmlSelectControl>("country");
    var select = new SelectElement(dropdown.Element);
    select.SelectByValue("US");
    Assert.Equal("US", select.SelectedOption.GetAttribute("value"));
}

// AFTER (Single implementation)
public void TestCountrySelection(IPageObject page)
{
    var dropdown = page.GetControl<ISingleSelectControl>("country");
    dropdown.SelectByValue("US");
    dropdown.AssertSelectedEquals("US");  // Works on all platforms
}
```

### Scenario 3: List/Table Tests

```csharp
// BEFORE (Complex Selenium code)
public void TestUserTableSearch_Selenium(SeleniumPageObject page)
{
    // Manual element finding
    var rows = page.Driver.FindElements(By.CssSelector("#userTable tbody tr"));
    var targetRow = rows.FirstOrDefault(row => 
        row.FindElement(By.CssSelector("td:nth-child(2)")).Text == "John Doe");
    
    if (targetRow != null)
    {
        targetRow.FindElement(By.CssSelector("button.edit")).Click();
    }

    // Manual scroll if needed
    ((IJavaScriptExecutor)page.Driver).ExecuteScript(
        "arguments[0].scrollIntoView(true);", targetRow);
}

// AFTER (Clean interface usage)
public void TestUserTableSearch(IPageObject page)
{
    var userTable = page.GetControl<IScrollableCollectionControl>("userTable");
    userTable.ScrollToItem("John Doe");
    userTable.ClickItem("John Doe");
}
```

### Scenario 4: Checkbox/Toggle Tests

```csharp
// BEFORE (Inconsistent across platforms)
public void TestTermsAgreement_Wpf(WpfPageObject page)
{
    var checkbox = page.GetControl<WpfCheckBoxControl>("terms");
    checkbox.IsChecked = true;
    Assert.True(checkbox.IsChecked);
}

public void TestTermsAgreement_Selenium(SeleniumPageObject page)
{
    var checkbox = page.GetControl<HtmlCheckboxControl>("terms");
    if (!checkbox.Selected)
        checkbox.Click();
    Assert.True(checkbox.Selected);
}

// AFTER (Unified)
public void TestTermsAgreement(IPageObject page)
{
    var checkbox = page.GetControl<ICheckableControl>("terms");
    checkbox.Check();
    checkbox.AssertChecked();
}
```

### Scenario 5: Slider/Range Tests

```csharp
// BEFORE (Platform-specific value setting)
public void TestVolumeControl_Wpf(WpfPageObject page)
{
    var slider = page.GetControl<WpfSliderControl>("volume");
    slider.Value = 75;  // Direct value property
    Assert.Equal(75, slider.Value);
}

public void TestVolumeControl_Selenium(SeleniumPageObject page)
{
    var slider = page.GetControl<HtmlRangeControl>("volume");
    slider.SendKeys("75");  // Keyboard input
    Assert.Equal("75", slider.GetAttribute("value"));
}

// AFTER (Unified)
public void TestVolumeControl(IPageObject page)
{
    var slider = page.GetControl<IRangeInputControl>("volume");
    slider.SetValue(75);
    slider.AssertValueEquals(75);
}
```

---

## 5. Breaking Changes & Deprecated Patterns

### Removed Platform-Specific Types

```csharp
// DEPRECATED - No longer use these types:
// ❌ WpfButtonControl
// ❌ WpfTextControl
// ❌ WpfCheckBoxControl
// ❌ WpfComboBoxControl
// ❌ MauiButtonControl
// ❌ MauiEntryControl
// ❌ HtmlButtonControl
// ❌ HtmlInputControl
// ❌ HtmlSelectControl
// etc.

// REPLACE WITH - Interface types:
// ✅ IClickable
// ✅ ITextInputControl
// ✅ ICheckableControl
// ✅ ISingleSelectControl
// ✅ IRangeInputControl
// etc.
```

### Removed Platform-Specific Methods

```csharp
// DEPRECATED methods are replaced:

// WPF-specific
❌ button.Invoke();               → ✅ button.Click();
❌ textBox.SetText(value);        → ✅ textBox.Enter(value);
❌ checkbox.IsChecked = true;     → ✅ checkbox.Check();
❌ dropdown.SelectedItem = value; → ✅ dropdown.SelectByText(value);

// Selenium-specific
❌ element.SendKeys("text");      → ✅ element.Enter("text");
❌ element.Clear();               → ✅ element.Clear(); // Same!
❌ element.Click();               → ✅ element.Click();  // Same!
// (Some Selenium methods are identical, so migration is easy)

// MAUI-specific
❌ button.TapAsync().Wait();      → ✅ button.Click();
❌ entry.TextAsync = "value";     → ✅ entry.Enter("value");
```

### Property-Based Access Removed

```csharp
// DEPRECATED - Property-based access
❌ button.IsEnabled;
❌ textField.Value;
❌ checkbox.IsChecked;
❌ dropdown.SelectedItem;

// REPLACED WITH - Method-based access (more consistent)
✅ control.IsEnabled();
✅ textField.GetText();
✅ checkbox.IsChecked();
✅ dropdown.GetSelectedText();
```

### Async/Await Patterns Abstracted

```csharp
// DEPRECATED - Raw async code in tests
❌ await button.ClickAsync();
❌ var result = await textField.GetTextAsync();

// REPLACED WITH - Synchronous wrapper methods
✅ button.Click();  // Internally uses .Wait() on async
✅ var result = textField.GetText();  // Abstracted async

// This allows:
// - MAUI tests to use async framework
// - Playwright tests to use async framework
// - Other platforms to use sync framework
// WITHOUT requiring test writers to manage async/await
```

### Conditional Platform Code Discouraged

```csharp
// DEPRECATED - Lots of platform checks
❌ if (page is WpfPageObject wpf)
   {
       wpf.GetControl<WpfButtonControl>("btn").Invoke();
   }
   else if (page is MauiPageObject maui)
   {
       maui.GetControl<MauiButtonControl>("btn").TapAsync().Wait();
   }

// REPLACED WITH - Single unified code path
✅ page.GetControl<IClickable>("btn").Click();

// Optional: platform-specific features only when needed
if (page is INavigableControl nav)
{
    nav.Goto("https://example.com");  // Web-specific
}
```

---

## 6. Backward Compatibility

### Old Code Still Works (During Transition)

```csharp
// Your old WPF tests don't break immediately
var button = page.GetControl<WpfButtonControl>("submit");
button.Invoke();

// ✅ Still compiles
// ✅ Still runs
// ⚠️  But should be migrated to interfaces
```

### Phased Migration Approach

**Option 1: Migrate All at Once** (Recommended for small codebases)
```
Week 1: Migrate all tests to use interfaces
Result: 100% platform-independent code
```

**Option 2: Gradual Migration** (For large codebases)
```
Week 1:  Migrate high-priority tests to interfaces
Week 2:  Migrate medium-priority tests
Week 3:  Migrate remaining tests
Result: Gradually approach 100% platform independence
```

**Option 3: Mixed Approach** (During heavy development)
```
// Old code (still works)
var button = page.GetControl<WpfButtonControl>("submit");
button.Invoke();

// New code (uses interfaces)
var textField = page.GetControl<ITextInputControl>("name");
textField.Enter("value");

// Both work together during transition
// Migration can happen test-by-test
```

---

## 7. FAQ for Test Writers

### Q: Do I need to rewrite all my existing tests?

**A:** No, but you should gradually migrate them for these benefits:

- Write once, run on 6 platforms (massive time savings)
- Cleaner, more maintainable code
- Better IDE IntelliSense and refactoring support
- Future-proof for new platforms

For new tests, always use interfaces.

### Q: What if my platform has unique features?

**A:** Platform-specific features are still available, but through conditional code:

```csharp
// Generic test (all platforms)
page.GetControl<IClickable>("button").Click();

// Platform-specific feature (web only)
if (page is INavigableControl nav)
{
    nav.Goto("https://example.com");
}

// Platform-specific feature (desktop only)
if (page is WindowObject window)
{
    window.SetSize(1920, 1080);
}
```

### Q: How do I debug issues across platforms?

**A:** Start with interface-based tests that work on all platforms, then add platform-specific diagnostics:

```csharp
[Theory]
[MemberData(nameof(GetTestPlatforms))]
public void LoginTest(IPageObject page, string platformName)
{
    try
    {
        var username = page.GetControl<ITextInputControl>("username");
        username.Enter("testuser");
        
        var button = page.GetControl<IClickable>("login");
        button.Click();
    }
    catch (Exception ex)
    {
        _logger.LogError($"Test failed on {platformName}: {ex.Message}");
        throw;
    }
}

public static IEnumerable<object[]> GetTestPlatforms() => new[]
{
    new object[] { new WpfPageObject(...), "WPF" },
    new object[] { new MauiPageObject(...), "MAUI" },
    new object[] { new SeleniumPageObject(...), "Selenium" },
    // ... etc
};
```

### Q: What if a test only works on one platform?

**A:** That's fine - just skip the test on other platforms:

```csharp
[Fact(Skip = "Only works on MAUI platform")]
public void MauiSpecificGestureTest()
{
    var control = page.GetControl<IClickable>("element");
    control.DoubleClick();  // Might not work as expected on all platforms
}

// Or use platform detection:
[Fact]
public void TestMaySkip()
{
    if (!(page is MauiPageObject))
        return;  // Skip on non-MAUI platforms

    // MAUI-specific test code
}
```

### Q: How do I handle platform-specific timeouts?

**A:** Use platform awareness for timeout values:

```csharp
public class CrossPlatformTest
{
    private readonly Dictionary<Type, int> _timeoutsByPlatform = new()
    {
        { typeof(MauiTestContext), 10000 },      // Mobile is slower
        { typeof(FlaUITestContext), 3000 },      // Desktop is fast
        { typeof(SeleniumTestContext), 5000 },   // Web varies
        { typeof(StrideTestContext), 15000 }     // Game engine is slower
    };

    [Theory]
    [MemberData(nameof(GetTestContexts))]
    public void TestWithPlatformAwareTimeout(ITestContext context)
    {
        var timeout = _timeoutsByPlatform[context.GetType()];
        var control = context.GetPage().GetControl<IInteractive>("element");
        control.WaitVisible(true, timeout);
    }
}
```

### Q: Can I still use platform-specific assertions?

**A:** Yes, but prefer unified assertions:

```csharp
// Old style (platform-specific)
var wpfControl = page.GetControl<WpfTextControl>("name");
Assert.Equal("John", wpfControl.Value);

// New style (unified, works on all platforms)
var control = page.GetControl<ITextInputControl>("name");
control.AssertTextEquals("John");

// Both work, but unified is better for cross-platform tests
```

### Q: What happens to existing test data?

**A:** No changes needed - test data stays the same. Only test code changes:

```csharp
// Test data (unchanged)
public class LoginCredentials
{
    public string Username { get; set; }
    public string Password { get; set; }
}

// Test code (migrated to interfaces)
[Theory]
[MemberData(nameof(GetTestCredentials))]
public void LoginTest(LoginCredentials creds, IPageObject page)
{
    var username = page.GetControl<ITextInputControl>("username");
    username.Enter(creds.Username);
    
    var password = page.GetControl<ITextInputControl>("password");
    password.Enter(creds.Password);

    page.GetControl<IClickable>("login").Click();
}
```

### Q: How do I migrate large test suites efficiently?

**A:** Use these strategies:

1. **Start with base classes/helper methods:**
   ```csharp
   // Before: each test method has platform-specific code
   // After: one helper method with unified code
   
   // Helper method (unified once)
   private void LoginUser(IPageObject page, string username, string password)
   {
       page.GetControl<ITextInputControl>("username").Enter(username);
       page.GetControl<ITextInputControl>("password").Enter(password);
       page.GetControl<IClickable>("login").Click();
   }
   
   // All test methods now use helper
   [Fact]
   public void TestValidLogin() => LoginUser(_page, "user1", "pass1");
   
   [Fact]
   public void TestInvalidLogin() => LoginUser(_page, "user1", "wrong");
   ```

2. **Migrate by test class, not individual methods**
3. **Create shared test utilities for common operations**
4. **Use interface extractors for common patterns:**
   ```csharp
   public static class PageObjectExtensions
   {
       public static void FillLoginForm(this IPageObject page, string username, string password)
       {
           page.GetControl<ITextInputControl>("username").Enter(username);
           page.GetControl<ITextInputControl>("password").Enter(password);
       }
   }
   ```

### Q: How do I verify my migration was successful?

**A:** Test checklist:

```csharp
// ✅ Code compiles without errors
// ✅ All tests pass on original platform
// ✅ Tests pass on at least one other platform
// ✅ No platform-specific type references remain
// ✅ Methods use unified names (Click, Enter, etc.)
// ✅ Assertions use unified methods (AssertTextEquals, etc.)
// ✅ No conditional platform logic except for features

// Run tests across platforms:
public static IEnumerable<IPageObject> GetAllTestPlatforms() => new[]
{
    new WpfPageObject(...),
    new MauiPageObject(...),
    new SeleniumPageObject(...),
    new PlaywrightPageObject(...),
    new WinFormsPageObject(...),
    new StridePageObject(...)
};

// Verify same test runs on all
[Theory]
[MemberData(nameof(GetAllTestPlatforms))]
public void TestRunsOnAllPlatforms(IPageObject page)
{
    // Single test implementation
    page.GetControl<IClickable>("button").Click();
    // ✅ Works on all 6 platforms
}
```

---

## Next Steps

1. **Read** [16-interface-usage-guide.md](16-interface-usage-guide.md) for interface details
2. **Review** [17-platform-specific-implementation-guides.md](17-platform-specific-implementation-guides.md) for platform examples
3. **Start** migrating one test class at a time
4. **Validate** tests pass on original platform
5. **Test** on other platforms to verify cross-platform compatibility

---

**Migration Difficulty by Control Type:**

| Control Type | Difficulty | Est. Time per Test |
|---|---|---|
| Button (IClickable) | Very Easy | 2-5 min |
| Text Input (ITextInputControl) | Easy | 5-10 min |
| Dropdown (ISingleSelectControl) | Easy | 5-10 min |
| Checkbox (ICheckableControl) | Easy | 5 min |
| Slider (IRangeInputControl) | Medium | 10-15 min |
| List (ICollectionControl) | Medium | 15-20 min |
| Complex Form | Hard | 30-60 min |

**Estimated Total Migration Effort:**
- Small project (< 50 tests): 4-8 hours
- Medium project (50-200 tests): 1-2 weeks
- Large project (> 200 tests): 2-4 weeks

---

**Document Status:** Complete for Phase 1, Task 5, Step 6  
**Last Modified:** January 3, 2026  
**Next Document:** [19-phase-1-task-5-completion-summary.md](19-phase-1-task-5-completion-summary.md)
