# 211.003 Controls Module

**Block Type:** MOD (Module)  
**ID:** 211.003  
**Title:** Controls Module Definition  
**Status:** Draft  
**Version:** 1.0

---

## 1. Overview

The Controls module contains concrete control implementations for each platform. Controls inherit from base classes and implement interfaces to provide automation capabilities for specific UI control types.

> **Note:** Code snippets in this document are illustrative examples showing the intended patterns and API design. Final implementations may vary in details.

### Module Identity

- **Packages:** `Brinell.Maui`, `Brinell.Blazor`, `Brinell.Wpf`
- **Namespace:** `Brinell.<Platform>.Controls`
- **Dependencies:** Base Classes, Core Interfaces
- **Consumers:** Page objects, test code

---

## 2. Purpose

The Controls module provides:

1. **Concrete Implementations** — Actual control objects for UI automation
2. **Platform Mapping** — Maps framework interfaces to platform controls
3. **Control-Specific Behavior** — Handles quirks of specific control types
4. **Test API** — The classes test writers instantiate and use

---

## 3. Control Implementation Pattern

Controls follow a consistent implementation pattern: single inheritance from the most appropriate base class, plus implementation of additional capability interfaces as needed.

### 3.1 Single Inheritance + Multiple Interfaces

C# supports single class inheritance but multiple interface implementation. Controls inherit common functionality from one base class while implementing additional interfaces to express all capabilities.

```csharp
// Controls inherit from ONE base class and implement ADDITIONAL interfaces
public class EntryControl : EditableTextControlBase, IClickableControlObject
{
    public EntryControl(IMauiTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    // IClickableControlObject implementation
    public void Click(int? timeoutMs = null) => ClickElement(FindElement());
    public void DoubleClick(int? timeoutMs = null) => DoubleClickElement(FindElement());
    public void RightClick(int? timeoutMs = null) => RightClickElement(FindElement());
    
    // Platform-specific overrides
    protected override void EnterText(object element, string text)
    {
        ((AppiumElement)element).SendKeys(text);
    }
    
    protected override void ClearElement(object element)
    {
        ((AppiumElement)element).Clear();
    }
}
```

### 3.2 Control Creation Pattern

Page objects create controls using the `new` pattern with a page reference. This provides explicit control instantiation while maintaining page association for logging and scoping.

```csharp
// Page objects use 'new' pattern to create controls
public class LoginPage : PageObjectBase
{
    public EntryControl Username { get; }
    public EntryControl Password { get; }
    public ButtonControl LoginButton { get; }
    
    public LoginPage(IMauiTestContext context) : base(context, "Login")
    {
        Username = new EntryControl(context, Locator.ByAutomationId("UsernameEntry"), this);
        Password = new EntryControl(context, Locator.ByAutomationId("PasswordEntry"), this);
        LoginButton = new ButtonControl(context, Locator.ByAutomationId("LoginButton"), this);
    }
}
```

**Why `new` pattern:**
- Explicit control instantiation (no hidden factories)
- Type-safe with compile-time checking
- Page reference via constructor parameter
- Supports custom control parameters
- Simple and predictable behavior

---

## 4. MAUI Controls

Controls for .NET MAUI applications automated via Appium. Each MAUI control type maps to a Brinell class implementing the appropriate interfaces.

| MAUI Control | Brinell Class | Interfaces |
|--------------|---------------|------------|
| Button | ButtonControl | IClickableControlObject |
| Label | LabelControl | ITextControlObject |
| Entry | EntryControl | IEditableTextControlObject, IClickableControlObject |
| Editor | EditorControl | IEditableTextControlObject |
| CheckBox | CheckBoxControl | IToggleControlObject, IClickableControlObject |
| Switch | SwitchControl | IToggleControlObject |
| RadioButton | RadioButtonControl | IToggleControlObject, IClickableControlObject |
| Picker | PickerControl | ISelectorControlObject |
| Slider | SliderControl | IRangeControlObject |
| Stepper | StepperControl | IRangeControlObject |
| ProgressBar | ProgressBarControl | IRangeControlObject (read-only) |
| ActivityIndicator | ActivityIndicatorControl | IControlObject |
| Image | ImageControl | IClickableControlObject |
| ImageButton | ImageButtonControl | IClickableControlObject |
| ListView | ListViewControl | IItemsControlObject, IScrollableControlObject |
| CollectionView | CollectionViewControl | IItemsControlObject, IScrollableControlObject |
| ScrollView | ScrollViewControl | IContainerControlObject, IScrollableControlObject |
| Frame | FrameControl | IContainerControlObject |
| Grid | GridControl | IContainerControlObject |
| StackLayout | StackLayoutControl | IContainerControlObject |
| DatePicker | DatePickerControl | IDateControlObject |
| TimePicker | TimePickerControl | ITimeControlObject |
| SearchBar | SearchBarControl | IEditableTextControlObject |
| WebView | WebViewControl | IWebViewControlObject |

