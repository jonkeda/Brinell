# SPEC-006-003: MAUI Control Hierarchy

**Version:** 1.0  
**Status:** Draft  
**Date:** January 2026

---

## 1. Base Classes

### ControlObjectBase

Foundation for all MAUI controls.

```csharp
public abstract class ControlObjectBase : IInteractiveControlObject
{
    protected readonly MauiTestContext Context;
    
    public ControlLocator Locator { get; }
    public IPageObject? Page { get; }

    // Primary constructor
    protected ControlObjectBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Locator = locator ?? throw new ArgumentNullException(nameof(locator));
        Page = page;
    }

    // String convenience constructor
    protected ControlObjectBase(MauiTestContext context, string automationId, IPageObject? page)
        : this(context, By.AutomationId(automationId), page)
    {
    }

    // Logging
    protected void Log(string message)
    {
        Context.Log($"[{GetType().Name}] {Locator}: {message}");
    }

    // Element access (existing)
    protected AppiumElement? FindElement();
    protected AppiumElement FindElementRequired(int? timeoutMs = null);

    // Is methods
    public bool IsExists();
    public bool IsVisible();
    public bool IsEnabled();
    public virtual string GetText(int? timeoutMs = null);

    // Wait methods  
    public bool WaitExists(bool? expected, int? timeoutMs = null);
    public bool WaitVisible(bool? expected, int? timeoutMs = null);
    public bool WaitEnabled(bool? expected, int? timeoutMs = null);

    // Check methods
    public void CheckExists(bool? expected, int? timeoutMs = null);
    public void CheckVisible(bool? expected, int? timeoutMs = null);
    public void CheckEnabled(bool? expected, int? timeoutMs = null);

    // Assert methods
    public void AssertExists(bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertVisible(bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertText(string? expected, string? message = null, int? timeoutMs = null);
    public void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null);
}
```

---

### ClickableControlBase

Base for clickable controls with virtual methods.

```csharp
public abstract class ClickableControlBase : ControlObjectBase, IClickableControlObject
{
    protected ClickableControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected ClickableControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    public virtual void Click(int? timeoutMs = null)
    {
        Log("Click()");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);
        FindElementRequired(timeoutMs).Click();
    }

    public virtual void DoubleClick(int? timeoutMs = null)
    {
        Log("DoubleClick()");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);
        var element = FindElementRequired(timeoutMs);
        new Actions(Driver).DoubleClick(element).Perform();
    }

    public virtual void RightClick(int? timeoutMs = null)
    {
        Log("RightClick()");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);
        var element = FindElementRequired(timeoutMs);
        new Actions(Driver).ContextClick(element).Perform();
    }

    public virtual void Hover(int? timeoutMs = null)
    {
        Log("Hover()");
        CheckVisible(true, timeoutMs);
        var element = FindElementRequired(timeoutMs);
        new Actions(Driver).MoveToElement(element).Perform();
    }

    public virtual void LongPress(int? durationMs = null, int? timeoutMs = null)
    {
        var duration = durationMs ?? 1000;
        Log($"LongPress(duration={duration}ms)");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);
        // W3C Actions implementation...
    }
}
```

---

### TextControlBase

Base for text input controls.

