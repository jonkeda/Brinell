# SPEC-006-002c: Input Classes

**Version:** 1.0  
**Status:** Final  
**Date:** January 2026

---

## 1. ClickableControlBase (Abstract)

```csharp
namespace Brinell.Core;

/// <summary>
/// Base class for clickable controls like buttons and links.
/// </summary>
public abstract class ClickableControlBase : InteractiveControlBase, IClickableControlObject
{
    protected ClickableControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for ClickAndWait with logging
    public virtual void ClickAndWait(int? waitMs = null, int? timeoutMs = null)
    {
        Click(timeoutMs);
        if (waitMs.HasValue)
        {
            Log($"ClickAndWait: waiting {waitMs}ms");
            Thread.Sleep(waitMs.Value);
        }
    }

    // Method signatures only
    public abstract void ClickIfExists(int? timeoutMs = null);
    public abstract void ClickIfEnabled(int? timeoutMs = null);
}
```

---

## 2. ButtonControlBase (Abstract)

```csharp
namespace Brinell.Core;

/// <summary>
/// Base class for button controls.
/// </summary>
public abstract class ButtonControlBase : ClickableControlBase, IButtonControlObject
{
    protected ButtonControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetLabel
    public virtual string? GetLabel(int? timeoutMs = null)
    {
        var text = GetText(timeoutMs);
        Log($"GetLabel: '{text}'");
        return text;
    }

    // Method signatures only
    public abstract bool WaitLabel(string? expected, int? timeoutMs = null);
    public abstract void AssertLabel(string? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 3. TextControlBase (Abstract)

```csharp
namespace Brinell.Core;

/// <summary>
/// Base class for editable text controls.
/// </summary>
public abstract class TextControlBase : FocusableControlBase, IEditableTextControlObject
{
    protected TextControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for Enter with logging
    public virtual void Enter(string? text, int? timeoutMs = null)
    {
        if (text == null) return;
        
        EnsureEnabled(timeoutMs);
        var element = WaitForElementVisible(timeoutMs);
        if (element == null)
            ThrowCheckFailed("Enter", $"Element '{_locator}' not visible.");
        
        EnterCore(element!, text);
        LogAction("Enter", text);
    }

    // Full implementation for Clear with logging
    public virtual void Clear(int? timeoutMs = null)
    {
        EnsureEnabled(timeoutMs);
        var element = WaitForElementVisible(timeoutMs);
        if (element == null)
            ThrowCheckFailed("Clear", $"Element '{_locator}' not visible.");
        
        ClearCore(element!);
        LogAction("Clear");
    }

    // Full implementation for SetText with logging
    public virtual void SetText(string? text, int? timeoutMs = null)
    {
        if (text == null) return;
        
        Clear(timeoutMs);
        Enter(text, timeoutMs);
        LogAction("SetText", text);
    }

    // Abstract core methods
    protected abstract void EnterCore(object element, string text);
    protected abstract void ClearCore(object element);

