# 6. ControlObject Hierarchy - Code Examples

**Parent:** [ControlObject Hierarchy](21d6_ControlObjectHierarchy.md)  
**Version:** 3.0 (Updated December 2025)

**Note (v3):** Base classes are platform-specific, not shared. The examples below show the WPF implementation pattern. MAUI and HTML projects have similar structures using their native drivers.

---

## 6.1 WPF ControlBase (Implements IControlObject)

```csharp
namespace Oravey.UITestFramework.Core.Controls.Base;

using Oravey.UITestFramework.Core.Abstractions;
using Oravey.UITestFramework.Core.Logging;
using Oravey.UITestFramework.Core.Pages.Base;

/// <summary>
/// Base class for all control objects.
/// All methods are virtual for platform-specific customization.
/// </summary>
public abstract class ControlObjectBase
{
    protected readonly ITestContext Context;
    protected readonly PageObjectBase? Page;
    protected readonly string AutomationId;
    protected ITestLogger Logger => Context.Logger;
    
    protected ControlObjectBase(ITestContext context, PageObjectBase? page, string automationId)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Page = page;
        AutomationId = automationId ?? throw new ArgumentNullException(nameof(automationId));
    }
    
    #region Element Access
    
    /// <summary>Get the element, returns null if not found.</summary>
    protected virtual IElementAdapter? GetElement() => Context.Driver.FindElement(AutomationId);
    
    /// <summary>Get the element, throws if not found.</summary>
    protected virtual IElementAdapter GetRequiredElement()
    {
        return GetElement() 
            ?? throw new AssertionException($"Element '{AutomationId}' not found");
    }
    
    #endregion
    
    #region Existence - Is/Wait/Check/Assert (all virtual)
    
    /// <summary>Immediate check if element exists.</summary>
    public virtual bool IsExists() => GetElement() != null;
    
    /// <summary>Wait for existence state.</summary>
    public virtual bool WaitExists(bool expected = true, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => IsExists() == expected,
            timeoutMs,
            $"'{AutomationId}' exists = {expected}");
    }
    
    /// <summary>Wait for existence, throw on failure.</summary>
    public virtual void CheckExists(bool expected = true, int? timeoutMs = null)
    {
        if (!WaitExists(expected, timeoutMs))
        {
            throw new AssertionException(
                $"Control '{AutomationId}' existence check failed. Expected: {expected}");
        }
    }
    
    /// <summary>Semantic assertion for existence with logging.</summary>
    public virtual void AssertExists(bool expected = true, int? timeoutMs = null)
    {
        var actual = IsExists();
        var passed = WaitExists(expected, timeoutMs);
        
        Logger.LogAssertion(
            Context.TestName,
            Page?.PageName,
            AutomationId,
            "AssertExists",
            actual.ToString(),
            expected.ToString(),
            passed,
            passed ? null : $"Expected exists={expected}, was {actual}");
        
        if (!passed)
        {
            throw new AssertionException(
                $"Control '{AutomationId}' exists assertion failed. Expected: {expected}, Actual: {actual}");
        }
    }
    
    #endregion
    
    #region Visibility - Is/Wait/Check/Assert (all virtual)
    
    /// <summary>Immediate visibility check.</summary>
    public virtual bool IsVisible()
    {
        var element = GetElement();
        return element != null && Context.Driver.IsVisible(element);
    }
    
    /// <summary>Wait for visibility state.</summary>
    public virtual bool WaitVisible(bool expected = true, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => IsVisible() == expected,
            timeoutMs,
            $"'{AutomationId}' visible = {expected}");
    }
    
    /// <summary>Wait for visibility, throw on failure.</summary>
    public virtual void CheckVisible(bool expected = true, int? timeoutMs = null)
    {
        if (!WaitVisible(expected, timeoutMs))
        {
            throw new AssertionException(
                $"Control '{AutomationId}' visibility check failed. Expected: {expected}");
        }
    }
    
    /// <summary>Semantic assertion for visibility with logging.</summary>
    public virtual void AssertVisible(bool expected = true, int? timeoutMs = null)
    {
        var actual = IsVisible();
        var passed = WaitVisible(expected, timeoutMs);
        
        Logger.LogAssertion(
            Context.TestName,
            Page?.PageName,
            AutomationId,
            "AssertVisible",
            actual.ToString(),
            expected.ToString(),
            passed,
            passed ? null : $"Expected visible={expected}, was {actual}");
        
        if (!passed)
        {
            throw new AssertionException(
                $"Control '{AutomationId}' visibility assertion failed. Expected: {expected}, Actual: {actual}");
        }
    }
    
    #endregion
    
    #region Enabled - Is/Wait/Check/Assert (all virtual)
    
    /// <summary>Immediate enabled check.</summary>
    public virtual bool IsEnabled()
    {
        var element = GetElement();
        return element != null && Context.Driver.IsEnabled(element);
    }
    
    /// <summary>Wait for enabled state.</summary>
    public virtual bool WaitEnabled(bool expected = true, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => IsEnabled() == expected,
            timeoutMs,
            $"'{AutomationId}' enabled = {expected}");
    }
    
    /// <summary>Wait for enabled, throw on failure.</summary>
    public virtual void CheckEnabled(bool expected = true, int? timeoutMs = null)
    {
        if (!WaitEnabled(expected, timeoutMs))
        {
            throw new AssertionException(
                $"Control '{AutomationId}' enabled check failed. Expected: {expected}");
        }
    }
    
    /// <summary>Semantic assertion for enabled with logging.</summary>
    public virtual void AssertEnabled(bool expected = true, int? timeoutMs = null)
    {
        var actual = IsEnabled();
        var passed = WaitEnabled(expected, timeoutMs);
        
        Logger.LogAssertion(
            Context.TestName,
            Page?.PageName,
            AutomationId,
            "AssertEnabled",
            actual.ToString(),
            expected.ToString(),
            passed,
            passed ? null : $"Expected enabled={expected}, was {actual}");
        
        if (!passed)
        {
            throw new AssertionException(
                $"Control '{AutomationId}' enabled assertion failed. Expected: {expected}, Actual: {actual}");
        }
    }
    
    #endregion
    
    #region Clickable - Is/Wait/Check/Assert (all virtual)
    
    /// <summary>Check if control is clickable (visible AND enabled).</summary>
    public virtual bool IsClickable() => IsVisible() && IsEnabled();
    
    /// <summary>Wait for clickable state.</summary>
    public virtual bool WaitClickable(int? timeoutMs = null)
    {
        return Context.WaitFor(
            IsClickable,
            timeoutMs,
            $"'{AutomationId}' clickable");
    }
    
    /// <summary>Wait for clickable, throw on failure.</summary>
    public virtual void CheckClickable(int? timeoutMs = null)
    {
        if (!WaitClickable(timeoutMs))
        {
            var visible = IsVisible();
            var enabled = IsEnabled();
            throw new AssertionException(
                $"Control '{AutomationId}' is not clickable. " +
                $"Visible: {visible}, Enabled: {enabled}");
        }
    }
    
    /// <summary>Semantic assertion for clickable with logging.</summary>
    public virtual void AssertClickable(int? timeoutMs = null)
    {
        var passed = WaitClickable(timeoutMs);
        var visible = IsVisible();
        var enabled = IsEnabled();
        
        Logger.LogAssertion(
            Context.TestName,
            Page?.PageName,
            AutomationId,
            "AssertClickable",
            $"visible={visible},enabled={enabled}",
            "clickable=true",
            passed,
            passed ? null : $"Not clickable: visible={visible}, enabled={enabled}");
        
        if (!passed)
        {
            throw new AssertionException(
                $"Control '{AutomationId}' clickable assertion failed. " +
                $"Visible: {visible}, Enabled: {enabled}");
        }
    }
    
    #endregion
    
    protected virtual void Log(string message)
    {
        Logger.LogInfo(Context.TestName, Page?.PageName, $"[{AutomationId}] {message}");
    }
}

/// <summary>
/// Exception thrown when an assertion fails.
/// </summary>
public class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
    public AssertionException(string message, Exception inner) : base(message, inner) { }
}
```