```csharp
public abstract class TextControlBase : ClickableControlBase, ITextControlObject
{
    protected TextControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected TextControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    // Focus
    public virtual bool IsFocused()
    {
        var element = FindElement();
        return element?.GetAttribute("focused") == "true";
    }

    public virtual void Focus(int? timeoutMs = null)
    {
        Log("Focus()");
        Click(timeoutMs); // Clicking focuses
    }

    public virtual void Blur(int? timeoutMs = null)
    {
        Log("Blur()");
        var element = FindElementRequired(timeoutMs);
        element.SendKeys("\t");
    }

    // Text input
    public virtual void Enter(string? text, int? timeoutMs = null)
    {
        if (text is null) return;
        Log($"Enter(\"{text}\")");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);
        var element = FindElementRequired(timeoutMs);
        element.Clear();
        element.SendKeys(text);
    }

    public virtual void Clear(int? timeoutMs = null)
    {
        Log("Clear()");
        CheckVisible(true, timeoutMs);
        FindElementRequired(timeoutMs).Clear();
    }

    public virtual void ClearAndEnter(string? text, int? timeoutMs = null)
    {
        Clear(timeoutMs);
        if (text is not null)
        {
            var element = FindElementRequired(timeoutMs);
            element.SendKeys(text);
        }
    }

    public virtual void Append(string? text, int? timeoutMs = null)
    {
        if (text is null) return;
        Log($"Append(\"{text}\")");
        CheckVisible(true, timeoutMs);
        FindElementRequired(timeoutMs).SendKeys(text);
    }

    // Read-only
    public virtual bool IsReadOnly()
    {
        var element = FindElement();
        return element?.GetAttribute("readonly") == "true";
    }

    public virtual int GetTextLength(int? timeoutMs = null)
    {
        return GetText(timeoutMs)?.Length ?? 0;
    }
}
```

---

## 2. Concrete Controls

### ButtonControl

```csharp
public class ButtonControl : ClickableControlBase
{
    public ButtonControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public ButtonControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }

    // Inherits all virtual methods from ClickableControlBase
    // Override only if MAUI-specific behavior needed
}
```

### EntryControl

```csharp
public class EntryControl : TextControlBase
{
    public EntryControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public EntryControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }

    // Inherits all virtual methods from TextControlBase
}
```

### LabelControl

```csharp
public class LabelControl : ControlObjectBase
{
    public LabelControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public LabelControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }

    // Labels are not clickable - only text retrieval
}
```

---

## 3. PageObject Pattern

### PageObjectBase

```csharp
public abstract class PageObjectBase : IPageObject
{
    protected readonly MauiTestContext Context;
    
    public abstract string Name { get; }
    protected abstract ControlLocator PageLocator { get; }

    protected PageObjectBase(MauiTestContext context)
    {
        Context = context;
    }

    public bool IsLoaded(int? timeoutMs = null) { ... }
    public bool WaitLoaded(bool? expected, int? timeoutMs = null) { ... }
    
    // Control helpers - use 'new' pattern
    protected ButtonControl Button(string automationId) => new(Context, automationId, this);
    protected EntryControl Entry(string automationId) => new(Context, automationId, this);
    protected LabelControl Label(string automationId) => new(Context, automationId, this);
}
```

### Example PageObject

```csharp
public class MainPage : PageObjectBase
{
    public override string Name => "MainPage";
    protected override ControlLocator PageLocator => By.AutomationId("MainPage");

    public MainPage(MauiTestContext context) : base(context) { }

    // Controls - using 'new' pattern
    public ButtonControl IncrementButton => new(Context, "IncrementButton", this);
    public ButtonControl DecrementButton => new(Context, "DecrementButton", this);
    public ButtonControl ResetButton => new(Context, "ResetButton", this);
    
    public EntryControl NameEntry => new(Context, "NameEntry", this);
    public EntryControl EmailEntry => new(Context, "EmailEntry", this);
    
    public LabelControl CounterLabel => new(Context, "CounterLabel", this);
    public LabelControl TitleLabel => new(Context, "TitleLabel", this);
}
```

---

## 4. Inheritance Diagram

```
IControlObject
│
├── IInteractiveControlObject
│   │
│   ├── IClickableControlObject
│   │   └── ClickableControlBase (virtual Click, DoubleClick, etc.)
│   │       └── ButtonControl
│   │
│   └── IFocusableControlObject
│       └── ITextControlObject
│           └── TextControlBase (virtual Enter, Clear, etc.)
│               ├── EntryControl
│               └── EditorControl
│
└── ControlObjectBase (Is/Wait/Check/Assert)
    └── LabelControl
```