---

## 5. Blazor Controls

Controls for Blazor applications automated via Selenium. HTML elements map to Brinell classes based on their behavior and attributes.

| HTML Element | Brinell Class | Interfaces |
|--------------|---------------|------------|
| button | ButtonControl | IClickableControlObject |
| a | LinkControl | IClickableControlObject, ITextControlObject |
| span, label, p | LabelControl | ITextControlObject |
| input[type=text] | TextInputControl | IEditableTextControlObject |
| input[type=password] | PasswordInputControl | IEditableTextControlObject |
| input[type=email] | EmailInputControl | IEditableTextControlObject |
| input[type=number] | NumberInputControl | IEditableTextControlObject, IRangeControlObject |
| textarea | TextAreaControl | IEditableTextControlObject |
| input[type=checkbox] | CheckBoxControl | IToggleControlObject |
| input[type=radio] | RadioButtonControl | IToggleControlObject |
| select | SelectControl | ISelectorControlObject |
| input[type=range] | RangeControl | IRangeControlObject |
| input[type=date] | DateInputControl | IDateControlObject |
| input[type=time] | TimeInputControl | ITimeControlObject |
| table | TableControl | IItemsControlObject |
| ul, ol | ListControl | IItemsControlObject |
| div, section | ContainerControl | IContainerControlObject |
| img | ImageControl | IControlObject |
| iframe | IFrameControl | IContainerControlObject |

---

## 6. Control Implementation Examples

Concrete examples showing how controls implement base classes and interfaces. These demonstrate the patterns used throughout the framework.

### 6.1 ButtonControl (MAUI)

A simple clickable control. Inherits click functionality from `ClickableControlBase` and provides platform-specific click implementation. The constructor takes `IMauiTestContext` for platform operations and optional `IPageObject?` for page association.

```csharp
public class ButtonControl : ClickableControlBase
{
    protected readonly IMauiTestContext _mauiContext;
    
    public ButtonControl(IMauiTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
        _mauiContext = context;
    }
    
    public ButtonControl(IMauiTestContext context, string automationId, IPageObject? page = null)
        : this(context, Locator.ByAutomationId(automationId), page) { }
    
    protected override void ClickElement(object element)
    {
        ((AppiumElement)element).Click();
    }
    
    protected override void DoubleClickElement(object element)
    {
        var actions = new Actions(_mauiContext.Driver);
        actions.DoubleClick((AppiumElement)element).Perform();
    }
    
    protected override void RightClickElement(object element)
    {
        var actions = new Actions(_mauiContext.Driver);
        actions.ContextClick((AppiumElement)element).Perform();
    }
}
```

### 6.2 CheckBoxControl (Blazor)

A toggle control that is also clickable. Inherits toggle functionality from `ToggleControlBase` and implements `IClickableControlObject` for direct click access. The constructor takes `IBlazorTestContext` for platform operations and optional `IPageObject?` for page association.

```csharp
public class CheckBoxControl : ToggleControlBase, IClickableControlObject
{
    public CheckBoxControl(IBlazorTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public override bool IsChecked()
    {
        var element = (IWebElement)FindElement();
        return element.Selected;
    }
    
    protected override void ToggleElement(object element)
    {
        ((IWebElement)element).Click();
    }
    
    // IClickableControlObject
    public void Click(int? timeoutMs = null) => ToggleElement(FindElement());
    public void DoubleClick(int? timeoutMs = null) { Click(); Click(); }
    public void RightClick(int? timeoutMs = null) 
    {
        // Right-click typically doesn't apply to checkboxes
    }
}
```