    // Method signatures only
    public abstract void TypeText(string? text, int? delayMs = null, int? timeoutMs = null);
    public abstract void AppendText(string? text, int? timeoutMs = null);
    public abstract string? GetPlaceholder(int? timeoutMs = null);
    public abstract int GetMaxLength(int? timeoutMs = null);
    public abstract int GetTextLength(int? timeoutMs = null);
    public abstract bool IsReadOnly(int? timeoutMs = null);
    public abstract bool WaitTextLength(int? expected, int? timeoutMs = null);
    public abstract void AssertTextLength(int? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertPlaceholder(string? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertReadOnly(bool? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 4. SearchControlBase (Abstract)

```csharp
namespace Brinell.Core;

/// <summary>
/// Base class for search input controls.
/// </summary>
public abstract class SearchControlBase : TextControlBase, ISearchControlObject
{
    protected SearchControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for Search with logging
    public virtual void Search(string? text, int? timeoutMs = null)
    {
        SetText(text, timeoutMs);
        Submit(timeoutMs);
        LogAction("Search", text);
    }

    // Method signatures only
    public abstract void Submit(int? timeoutMs = null);
    public abstract void ClearSearch(int? timeoutMs = null);
    public abstract IReadOnlyList<string> GetSuggestions(int? timeoutMs = null);
    public abstract void SelectSuggestion(int? index, int? timeoutMs = null);
    public abstract void SelectSuggestion(string? text, int? timeoutMs = null);
    public abstract bool HasSuggestions(int? timeoutMs = null);
    public abstract bool WaitSuggestions(bool? expected, int? timeoutMs = null);
    public abstract void AssertSuggestions(bool? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertSuggestionCount(int? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 5. MAUI Implementation

```csharp
namespace Brinell.Maui;

/// <summary>
/// MAUI button control implementation.
/// </summary>
public class MauiButton : MauiInteractiveControlBase, IButtonControlObject
{
    public MauiButton(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetLabel
    public string? GetLabel(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs) as AppiumElement;
        var label = element?.Text ?? element?.GetAttribute("content-desc");
        Log($"GetLabel: '{label}'");
        return label;
    }

    // Method signatures only
    public void ClickAndWait(int? waitMs = null, int? timeoutMs = null);
    public void ClickIfExists(int? timeoutMs = null);
    public void ClickIfEnabled(int? timeoutMs = null);
    public bool WaitLabel(string? expected, int? timeoutMs = null);
    public void AssertLabel(string? expected, string? message = null, int? timeoutMs = null);
}

/// <summary>
/// MAUI Entry (text input) control implementation.
/// </summary>
public class MauiEntry : MauiFocusableControlBase, IEditableTextControlObject
{
    public MauiEntry(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Full implementation for Enter
    public void Enter(string? text, int? timeoutMs = null)
    {
        if (text == null) return;
        
        EnsureEnabled(timeoutMs);
        var element = WaitForElementVisible(timeoutMs) as AppiumElement;
        if (element == null)
            ThrowCheckFailed("Enter", $"Element '{_locator}' not visible.");
        
        element!.SendKeys(text);
        LogAction("Enter", text);
    }

    // Full implementation for Clear
    public void Clear(int? timeoutMs = null)
    {
        EnsureEnabled(timeoutMs);
        var element = WaitForElementVisible(timeoutMs) as AppiumElement;
        if (element == null)
            ThrowCheckFailed("Clear", $"Element '{_locator}' not visible.");
        
        element!.Clear();
        LogAction("Clear");
    }

    // Full implementation for SetText
    public void SetText(string? text, int? timeoutMs = null)
    {
        if (text == null) return;
        Clear(timeoutMs);
        Enter(text, timeoutMs);
    }

    // Method signatures only
    public void TypeText(string? text, int? delayMs = null, int? timeoutMs = null);
    public void AppendText(string? text, int? timeoutMs = null);
    public string? GetPlaceholder(int? timeoutMs = null);
    public int GetMaxLength(int? timeoutMs = null);
    public int GetTextLength(int? timeoutMs = null);
    public bool IsReadOnly(int? timeoutMs = null);
    public bool WaitTextLength(int? expected, int? timeoutMs = null);
    public void AssertTextLength(int? expected, string? message = null, int? timeoutMs = null);
    public void AssertPlaceholder(string? expected, string? message = null, int? timeoutMs = null);
    public void AssertReadOnly(bool? expected, string? message = null, int? timeoutMs = null);
}

/// <summary>
/// MAUI Editor (multiline text) control implementation.
/// </summary>
public class MauiEditor : MauiEntry
{
    public MauiEditor(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }
}

/// <summary>
/// MAUI SearchBar control implementation.
/// </summary>
public class MauiSearchBar : MauiEntry, ISearchControlObject
{
    public MauiSearchBar(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Full implementation for Search
    public void Search(string? text, int? timeoutMs = null)
    {
        SetText(text, timeoutMs);
        Submit(timeoutMs);
        LogAction("Search", text);
    }

    // Full implementation for Submit
    public void Submit(int? timeoutMs = null)
    {
        EnsureEnabled(timeoutMs);
        
        // Press Enter/Search key on keyboard
        _mauiContext.Driver.PressKeyCode(AndroidKeyCode.Enter);
        LogAction("Submit");
    }

    // Method signatures only
    public void ClearSearch(int? timeoutMs = null);
    public IReadOnlyList<string> GetSuggestions(int? timeoutMs = null);
    public void SelectSuggestion(int? index, int? timeoutMs = null);
    public void SelectSuggestion(string? text, int? timeoutMs = null);
    public bool HasSuggestions(int? timeoutMs = null);
    public bool WaitSuggestions(bool? expected, int? timeoutMs = null);
    public void AssertSuggestions(bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertSuggestionCount(int? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 6. Blazor Implementation

```csharp
namespace Brinell.Blazor;

/// <summary>
/// Blazor button control implementation.
/// </summary>
public class BlazorButton : BlazorInteractiveControlBase, IButtonControlObject
{
    public BlazorButton(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetLabel
    public string? GetLabel(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var label = locator.InnerTextAsync().GetAwaiter().GetResult();
        Log($"GetLabel: '{label}'");
        return label;
    }

    // Method signatures only
    public void ClickAndWait(int? waitMs = null, int? timeoutMs = null);
    public void ClickIfExists(int? timeoutMs = null);
    public void ClickIfEnabled(int? timeoutMs = null);
    public bool WaitLabel(string? expected, int? timeoutMs = null);
    public void AssertLabel(string? expected, string? message = null, int? timeoutMs = null);
}

/// <summary>
/// Blazor link control implementation.
/// </summary>
public class BlazorLink : BlazorButton
{
    public BlazorLink(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetHref
    public string? GetHref(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var href = locator.GetAttributeAsync("href").GetAwaiter().GetResult();
        Log($"GetHref: '{href}'");
        return href;
    }

    // Method signatures only
    public bool WaitHref(string? expected, int? timeoutMs = null);
    public void AssertHref(string? expected, string? message = null, int? timeoutMs = null);
}

/// <summary>
/// Blazor text input control implementation.
/// </summary>
public class BlazorInput : BlazorFocusableControlBase, IEditableTextControlObject
{
    public BlazorInput(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Full implementation for Enter
    public void Enter(string? text, int? timeoutMs = null)
    {
        if (text == null) return;
        
        EnsureEnabled(timeoutMs);
        var locator = GetPlaywrightLocator(timeoutMs);
        locator.FillAsync(text).GetAwaiter().GetResult();
        LogAction("Enter", text);
    }

    // Full implementation for Clear
    public void Clear(int? timeoutMs = null)
    {
        EnsureEnabled(timeoutMs);
        var locator = GetPlaywrightLocator(timeoutMs);
        locator.ClearAsync().GetAwaiter().GetResult();
        LogAction("Clear");
    }

    // Full implementation for SetText
    public void SetText(string? text, int? timeoutMs = null)
    {
        if (text == null) return;
        Clear(timeoutMs);
        Enter(text, timeoutMs);
    }

    // Full implementation for GetText override
    public override string? GetText(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var text = locator.InputValueAsync().GetAwaiter().GetResult();
        Log($"GetText: '{text}'");
        return text;
    }

    // Method signatures only
    public void TypeText(string? text, int? delayMs = null, int? timeoutMs = null);
    public void AppendText(string? text, int? timeoutMs = null);
    public string? GetPlaceholder(int? timeoutMs = null);
    public int GetMaxLength(int? timeoutMs = null);
    public int GetTextLength(int? timeoutMs = null);
    public bool IsReadOnly(int? timeoutMs = null);
    public bool WaitTextLength(int? expected, int? timeoutMs = null);
    public void AssertTextLength(int? expected, string? message = null, int? timeoutMs = null);
    public void AssertPlaceholder(string? expected, string? message = null, int? timeoutMs = null);
    public void AssertReadOnly(bool? expected, string? message = null, int? timeoutMs = null);
}

/// <summary>
/// Blazor textarea control implementation.
/// </summary>
public class BlazorTextArea : BlazorInput
{
    public BlazorTextArea(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }
}

/// <summary>
/// Blazor search input control implementation.
/// </summary>
public class BlazorSearchInput : BlazorInput, ISearchControlObject
{
    public BlazorSearchInput(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Full implementation for Search
    public void Search(string? text, int? timeoutMs = null)
    {
        SetText(text, timeoutMs);
        Submit(timeoutMs);
        LogAction("Search", text);
    }

    // Full implementation for Submit
    public void Submit(int? timeoutMs = null)
    {
        EnsureEnabled(timeoutMs);
        var locator = GetPlaywrightLocator(timeoutMs);
        locator.PressAsync("Enter").GetAwaiter().GetResult();
        LogAction("Submit");
    }

    // Method signatures only
    public void ClearSearch(int? timeoutMs = null);
    public IReadOnlyList<string> GetSuggestions(int? timeoutMs = null);
    public void SelectSuggestion(int? index, int? timeoutMs = null);
    public void SelectSuggestion(string? text, int? timeoutMs = null);
    public bool HasSuggestions(int? timeoutMs = null);
    public bool WaitSuggestions(bool? expected, int? timeoutMs = null);
    public void AssertSuggestions(bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertSuggestionCount(int? expected, string? message = null, int? timeoutMs = null);
}
```

---

**Next:** [SPEC-006-002d: Toggle Classes](SPEC-006-002-CLASSES-TOGGLE.md)
