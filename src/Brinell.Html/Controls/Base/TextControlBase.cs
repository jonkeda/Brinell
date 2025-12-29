using OpenQA.Selenium;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Html.Infrastructure;

namespace Brinell.Html.Controls.Base;

/// <summary>
/// HTML/Selenium base class for text input controls (input, textarea).
/// </summary>
public abstract class TextControlBase : ControlBase, ITextControl
{
    protected TextControlBase(SeleniumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    protected TextControlBase(SeleniumTestContext context, string automationId)
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
    /// </summary>
    public virtual bool IsReadOnly()
    {
        var element = FindElement();
        if (element == null) return true;
        
        // Check readonly attribute
        var readonlyAttr = element.GetAttribute("readonly");
        if (!string.IsNullOrEmpty(readonlyAttr)) return true;
        
        // Also check disabled
        return !element.Enabled;
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
        if (element != null)
        {
            _context.ExecuteScript("arguments[0].focus();", element);
        }
    }
    
    /// <summary>
    /// Blur (unfocus) the input element.
    /// </summary>
    public virtual void Blur()
    {
        LogAction("Blur");
        var element = FindElement();
        if (element != null)
        {
            _context.ExecuteScript("arguments[0].blur();", element);
        }
    }
}