---

## 6.2 ViewControlBase

```csharp
namespace Oravey.UITestFramework.Core.Controls.Base;

using Oravey.UITestFramework.Core.Abstractions;
using Oravey.UITestFramework.Core.Pages.Base;

/// <summary>
/// Base class for controls that display content (text, attributes).
/// </summary>
public abstract class ViewControlBase : ControlObjectBase
{
    protected ViewControlBase(ITestContext context, PageObjectBase? page, string automationId)
        : base(context, page, automationId)
    {
    }
    
    #region Text - Get/Wait/Check/Assert (all virtual)
    
    /// <summary>Get current text content.</summary>
    public virtual string GetText()
    {
        var element = GetElement();
        return element != null ? Context.Driver.GetText(element) : string.Empty;
    }
    
    /// <summary>Wait for exact text.</summary>
    public virtual bool WaitText(string expected, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => GetText() == expected,
            timeoutMs,
            $"'{AutomationId}' text = '{expected}'");
    }
    
    /// <summary>Wait for text containing substring.</summary>
    public virtual bool WaitTextContains(string substring, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => GetText().Contains(substring, StringComparison.OrdinalIgnoreCase),
            timeoutMs,
            $"'{AutomationId}' text contains '{substring}'");
    }
    
    /// <summary>Wait for text, throw on failure.</summary>
    public virtual void CheckText(string expected, int? timeoutMs = null)
    {
        if (!WaitText(expected, timeoutMs))
        {
            throw new AssertionException(
                $"Control '{AutomationId}' text check failed. Expected: '{expected}', Actual: '{GetText()}'");
        }
    }
    
    /// <summary>Semantic assertion for text with logging.</summary>
    public virtual void AssertText(string expected, int? timeoutMs = null)
    {
        var actual = GetText();
        var passed = WaitText(expected, timeoutMs);
        
        Logger.LogAssertion(
            Context.TestName,
            Page?.PageName,
            AutomationId,
            "AssertText",
            actual,
            expected,
            passed,
            passed ? null : $"Expected '{expected}', was '{actual}'");
        
        if (!passed)
        {
            throw new AssertionException(
                $"Control '{AutomationId}' text assertion failed. Expected: '{expected}', Actual: '{actual}'");
        }
    }
    
    /// <summary>Semantic assertion for text contains with logging.</summary>
    public virtual void AssertTextContains(string substring, int? timeoutMs = null)
    {
        var actual = GetText();
        var passed = WaitTextContains(substring, timeoutMs);
        
        Logger.LogAssertion(
            Context.TestName,
            Page?.PageName,
            AutomationId,
            "AssertTextContains",
            actual,
            $"contains '{substring}'",
            passed,
            passed ? null : $"'{actual}' does not contain '{substring}'");
        
        if (!passed)
        {
            throw new AssertionException(
                $"Control '{AutomationId}' text assertion failed. " +
                $"Expected to contain: '{substring}', Actual: '{actual}'");
        }
    }
    
    #endregion
    
    #region Attribute - Get/Wait/Assert (all virtual)
    
    /// <summary>Get attribute value.</summary>
    public virtual string? GetAttribute(string name)
    {
        var element = GetElement();
        return element != null ? Context.Driver.GetAttribute(element, name) : null;
    }
    
    /// <summary>Wait for attribute value.</summary>
    public virtual bool WaitAttribute(string name, string expected, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => GetAttribute(name) == expected,
            timeoutMs,
            $"'{AutomationId}' attribute '{name}' = '{expected}'");
    }
    
    /// <summary>Semantic assertion for attribute with logging.</summary>
    public virtual void AssertAttribute(string name, string expected, int? timeoutMs = null)
    {
        var actual = GetAttribute(name);
        var passed = WaitAttribute(name, expected, timeoutMs);
        
        Logger.LogAssertion(
            Context.TestName,
            Page?.PageName,
            AutomationId,
            $"AssertAttribute[{name}]",
            actual ?? "(null)",
            expected,
            passed,
            passed ? null : $"Expected '{expected}', was '{actual}'");
        
        if (!passed)
        {
            throw new AssertionException(
                $"Control '{AutomationId}' attribute '{name}' assertion failed. " +
                $"Expected: '{expected}', Actual: '{actual}'");
        }
    }
    
    #endregion
}
```

