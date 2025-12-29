using OpenQA.Selenium.Appium;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls.Base;

/// <summary>
/// MAUI base class for text input controls (Entry, Editor).
/// </summary>
public abstract class TextControlBase : ControlBase, ITextControl
{
    protected TextControlBase(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    protected TextControlBase(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Enter text into the control (appends to existing text).
    /// </summary>
    public virtual void Enter(string text)
    {
        LogAction("Enter", text);
        var element = WaitForElementVisible();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not visible for text entry.");
        element.SendKeys(text);
        
        // Hide keyboard on mobile
        _context.HideKeyboard();
    }

    /// <summary>
    /// Clear the control's text.
    /// </summary>
    public virtual void Clear()
    {
        LogAction("Clear");
        var element = FindElement();
        element?.Clear();
    }

    /// <summary>
    /// Clear and enter new text.
    /// </summary>
    public virtual void ClearAndEnter(string text)
    {
        LogAction("ClearAndEnter", text);
        var element = WaitForElementVisible();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not visible for text entry.");
        element.Clear();
        element.SendKeys(text);
        
        // Hide keyboard on mobile
        _context.HideKeyboard();
    }

    /// <summary>
    /// Set text (alias for ClearAndEnter for backward compatibility).
    /// </summary>
    public virtual void SetText(string text)
    {
        ClearAndEnter(text);
    }

    /// <summary>
    /// Append text to existing text.
    /// </summary>
    public virtual void Append(string text)
    {
        Enter(text);
    }

    /// <summary>
    /// Check if the control is read-only.
    /// Override in derived classes for specific behavior.
    /// </summary>
    public virtual bool IsReadOnly()
    {
        var element = FindElement();
        // Default: check if enabled (if not enabled, treat as read-only)
        return element == null || !element.Enabled;
    }

    /// <summary>
    /// Get the text length.
    /// </summary>
    public virtual int GetTextLength()
    {
        return GetText().Length;
    }
}
