# Platform-Specific Implementation Guides

**Document Version:** 1.0  
**Status:** Phase 1, Task 5, Step 6 - Documentation  
**Last Updated:** January 3, 2026  

---

## Table of Contents

1. [MAUI (Appium)](#1-maui-appium)
2. [WPF/WinForms (FlaUI)](#2-wpfwinforms-flui)
3. [Html/Selenium](#3-htmlselenium)
4. [Html.Playwright](#4-htmlplaywright)
5. [Stride (Game Engine)](#5-stride-game-engine)
6. [Cross-Platform Patterns](#6-cross-platform-patterns)

---

## 1. MAUI (Appium)

### Overview

MAUI platform uses Appium framework for mobile and desktop automation.

**Package:** Brinell.Maui  
**Base Framework:** Appium  
**Supported Platforms:** iOS, Android, macOS, Windows  
**Controls:** 27 (all use enhanced base classes)

### Enhanced Base Classes

```csharp
// Location: src/Brinell.Maui/Controls/Base/

EnhancedControlBase              // IVisualElement, IInteractive, IClickable
EnhancedTextControlBase          // ITextInputControl, IEditableTextControl
EnhancedSelectorControlBase      // ISingleSelectControl, ISelectableControl
EnhancedToggleControlBase        // IToggleControl, ICheckableControl
EnhancedRangeControlBase         // IRangeInputControl, ISliderControl
EnhancedItemsControlBase         // ICollectionControl, IClickableCollectionControl, IScrollableCollectionControl
EnhancedContentControlBase       // IContainerControl, IScrollableControl
EnhancedPageBase                 // INavigableControl, IContainerControl
```

### Keyboard Operations (MAUI)

```csharp
// Standard keys
SendKeys(Key.Enter);
SendKeys(Key.Tab);
SendKeys(Key.Escape);
SendKeys(Key.Backspace);
SendKeys(Key.Delete);

// Modifier combinations
SendKeys(Keys.Control + "c");    // Ctrl+C (Copy)
SendKeys(Keys.Control + "x");    // Ctrl+X (Cut)
SendKeys(Keys.Control + "v");    // Ctrl+V (Paste)
SendKeys(Keys.Control + "z");    // Ctrl+Z (Undo)
SendKeys(Keys.Control + "y");    // Ctrl+Y (Redo)
SendKeys(Keys.Control + "a");    // Ctrl+A (Select All)
```

### Gesture Operations (MAUI)

```csharp
// Click operations
element.Click();                  // Single tap
element.DoubleClick();            // Double tap
element.RightClick();             // Long press (context menu)

// Scroll operations
element.Swipe();                  // Swipe gesture
ScrollView.ScrollTo();            // Scroll to position
```

### Example: Login Form (MAUI)

```csharp
public class LoginPageMaui : IPageObject
{
    private readonly MauiTestContext _context;

    public LoginPageMaui(MauiTestContext context)
    {
        _context = context;
    }

    public void Login(string username, string password)
    {
        // Get controls using enhanced base classes
        var usernameField = _context.GetControl<EnhancedTextControlBase>("username");
        var passwordField = _context.GetControl<EnhancedTextControlBase>("password");
        var loginButton = _context.GetControl<EnhancedControlBase>("loginButton");

        // Use unified interface methods
        usernameField.Enter(username);
        passwordField.Enter(password);
        loginButton.Click();

        // Verify navigation
        var welcomeMessage = _context.GetControl<ITextInputControl>("welcome");
        welcomeMessage.WaitVisible(true, 5000);
    }

    public void LoginWithKeyboard(string username, string password)
    {
        var usernameField = _context.GetControl<EnhancedTextControlBase>("username");
        var passwordField = _context.GetControl<EnhancedTextControlBase>("password");

        // Use keyboard-specific operations
        usernameField.Enter(username);
        usernameField.SelectAll();          // Ctrl+A
        var copied = usernameField.GetText();

        passwordField.Enter(password);
        passwordField.Copy();               // Ctrl+C
        passwordField.Paste();              // Ctrl+V
    }
}
```

### MAUI-Specific Control Features

```csharp
// Picker control (selection)
var picker = page.GetControl<EnhancedSelectorControlBase>("picker");
picker.SelectByText("Option 1");

// Slider control (range)
var slider = page.GetControl<EnhancedRangeControlBase>("volume");
slider.SetPercentage(75);
slider.Increment(5);

// CollectionView (scrollable list)
var listView = page.GetControl<EnhancedItemsControlBase>("items");
listView.ScrollToItem("Item 50");
listView.ClickItem("Item 50");

// Switch control (toggle)
var switchControl = page.GetControl<EnhancedToggleControlBase>("enableNotifications");
switchControl.SetOn();
```

### Common MAUI Test Patterns

```csharp
// Pattern 1: Wait for loading
var control = page.GetControl<IInteractive>("element");
control.WaitVisible(true, 10000);  // Wait up to 10 seconds

// Pattern 2: Gesture-based interaction
var button = page.GetControl<IClickable>("actionButton");
button.DoubleClick();  // Double tap on iOS/Android

// Pattern 3: Keyboard dismissal
var textField = page.GetControl<IInteractive>("textInput");
textField.Focus();
textField.Blur();  // Dismiss keyboard

// Pattern 4: Scroll within ScrollView
var scrollContainer = page.GetControl<EnhancedContentControlBase>("scrollView");
scrollContainer.ScrollBottom();
```

---

## 2. WPF/WinForms (FlaUI)

### Overview

WPF/WinForms platform uses FlaUI framework with Visual Studio UI Automation.

**Packages:**
- Brinell.Wpf (WPF controls)
- Brinell.WinForms (WinForms controls)

**Base Framework:** FlaUI (UI Automation)  
**Supported Platforms:** Windows desktop  
**Controls:** 29 total (13 WPF + 16 WinForms)

### Enhanced Base Classes (Shared)

```csharp
// Location: src/Brinell.FlaUI/Controls/Base/

EnhancedControlBase              // IVisualElement, IInteractive, IClickable
EnhancedTextControlBase          // ITextInputControl, IEditableTextControl
EnhancedSelectorControlBase      // ISingleSelectControl, ISelectableControl
EnhancedToggleControlBase        // IToggleControl, ICheckableControl
EnhancedRangeControlBase         // IRangeInputControl, ISliderControl
EnhancedItemsControlBase         // ICollectionControl, IClickableCollectionControl, IScrollableCollectionControl
EnhancedContentControlBase       // IContainerControl, IScrollableControl
```

### Keyboard Operations (FlaUI)

```csharp
// Using VirtualKeyShort enum
using FlaUI.Core;

// Standard keys
Keyboard.Type(VirtualKeyShort.RETURN);
Keyboard.Type(VirtualKeyShort.TAB);
Keyboard.Type(VirtualKeyShort.ESCAPE);

// Modifier combinations via extension methods
element.SendKeyboardInput(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_C);  // Ctrl+C
element.SendKeyboardInput(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_X);  // Ctrl+X
element.SendKeyboardInput(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);  // Ctrl+V
element.SendKeyboardInput(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_Z);  // Ctrl+Z
```

### Mouse Operations (FlaUI)

```csharp
// Click operations
element.Click();                  // Left click
element.DoubleClick();            // Double click
element.RightClick();             // Right click

// Focus operations
element.Focus();                  // Set keyboard focus
Keyboard.Type(VirtualKeyShort.TAB);  // Tab away (blur)

// Scroll operations
element.GetScrollPattern().Scroll(1, 10);  // Scroll down 10 units
```

### Example: WPF Data Entry Form

```csharp
public class DataEntryPageWpf : IPageObject
{
    private readonly FlaUITestContext _context;

    public DataEntryPageWpf(FlaUITestContext context)
    {
        _context = context;
    }

    public void EnterEmployeeData(string name, string email, string department)
    {
        // Get controls
        var nameField = _context.GetControl<EnhancedTextControlBase>("employeeName");
        var emailField = _context.GetControl<EnhancedTextControlBase>("emailAddress");
        var deptDropdown = _context.GetControl<EnhancedSelectorControlBase>("department");
        var saveButton = _context.GetControl<EnhancedControlBase>("saveButton");

        // Unified interface operations
        nameField.Enter(name);
        emailField.Enter(email);
        deptDropdown.SelectByText(department);
        saveButton.Click();
    }

    public void EditWithKeyboardShortcuts(string newValue)
    {
        var field = _context.GetControl<EnhancedTextControlBase>("dataField");

        // Use keyboard-based editing
        field.Focus();
        field.SelectAll();      // Ctrl+A
        field.Cut();            // Ctrl+X
        field.Enter(newValue);
        field.Copy();           // Ctrl+C
    }

    public void InteractWithDataGrid()
    {
        var grid = _context.GetControl<EnhancedItemsControlBase>("dataGrid");

        // Collection control operations
        int itemCount = grid.GetItemCount();
        grid.ClickItem("John Doe");
        grid.ScrollToItem("Last Employee");
    }
}
```

### WPF-Specific Control Examples

```csharp
// DataGrid
var dataGrid = page.GetControl<EnhancedItemsControlBase>("employees");
dataGrid.ClickItemAt(0);  // Click first row

// TreeView
var treeView = page.GetControl<EnhancedItemsControlBase>("folders");
treeView.ClickItem("Folder/SubFolder/Item");

// ComboBox
var comboBox = page.GetControl<EnhancedSelectorControlBase>("options");
comboBox.SelectByValue("VALUE_2");

// Slider
var slider = page.GetControl<EnhancedRangeControlBase>("quality");
slider.SetPercentage(50);

// CheckBox
var checkbox = page.GetControl<EnhancedToggleControlBase>("agreedTerms");
checkbox.Check();
```

### WinForms-Specific Control Examples

```csharp
// ListBox
var listBox = page.GetControl<EnhancedItemsControlBase>("items");
listBox.SelectByIndex(2);

// NumericUpDown
var numericInput = page.GetControl<EnhancedRangeControlBase>("quantity");
numericInput.SetValue(42);
numericInput.Increment(5);

// GroupBox
var groupBox = page.GetControl<EnhancedContentControlBase>("settings");
groupBox.GetChild<IClickable>("applyButton").Click();

// DataGridView
var dataGridView = page.GetControl<EnhancedItemsControlBase>("users");
dataGridView.ClickItem("admin@example.com");
```

### Common FlaUI Test Patterns

```csharp
// Pattern 1: Double-click action
var item = page.GetControl<IClickable>("itemToEdit");
item.DoubleClick();

// Pattern 2: Right-click context menu
var contextItem = page.GetControl<IClickable>("file");
contextItem.RightClick();
var deleteOption = page.GetControl<IClickable>("deleteOption");
deleteOption.Click();

// Pattern 3: Multi-select list
var multiSelectList = page.GetControl<EnhancedItemsControlBase>("items");
multiSelectList.ClickItem("Item 1");
multiSelectList.ClickItem("Item 2");  // Hold Ctrl for multi-select

// Pattern 4: Keyboard-driven navigation
var window = page as WindowObject;
var firstButton = window.GetControl<IClickable>("firstButton");
firstButton.Focus();
Keyboard.Type(VirtualKeyShort.RETURN);  // Activate button
```

---

## 3. Html/Selenium

### Overview

Html/Selenium platform uses Selenium WebDriver for web browser automation.

**Package:** Brinell.Html  
**Base Framework:** Selenium WebDriver  
**Supported Platforms:** Chrome, Firefox, Edge, Safari, etc.  
**Controls:** 13 (all use enhanced base classes)

### Enhanced Base Classes

```csharp
// Location: src/Brinell.Html/Controls/Base/

EnhancedControlBase              // IVisualElement, IInteractive, IClickable
EnhancedTextControlBase          // ITextInputControl, IEditableTextControl
EnhancedSelectorControlBase      // ISingleSelectControl, ISelectableControl
EnhancedToggleControlBase        // IToggleControl, ICheckableControl
EnhancedRangeControlBase         // IRangeInputControl, ISliderControl
EnhancedItemsControlBase         // ICollectionControl, IClickableCollectionControl, IScrollableCollectionControl
EnhancedContentControlBase       // IContainerControl, IScrollableControl
EnhancedPageBase                 // INavigableControl, IContainerControl
```

### Keyboard Operations (Selenium)

```csharp
// Standard keys
element.SendKeys(Keys.Return);
element.SendKeys(Keys.Tab);
element.SendKeys(Keys.Escape);

// Modifier combinations
element.SendKeys(Keys.Control + "c");  // Ctrl+C (Copy)
element.SendKeys(Keys.Control + "v");  // Ctrl+V (Paste)
element.SendKeys(Keys.Control + "x");  // Ctrl+X (Cut)
element.SendKeys(Keys.Control + "z");  // Ctrl+Z (Undo)
element.SendKeys(Keys.Control + "a");  // Ctrl+A (Select All)
```

### Mouse Operations (Selenium)

```csharp
var actions = new Actions(driver);

// Single click
actions.Click(element).Perform();

// Double click
actions.DoubleClick(element).Perform();

// Right click (context menu)
actions.ContextClick(element).Perform();

// Scroll to element
((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
```

### Example: E-Commerce Checkout (Html/Selenium)

```csharp
public class CheckoutPageSelenium : IPageObject
{
    private readonly SeleniumTestContext _context;
    private readonly IWebDriver _driver;

    public CheckoutPageSelenium(SeleniumTestContext context)
    {
        _context = context;
        _driver = context.Driver;
    }

    public void CheckoutWithCard(string cardNumber, string expiryDate, string cvv)
    {
        // Get controls using Selenium-backed enhanced base classes
        var cardField = _context.GetControl<EnhancedTextControlBase>("cardNumber");
        var expiryField = _context.GetControl<EnhancedTextControlBase>("expiryDate");
        var cvvField = _context.GetControl<EnhancedTextControlBase>("cvv");
        var submitButton = _context.GetControl<EnhancedControlBase>("submitPayment");

        // Use unified interface methods
        cardField.Enter(cardNumber);
        expiryField.Enter(expiryDate);
        cvvField.Enter(cvv);

        // Verify button is clickable
        submitButton.WaitClickable(true, 5000);
        submitButton.Click();
    }

    public void SelectShippingMethod(string method)
    {
        var shippingOptions = _context.GetControl<EnhancedItemsControlBase>("shippingMethods");

        // Collection operations work across all platforms
        var allMethods = shippingOptions.GetAllItems();
        Assert.Contains(method, allMethods);

        shippingOptions.ClickItem(method);
    }

    public void VerifyOrderSummary(string expectedTotal)
    {
        var orderTotal = _context.GetControl<ITextInputControl>("orderTotal");
        orderTotal.AssertTextEquals(expectedTotal);
    }
}
```

### Selenium-Specific Control Examples

```csharp
// HTML Select element
var dropdown = page.GetControl<EnhancedSelectorControlBase>("country");
dropdown.SelectByText("United States");
dropdown.SelectByValue("US");

// Input type="range"
var slider = page.GetControl<EnhancedRangeControlBase>("priceRange");
slider.SetPercentage(75);
slider.SetValue(750);

// Input type="checkbox"
var checkbox = page.GetControl<EnhancedToggleControlBase>("agreeTerms");
checkbox.Check();

// Input type="radio"
var radioGroup = page.GetControl<EnhancedToggleControlBase>("paymentMethod");
radioGroup.Check();  // Selects the radio button

// Table with rows
var dataTable = page.GetControl<EnhancedItemsControlBase>("userTable");
dataTable.ClickItem("admin@example.com");
dataTable.ScrollToItem("user100@example.com");
```

### JavaScript Execution Patterns (Selenium)

```csharp
// Execute JavaScript for advanced operations
var executor = _driver as IJavaScriptExecutor;

// Scroll to bottom of page
executor.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");

// Get element's computed style
var color = executor.ExecuteScript("return window.getComputedStyle(arguments[0]).color;", element);

// Click using JavaScript (sometimes more reliable)
executor.ExecuteScript("arguments[0].click();", element);

// Get element's visibility
var visible = executor.ExecuteScript("return arguments[0].offsetParent !== null;", element);
```

### Common Selenium Test Patterns

```csharp
// Pattern 1: Wait for element with explicit wait
var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
var element = wait.Until(ExpectedConditions.ElementToBeClickable(By.Id("button")));

// Pattern 2: Switch to iframe
_driver.SwitchTo().Frame("iframeName");
var controlInFrame = page.GetControl<IClickable>("elementInFrame");
_driver.SwitchTo().DefaultContent();

// Pattern 3: Handle dropdowns
var dropdown = page.GetControl<EnhancedSelectorControlBase>("selectElement");
dropdown.SelectByText("Option 1");

// Pattern 4: Multi-tab handling
var originalWindow = _driver.CurrentWindowHandle;
var windows = _driver.WindowHandles;
_driver.SwitchTo().Window(windows[1]);  // Switch to new tab
// ... perform actions ...
_driver.SwitchTo().Window(originalWindow);  // Switch back
```

---

## 4. Html.Playwright

### Overview

Html.Playwright platform uses Microsoft.Playwright for modern browser automation with native async support.

**Package:** Brinell.Html.Playwright  
**Base Framework:** Microsoft.Playwright  
**Supported Platforms:** Chromium, Firefox, WebKit (Safari)  
**Controls:** 15 (12 synchronous + 3 async variants)

### Enhanced Base Classes

```csharp
// Location: src/Brinell.Html.Playwright/Controls/Base/

EnhancedControlBase              // IVisualElement, IInteractive, IClickable
EnhancedTextControlBase          // ITextInputControl, IEditableTextControl
EnhancedSelectorControlBase      // ISingleSelectControl, ISelectableControl
EnhancedToggleControlBase        // IToggleControl, ICheckableControl
EnhancedRangeControlBase         // IRangeInputControl, ISliderControl
EnhancedItemsControlBase         // ICollectionControl, IClickableCollectionControl, IScrollableCollectionControl
EnhancedContentControlBase       // IContainerControl, IScrollableControl
EnhancedPageBase                 // INavigableControl, IContainerControl
```

### Async-to-Sync Wrapping

```csharp
// Playwright operations are async, but enhanced base classes wrap them
// The .Wait() extension method converts async tasks to sync operations

// Example: Playwright internally
await element.ClickAsync();
await element.TypeAsync("text");
await element.PressAsync("Control+C");

// Example: Enhanced base class wrapping
var control = page.GetControl<EnhancedControlBase>("button");
control.Click();      // Wraps ClickAsync() with .Wait()
control.Enter("text"); // Wraps TypeAsync() with .Wait()
control.Copy();       // Wraps PressAsync("Control+C") with .Wait()
```

### Keyboard Operations (Playwright)

```csharp
// Playwright uses "Control+C" string pattern
await element.PressAsync("Control+C");    // Copy
await element.PressAsync("Control+X");    // Cut
await element.PressAsync("Control+V");    // Paste
await element.PressAsync("Control+Z");    // Undo
await element.PressAsync("Control+A");    // Select All

// Standard keys
await element.PressAsync("Enter");
await element.PressAsync("Tab");
await element.PressAsync("Escape");
await element.PressAsync("Delete");
```

### JavaScript Operations (Playwright)

```csharp
// EvaluateAsync for JavaScript execution
var color = await element.EvaluateAsync<string>("el => window.getComputedStyle(el).color");

// Scroll operations
await element.EvaluateAsync("el => el.scrollTop = 0");  // Scroll to top
await element.EvaluateAsync("el => el.scrollTop += 100"); // Scroll down
await element.EvaluateAsync("el => el.scrollIntoView()");  // Scroll into view

// Get property values
var value = await element.EvaluateAsync<string>("el => el.value");
var count = await element.EvaluateAsync<int>("el => el.children.length");
```

### Example: SPA Navigation Test (Html.Playwright)

```csharp
public class SpaPagePlaywright : IPageObject
{
    private readonly PlaywrightTestContext _context;

    public SpaPagePlaywright(PlaywrightTestContext context)
    {
        _context = context;
    }

    public void NavigateAndSearch(string searchTerm)
    {
        // Navigation operations (web-specific)
        var page = _context as INavigableControl;
        page.Goto("https://app.example.com");

        // Search operations
        var searchBox = _context.GetControl<EnhancedTextControlBase>("searchInput");
        searchBox.Enter(searchTerm);

        var searchButton = _context.GetControl<EnhancedControlBase>("searchButton");
        searchButton.Click();

        // Wait for results
        var resultsList = _context.GetControl<EnhancedItemsControlBase>("results");
        resultsList.WaitExists(true, 5000);
    }

    public void VerifySearchResults(string[] expectedResults)
    {
        var resultsList = _context.GetControl<EnhancedItemsControlBase>("results");
        var actualResults = resultsList.GetAllItems();

        foreach (var expected in expectedResults)
        {
            Assert.Contains(expected, actualResults);
        }
    }

    public void InteractWithResults(string resultText)
    {
        var resultsList = _context.GetControl<EnhancedItemsControlBase>("results");

        // Scroll to ensure visibility
        resultsList.ScrollToItem(resultText);
        resultsList.ClickItem(resultText);

        // Verify navigation
        var page = _context as INavigableControl;
        page.WaitForNavigation();
    }
}
```

### Playwright-Specific Control Examples

```csharp
// Page navigation (web-specific)
var page = testContext as INavigableControl;
page.Goto("https://example.com");
page.GoBack();
page.GoForward();
string currentUrl = page.GetCurrentUrl();
string title = page.GetTitle();

// Complex interaction sequence
var input = page.GetControl<EnhancedTextControlBase>("username");
input.Focus();
input.Triple click();  // Select all via triple-click
input.Copy();
input.Paste();

// Evaluate JavaScript for advanced queries
var element = page.GetControl<EnhancedControlBase>("element");
// element.EvaluateAsync(...) available through IPage
```

### Common Playwright Test Patterns

```csharp
// Pattern 1: Wait for navigation
page.Goto("https://example.com/form");
var submitButton = page.GetControl<IClickable>("submit");
submitButton.Click();
// Wait for next page load
page.WaitForNavigation();

// Pattern 2: Evaluate JavaScript
var jsResult = page.GetControl<EnhancedControlBase>("elem")
    .EvaluateAsync("el => el.getAttribute('data-value')");

// Pattern 3: Multiple page/context
var context = await playwright.Chromium.LaunchPersistentContextAsync(userDataDir);
var page1 = context.Pages[0];
var page2 = await context.NewPageAsync();
// Use page1 and page2 independently

// Pattern 4: Capture screenshot
await page.ScreenshotAsync(new PageScreenshotOptions 
{ 
    Path = "screenshot.png" 
});
```

---

## 5. Stride (Game Engine)

### Overview

Stride is a game engine with network-based UI automation capabilities.

**Package:** Brinell.Stride  
**Base Framework:** Stride GameEngine + Network Communication  
**Supported Platforms:** Windows (game client)  
**Controls:** 11 (all use enhanced base classes)

### Enhanced Base Classes

```csharp
// Location: src/Brinell.Stride/Controls/Base/

EnhancedStrideControlBase              // IVisualElement, IInteractive, IClickable
EnhancedStrideTextControlBase          // ITextInputControl, IEditableTextControl
EnhancedStrideSelectorControlBase      // ISingleSelectControl, ISelectableControl
EnhancedStrideToggleControlBase        // IToggleControl, ICheckableControl
EnhancedStrideRangeControlBase         // IRangeInputControl, ISliderControl
EnhancedStrideContentControlBase       // IContainerControl, IScrollableControl
```

### Network-Based Operations

```csharp
// Stride uses StrideTestContext for network communication
// All operations are delegated to the game engine via network calls

var context = new StrideTestContext("localhost", 5000);

// Get element state via network
var button = context.GetControl<EnhancedStrideControlBase>("gameButton");
button.Click();  // Sends network command to game engine

// Element operations are implemented as network commands:
// - LeftClick() -> Network message to game
// - GetState() -> Network query for element state
// - SetFocus() -> Network command to focus element
```

### Example: Game UI Testing (Stride)

```csharp
public class GameMenuPageStride : IPageObject
{
    private readonly StrideTestContext _context;

    public GameMenuPageStride(StrideTestContext context)
    {
        _context = context;
    }

    public void SelectGameDifficulty(string difficulty)
    {
        // Game menu selection
        var difficultySelector = _context.GetControl<EnhancedStrideSelectorControlBase>("difficulty");
        difficultySelector.SelectByText(difficulty);

        var confirmButton = _context.GetControl<EnhancedStrideControlBase>("confirm");
        confirmButton.Click();
    }

    public void AdjustGameSettings(int volume, int brightness)
    {
        // Volume slider
        var volumeSlider = _context.GetControl<EnhancedStrideRangeControlBase>("volume");
        volumeSlider.SetPercentage(volume);

        // Brightness slider
        var brightnessSlider = _context.GetControl<EnhancedStrideRangeControlBase>("brightness");
        brightnessSlider.SetPercentage(brightness);

        // Save settings
        var saveButton = _context.GetControl<EnhancedStrideControlBase>("saveSettings");
        saveButton.Click();
    }

    public void VerifyGameStarted()
    {
        var startButton = _context.GetControl<EnhancedStrideControlBase>("startGame");
        startButton.WaitClickable(true, 10000);
        startButton.Click();

        // Wait for game scene to load
        var gameScene = _context.GetControl<EnhancedStrideControlBase>("gameView");
        gameScene.WaitVisible(true, 30000);
    }
}
```

### Stride-Specific Control Examples

```csharp
// Button control
var button = page.GetControl<EnhancedStrideControlBase>("playButton");
button.Click();
button.WaitClickable(true, 5000);

// Text input (on-screen keyboard simulation)
var textField = page.GetControl<EnhancedStrideTextControlBase>("playerName");
textField.Enter("PlayerOne");
textField.Clear();

// Selection menu
var menu = page.GetControl<EnhancedStrideSelectorControlBase>("mainMenu");
menu.SelectByText("Settings");

// Toggle/checkbox
var soundToggle = page.GetControl<EnhancedStrideToggleControlBase>("soundEnabled");
soundToggle.SetOn();

// Slider
var speedSlider = page.GetControl<EnhancedStrideRangeControlBase>("gameSpeed");
speedSlider.SetPercentage(100);
```

### Game Engine Communication Pattern

```csharp
// Network message flow:
// 1. Test calls: button.Click()
// 2. Enhanced base class creates network message: "LeftClick" for button ID
// 3. Message sent to game engine via network socket
// 4. Game engine receives and executes UI action
// 5. Test receives acknowledgment or state update

// Log action for debugging
context.LogAction("USER_CLICKED_PLAY_BUTTON");
```

### Common Stride Test Patterns

```csharp
// Pattern 1: Wait for network response
var control = page.GetControl<EnhancedStrideControlBase>("loadingIndicator");
control.WaitVisible(false, 30000);  // Wait for loading to finish

// Pattern 2: Menu navigation
var mainMenu = page.GetControl<EnhancedStrideSelectorControlBase>("menu");
mainMenu.SelectByIndex(0);  // Select first menu item

// Pattern 3: Verify game state
var healthBar = page.GetControl<EnhancedStrideRangeControlBase>("health");
var health = healthBar.GetValue();
Assert.True(health > 0);

// Pattern 4: Complex game flow
var settingsMenu = page.GetControl<EnhancedStrideControlBase>("settingsButton");
settingsMenu.Click();
var audioTab = page.GetControl<EnhancedStrideControlBase>("audioTab");
audioTab.Click();
var volumeSlider = page.GetControl<EnhancedStrideRangeControlBase>("volume");
volumeSlider.SetPercentage(50);
```

---

## 6. Cross-Platform Patterns

### Pattern 1: Platform-Agnostic Test Code

```csharp
// This test code works IDENTICALLY on all 6 platforms
public class AuthenticationTests
{
    private readonly IPageObject _page;

    public AuthenticationTests(IPageObject page)
    {
        _page = page;
    }

    [Fact]
    public void LoginWithValidCredentials()
    {
        // Interfaces don't reveal platform
        var username = _page.GetControl<ITextInputControl>("username");
        var password = _page.GetControl<ITextInputControl>("password");
        var loginBtn = _page.GetControl<IClickable>("loginButton");

        // Same method calls work on all platforms
        username.Enter("testuser");
        password.Enter("password123");
        loginBtn.Click();

        // Verify success
        var welcomeMsg = _page.GetControl<ITextInputControl>("welcomeMessage");
        welcomeMsg.WaitVisible(true, 5000);
    }

    [Fact]
    public void LoginWithInvalidPassword()
    {
        var username = _page.GetControl<ITextInputControl>("username");
        var password = _page.GetControl<ITextInputControl>("password");
        var loginBtn = _page.GetControl<IClickable>("loginButton");

        username.Enter("testuser");
        password.Enter("wrongpassword");
        loginBtn.Click();

        // Verify error
        var errorMsg = _page.GetControl<ITextInputControl>("errorMessage");
        errorMsg.WaitVisible(true, 3000);
        errorMsg.AssertTextContains("Invalid");
    }
}

// Usage - same tests on all platforms:
// RunTests(new MauiPageObject(...));      // MAUI
// RunTests(new WpfPageObject(...));       // WPF
// RunTests(new WinFormsPageObject(...));  // WinForms
// RunTests(new SeleniumPageObject(...));  // Html/Selenium
// RunTests(new PlaywrightPageObject(...));// Html.Playwright
// RunTests(new StridePageObject(...));    // Stride
```

### Pattern 2: Platform-Specific Setup

```csharp
// Abstract test class with platform-specific page factory
public abstract class BaseTestSuite
{
    protected abstract IPageObject CreatePage();

    [Fact]
    public void RunTestOnAnyPlatform()
    {
        var page = CreatePage();

        // Test code is platform-agnostic
        var button = page.GetControl<IClickable>("submitButton");
        button.Click();
    }
}

// MAUI implementation
public class MauiTestSuite : BaseTestSuite
{
    protected override IPageObject CreatePage()
    {
        var context = new MauiTestContext(appPath, capabilities);
        return new LoginPageMaui(context);
    }
}

// WPF implementation
public class WpfTestSuite : BaseTestSuite
{
    protected override IPageObject CreatePage()
    {
        var context = new FlaUITestContext(processPath);
        return new LoginPageWpf(context);
    }
}

// Html/Selenium implementation
public class SeleniumTestSuite : BaseTestSuite
{
    protected override IPageObject CreatePage()
    {
        var context = new SeleniumTestContext(remoteUrl);
        return new LoginPageSelenium(context);
    }
}
```

### Pattern 3: Conditional Platform Features

```csharp
// Some features are platform-specific
public class CrossPlatformTest
{
    private readonly IPageObject _page;

    public void TestWithPlatformDetection(IPageObject page)
    {
        // Basic operations work everywhere
        var control = page.GetControl<ITextInputControl>("name");
        control.Enter("value");

        // Web-only features (navigation)
        if (page is INavigableControl nav)
        {
            nav.Goto("https://example.com");
            var url = nav.GetCurrentUrl();
        }

        // Desktop-only features (window management)
        if (page is WindowObject window)
        {
            window.Maximize();
            window.SetSize(1920, 1080);
        }
    }
}
```

### Pattern 4: Retry with Platform Awareness

```csharp
public static class PlatformAwareRetry
{
    // Different timeout tolerances per platform
    private static readonly Dictionary<Type, int> PlatformTimeouts = new()
    {
        { typeof(MauiTestContext), 10000 },      // Mobile can be slower
        { typeof(FlaUITestContext), 3000 },      // Desktop is fast
        { typeof(SeleniumTestContext), 5000 },   // Web varies
        { typeof(PlaywrightTestContext), 4000 }, // Playwright is fast
        { typeof(StrideTestContext), 15000 }     // Game engine can be slow
    };

    public static void ClickWithRetry(IClickable control, Type contextType)
    {
        int timeout = PlatformTimeouts[contextType];
        int maxRetries = 3;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                control.WaitClickable(true, timeout);
                control.Click();
                return;
            }
            catch when (i < maxRetries - 1)
            {
                Thread.Sleep(500);
            }
        }
    }
}
```

### Pattern 5: Test Data Management

```csharp
// Define test data in platform-neutral way
public class TestDataSet
{
    public LoginCredentials[] ValidLogins { get; set; }
    public string[] InvalidPasswords { get; set; }
    public FormData[] ComplexForms { get; set; }
}

// Use same test data on all platforms
public class DataDrivenTests
{
    [Theory]
    [MemberData(nameof(GetValidLogins))]
    public void LoginWithEachValidCredential(LoginCredentials creds, IPageObject page)
    {
        // Same test code, different data, all platforms
        var username = page.GetControl<ITextInputControl>("username");
        var password = page.GetControl<ITextInputControl>("password");
        var loginBtn = page.GetControl<IClickable>("loginButton");

        username.Enter(creds.Username);
        password.Enter(creds.Password);
        loginBtn.Click();

        var welcome = page.GetControl<ITextInputControl>("welcomeMessage");
        welcome.WaitVisible(true, 5000);
    }

    public static TheoryData<LoginCredentials> GetValidLogins() => new()
    {
        { new LoginCredentials { Username = "user1", Password = "pass1" } },
        { new LoginCredentials { Username = "user2", Password = "pass2" } },
        { new LoginCredentials { Username = "admin", Password = "admin123" } }
    };
}
```

---

## Summary

All 6 platforms are now unified through enhanced base classes implementing consistent interfaces. Test code written to interfaces works identically across:

- **MAUI** (Appium)
- **WPF** (FlaUI)
- **WinForms** (FlaUI)
- **Html/Selenium**
- **Html.Playwright**
- **Stride** (Game Engine)

The only differences are:
1. **Platform setup** (creating the test context)
2. **Advanced platform features** (conditional code)
3. **Timeout values** (platform-specific performance)

Test automation code itself remains **100% platform-independent**.

---

**Document Status:** Complete for Phase 1, Task 5, Step 6  
**Last Modified:** January 3, 2026  
**Next Document:** [18-test-writer-migration-guide.md](18-test-writer-migration-guide.md)