### 6.3 PickerControl (MAUI)

A selector control with platform-specific selection behavior. The picker must be opened before items can be selected, which is handled in the `SelectByText` method. The constructor takes `IMauiTestContext` for platform operations and optional `IPageObject?` for page association.

```csharp
public class PickerControl : SelectorControlBase
{
    protected readonly IMauiTestContext _mauiContext;
    
    public PickerControl(IMauiTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
        _mauiContext = context;
    }
    
    public override void SelectByText(string? text, int? timeoutMs = null)
    {
        if (text is null) return;  // Skip-on-null pattern
        
        var element = (AppiumElement)FindElement();
        element.Click(); // Open picker
        
        // Find and click item in picker wheel
        var itemElement = _mauiContext.FindElement(Locator.ByText(text));
        itemElement.Click();
        
        _context.Logger.LogAction("SelectByText", _locator, text);
    }
    
    public override void SelectByIndex(int? index, int? timeoutMs = null)
    {
        if (index is null) return;  // Skip-on-null pattern
        
        var items = GetItemTexts(timeoutMs);
        if (index.Value >= 0 && index.Value < items.Count)
            SelectByText(items[index.Value], timeoutMs);
    }
    
    public override string GetSelectedText(int? timeoutMs = null)
    {
        var element = (AppiumElement)FindElement();
        return element.Text ?? string.Empty;
    }
    
    public override int GetSelectedIndex(int? timeoutMs = null)
    {
        var selected = GetSelectedText(timeoutMs);
        var items = GetItemTexts(timeoutMs);
        return items.IndexOf(selected);
    }
    
    public override IReadOnlyList<string> GetItemTexts(int? timeoutMs = null)
    {
        // Platform-specific: read picker items
        throw new NotImplementedException("Requires platform-specific implementation");
    }
}
```

---

## 7. Control Usage in Tests

Controls are instantiated directly in test code or via page objects. The `new` pattern provides explicit, type-safe control creation.

```csharp
// Direct control creation in tests
var button = new ButtonControl(context, "SubmitButton");
button.Click();

// Or via page objects
var loginPage = new LoginPage(context);
loginPage.Username.Enter("user@example.com");
loginPage.Password.Enter("password");
loginPage.LoginButton.Click();
```

---

## 8. Custom Controls

Test projects can define custom controls for application-specific components. Custom controls typically extend `ContainerControlBase` and expose child controls as properties.

```csharp
// Custom control for a specific component
public class DateRangePickerControl : ContainerControlBase
{
    public DatePickerControl StartDate { get; }
    public DatePickerControl EndDate { get; }
    
    public DateRangePickerControl(IMauiTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
        // Create child controls scoped to this container
        StartDate = new DatePickerControl(context, 
            Locator.ByAutomationId("StartDatePicker").ScopedTo(locator), page);
        EndDate = new DatePickerControl(context, 
            Locator.ByAutomationId("EndDatePicker").ScopedTo(locator), page);
    }
    
    public void SetRange(DateTime start, DateTime end)
    {
        StartDate.SetDate(start);
        EndDate.SetDate(end);
    }
}
```

---

## 9. Validation Rules

The Controls module is valid when:

- [ ] Each platform control type has a corresponding control class
- [ ] Controls inherit from appropriate base class
- [ ] Controls implement all required interfaces
- [ ] Constructors accept platform context interface, Locator, and optional IPageObject
- [ ] Platform-specific code is isolated in control classes
- [ ] Nullable skip pattern is implemented for input methods
- [ ] Controls can be instantiated via `new` pattern

---

## Related Documents

- [Base Classes Module](211_002_BaseClasses.spx.md)
- [Interfaces Module](211_001_Interfaces.spx.md)
- [Page/Context Module](211_004_PageContext.spx.md)
- [FR-100 Control Object](../../100_requirements/120_functional/120_100_ControlObject.spx.md)