---

## 6.3 ClickableControlBase

```csharp
namespace Oravey.UITestFramework.Core.Controls.Base;

using Oravey.UITestFramework.Core.Abstractions;
using Oravey.UITestFramework.Core.Pages.Base;

/// <summary>
/// Base class for clickable controls (buttons, links).
/// All action methods call CheckClickable() first.
/// </summary>
public abstract class ClickableControlBase : ViewControlBase
{
    protected ClickableControlBase(ITestContext context, PageObjectBase? page, string automationId)
        : base(context, page, automationId)
    {
    }
    
    /// <summary>
    /// Click the control.
    /// ALWAYS checks clickable state first.
    /// </summary>
    public virtual void Click()
    {
        // CRITICAL: Always check before action
        CheckClickable();
        
        var element = GetRequiredElement();
        Context.Driver.Click(element);
        
        Logger.LogAction(
            Context.TestName,
            Page?.PageName,
            AutomationId,
            "Click",
            null,
            null,
            "Success",
            null);
    }
    
    /// <summary>
    /// Double-click the control.
    /// ALWAYS checks clickable state first.
    /// </summary>
    public virtual void DoubleClick()
    {
        // CRITICAL: Always check before action
        CheckClickable();
        
        var element = GetRequiredElement();
        Context.Driver.DoubleClick(element);
        
        Logger.LogAction(
            Context.TestName,
            Page?.PageName,
            AutomationId,
            "DoubleClick",
            null,
            null,
            "Success",
            null);
    }
    
    /// <summary>
    /// Tap the control (alias for Click on mobile).
    /// </summary>
    public virtual void Tap() => Click();
}
```

