using Microsoft.Playwright;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls.Base;

/// <summary>
/// Playwright base class for text input controls (input, textarea).
/// </summary>
public abstract class TextControlBase : ControlBase, ITextControl
{
    protected TextControlBase(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    protected TextControlBase(PlaywrightTestContext context, IPageObject? page, ILocator? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    protected TextControlBase(PlaywrightTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Enter text into the control (appends to existing text using PressSequentially).
    /// </summary>
    public virtual void Enter(string text)
    {
        LogAction("Enter", text);
        var element = FindElement();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not visible for text entry.");
        element.PressSequentiallyAsync(text).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Enter text asynchronously.
    /// </summary>
    public virtual async Task EnterAsync(string text)
    {
        LogAction("Enter", text);
        var element = await FindElementAsync();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not visible for text entry.");
        await element.PressSequentiallyAsync(text);
    }

    /// <summary>
    /// Clear the control's text.
    /// </summary>
    public virtual void Clear()
    {
        LogAction("Clear");
        var element = FindElement();
        element?.ClearAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Clear the control's text asynchronously.
    /// </summary>
    public virtual async Task ClearAsync()
    {
        LogAction("Clear");
        var element = await FindElementAsync();
        if (element != null)
        {
            await element.ClearAsync();
        }
    }

    /// <summary>
    /// Clear and enter new text (using Playwright's Fill method).
    /// </summary>
    public virtual void ClearAndEnter(string text)
    {
        LogAction("ClearAndEnter", text);
        var element = FindElement();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not visible for text entry.");
        element.FillAsync(text).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Clear and enter new text asynchronously (using Playwright's Fill method).
    /// </summary>
    public virtual async Task ClearAndEnterAsync(string text)
    {
        LogAction("ClearAndEnter", text);
        var element = await FindElementAsync();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not visible for text entry.");
        await element.FillAsync(text);
    }

    /// <summary>
    /// Set text (alias for ClearAndEnter for backward compatibility).
    /// </summary>
    public virtual void SetText(string text)
    {
        ClearAndEnter(text);
    }

    /// <summary>
    /// Set text asynchronously (alias for ClearAndEnterAsync).
    /// </summary>
    public virtual Task SetTextAsync(string text)
    {
        return ClearAndEnterAsync(text);
    }

    /// <summary>
    /// Append text to existing text.
    /// </summary>
    public virtual void Append(string text)
    {
        Enter(text);
    }

    /// <summary>
    /// Append text asynchronously.
    /// </summary>
    public virtual Task AppendAsync(string text)
    {
        return EnterAsync(text);
    }

    /// <summary>
    /// Check if the control is read-only.
    /// </summary>
    public virtual bool IsReadOnly()
    {
        var element = FindElement();
        if (element == null) return true;

        // Check readonly attribute
        var readonlyAttr = element.GetAttributeAsync("readonly").GetAwaiter().GetResult();
        if (!string.IsNullOrEmpty(readonlyAttr)) return true;

        // Also check disabled
        return !element.IsEnabledAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Check if the control is read-only asynchronously.
    /// </summary>
    public virtual async Task<bool> IsReadOnlyAsync()
    {
        var element = await FindElementAsync();
        if (element == null) return true;

        var readonlyAttr = await element.GetAttributeAsync("readonly");
        if (!string.IsNullOrEmpty(readonlyAttr)) return true;

        return !await element.IsEnabledAsync();
    }

    /// <summary>
    /// Get the text length.
    /// </summary>
    public virtual int GetTextLength()
    {
        return GetText().Length;
    }

    /// <summary>
    /// Get the placeholder text.
    /// </summary>
    public virtual string? GetPlaceholder()
    {
        return GetAttribute("placeholder");
    }

    /// <summary>
    /// Focus the input element.
    /// </summary>
    public virtual void Focus()
    {
        LogAction("Focus");
        var element = FindElement();
        element?.FocusAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Focus the input element asynchronously.
    /// </summary>
    public virtual async Task FocusAsync()
    {
        LogAction("Focus");
        var element = await FindElementAsync();
        if (element != null)
        {
            await element.FocusAsync();
        }
    }

    /// <summary>
    /// Blur (unfocus) the input element.
    /// </summary>
    public virtual void Blur()
    {
        LogAction("Blur");
        var element = FindElement();
        element?.BlurAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Blur (unfocus) the input element asynchronously.
    /// </summary>
    public virtual async Task BlurAsync()
    {
        LogAction("Blur");
        var element = await FindElementAsync();
        if (element != null)
        {
            await element.BlurAsync();
        }
    }
}