---

## 6.4 InputControlBase

```csharp
namespace Oravey.UITestFramework.Core.Controls.Base;

using Oravey.UITestFramework.Core.Abstractions;
using Oravey.UITestFramework.Core.Pages.Base;

/// <summary>
/// Base class for input controls (text boxes, entries).
/// All action methods call CheckEnabled() first.
/// </summary>
public abstract class InputControlBase : ViewControlBase
{
    protected InputControlBase(ITestContext context, PageObjectBase? page, string automationId)
        : base(context, page, automationId)
    {
    }
    
    /// <summary>
    /// Enter text (clears existing content first).
    /// ALWAYS checks enabled state first.
    /// </summary>
    public virtual void EnterText(string text)
    {
        // CRITICAL: Always check before action
        CheckEnabled();
        
        var element = GetRequiredElement();
        Context.Driver.Clear(element);
        Context.Driver.SendKeys(element, text);
        
        Logger.LogAction(
            Context.TestName,
            Page?.PageName,
            AutomationId,
            "EnterText",
            text,
            null,
            "Success",
            null);
    }
    
    /// <summary>
    /// Clear the input content.
    /// ALWAYS checks enabled state first.
    /// </summary>
    public virtual void Clear()
    {
        // CRITICAL: Always check before action
        CheckEnabled();
        
        var element = GetRequiredElement();
        Context.Driver.Clear(element);
        
        Logger.LogAction(
            Context.TestName,
            Page?.PageName,
            AutomationId,
            "Clear",
            null,
            null,
            "Success",
            null);
    }
    
    /// <summary>
    /// Append text (does not clear existing content).
    /// ALWAYS checks enabled state first.
    /// </summary>
    public virtual void AppendText(string text)
    {
        // CRITICAL: Always check before action
        CheckEnabled();
        
        var element = GetRequiredElement();
        Context.Driver.SendKeys(element, text);
        
        Logger.LogAction(
            Context.TestName,
            Page?.PageName,
            AutomationId,
            "AppendText",
            text,
            null,
            "Success",
            null);
    }
}
```

---

## 6.5 ToggleControlBase

```csharp
namespace Oravey.UITestFramework.Core.Controls.Base;

using Oravey.UITestFramework.Core.Abstractions;
using Oravey.UITestFramework.Core.Pages.Base;

/// <summary>
/// Base class for toggle controls (checkboxes, switches).
/// All action methods call CheckClickable() first.
/// </summary>
public abstract class ToggleControlBase : ViewControlBase
{
    protected ToggleControlBase(ITestContext context, PageObjectBase? page, string automationId)
        : base(context, page, automationId)
    {
    }
    
    /// <summary>Get current toggle state.</summary>
    public virtual bool IsChecked()
    {
        var element = GetElement();
        return element != null && Context.Driver.IsSelected(element);
    }
    
    /// <summary>
    /// Toggle the control.
    /// ALWAYS checks clickable state first.
    /// </summary>
    public virtual void Toggle()
    {
        // CRITICAL: Always check before action
        CheckClickable();
        
        var element = GetRequiredElement();
        Context.Driver.Click(element);
        
        Logger.LogAction(
            Context.TestName,
            Page?.PageName,
            AutomationId,
            "Toggle",
            IsChecked().ToString(),
            null,
            "Success",
            null);
    }
    
    /// <summary>
    /// Set to specific checked state.
    /// Only toggles if current state differs from desired.
    /// ALWAYS checks clickable state first.
    /// </summary>
    public virtual void SetChecked(bool value)
    {
        // CRITICAL: Always check before action
        CheckClickable();
        
        var current = IsChecked();
        if (current != value)
        {
            Toggle();
        }
        
        Logger.LogAction(
            Context.TestName,
            Page?.PageName,
            AutomationId,
            "SetChecked",
            value.ToString(),
            null,
            "Success",
            $"Was {current}, now {value}");
    }
    
    /// <summary>Wait for checked state.</summary>
    public virtual bool WaitChecked(bool expected = true, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => IsChecked() == expected,
            timeoutMs,
            $"'{AutomationId}' checked = {expected}");
    }
    
    /// <summary>Semantic assertion for checked state with logging.</summary>
    public virtual void AssertChecked(bool expected = true, int? timeoutMs = null)
    {
        var actual = IsChecked();
        var passed = WaitChecked(expected, timeoutMs);
        
        Logger.LogAssertion(
            Context.TestName,
            Page?.PageName,
            AutomationId,
            "AssertChecked",
            actual.ToString(),
            expected.ToString(),
            passed,
            passed ? null : $"Expected checked={expected}, was {actual}");
        
        if (!passed)
        {
            throw new AssertionException(
                $"Control '{AutomationId}' checked assertion failed. " +
                $"Expected: {expected}, Actual: {actual}");
        }
    }
}
```

---

## 6.6 Platform-Specific ButtonControl (WPF)

```csharp
namespace Oravey.UITestFramework.Wpf.Controls;

using FlaUI.Core.AutomationElements;
using Oravey.UITestFramework.Core.Controls.Base;
using Oravey.UITestFramework.Core.Pages.Base;
using Oravey.UITestFramework.Wpf.Infrastructure;

/// <summary>
/// WPF Button control wrapper.
/// Inherits all virtual methods from ClickableControlBase.
/// </summary>
public class ButtonControl : ClickableControlBase
{
    public ButtonControl(FlaUITestContext context, PageObjectBase? page, string automationId)
        : base(context, page, automationId)
    {
    }
    
    /// <summary>
    /// WPF-specific: Use Invoke pattern for more reliable clicks.
    /// </summary>
    public override void Click()
    {
        // CRITICAL: Always check before action
        CheckClickable();
        
        var element = GetRequiredElement();
        var flaElement = ((FlaUIElementAdapter)element).Element;
        
        // Prefer Invoke pattern if supported
        if (flaElement.Patterns.Invoke.IsSupported)
        {
            flaElement.Patterns.Invoke.Pattern.Invoke();
        }
        else
        {
            flaElement.Click();
        }
        
        Logger.LogAction(
            Context.TestName,
            Page?.PageName,
            AutomationId,
            "Click",
            null,
            null,
            "Success",
            null);
    }
}
```

---

## 6.7 Platform-Specific EntryControl (MAUI)

```csharp
namespace Oravey.UITestFramework.Maui.Controls;

using OpenQA.Selenium.Appium;
using Oravey.UITestFramework.Core.Controls.Base;
using Oravey.UITestFramework.Core.Pages.Base;
using Oravey.UITestFramework.Maui.Infrastructure;

/// <summary>
/// MAUI Entry control wrapper.
/// Inherits all virtual methods from InputControlBase.
/// </summary>
public class EntryControl : InputControlBase
{
    private readonly AppiumTestContext _appiumContext;
    
    public EntryControl(AppiumTestContext context, PageObjectBase? page, string automationId)
        : base(context, page, automationId)
    {
        _appiumContext = context;
    }
    
    /// <summary>
    /// MAUI-specific: Handle keyboard dismissal after input.
    /// </summary>
    public override void EnterText(string text)
    {
        base.EnterText(text);
        
        // Dismiss keyboard on mobile
        if (_appiumContext.Platform.IsMobile())
        {
            DismissKeyboard();
        }
    }
    
    /// <summary>
    /// Dismiss the on-screen keyboard.
    /// </summary>
    public virtual void DismissKeyboard()
    {
        try
        {
            var driver = ((AppiumDriverAdapter)Context.Driver).NativeDriver;
            driver.HideKeyboard();
        }
        catch
        {
            // Keyboard may not be visible, ignore
        }
    }
}
```

---

*Related: [Wait/Check/Is/Assert Pattern Code Examples](21d7_WaitCheckIsAssertPattern_CodeExamples.md)*
