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

    /// <summary>
    /// Get the placeholder text.
    /// </summary>
    public virtual string? GetPlaceholder()
    {
        var element = FindElement();
        if (element == null) return null;
        
        // Try different attribute names used by different platforms
        return element.GetAttribute("placeholder") 
            ?? element.GetAttribute("hint")
            ?? element.GetAttribute("hintText");
    }

    #region Assert Methods

    /// <summary>
    /// Assert the control is read-only.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertIsReadOnly(string? message = null)
    {
        CheckVisible(expected: true);
        if (!IsReadOnly())
        {
            ThrowAssertionFailed("IsReadOnly", "false", "true",
                message ?? $"Expected element '{AutomationId}' to be read-only but it is editable.");
        }
        LogAssertPass("IsReadOnly", "true", "true");
    }

    /// <summary>
    /// Assert the control is not read-only (editable).
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertIsNotReadOnly(string? message = null)
    {
        CheckVisible(expected: true);
        if (IsReadOnly())
        {
            ThrowAssertionFailed("IsNotReadOnly", "true", "false",
                message ?? $"Expected element '{AutomationId}' to be editable but it is read-only.");
        }
        LogAssertPass("IsNotReadOnly", "false", "false");
    }

    /// <summary>
    /// Assert the placeholder text equals expected.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertPlaceholder(string expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetPlaceholder();
        if (actual != expected)
        {
            ThrowAssertionFailed("Placeholder", actual ?? "(null)", expected,
                message ?? $"Expected placeholder '{expected}' but got '{actual}' for element '{AutomationId}'.");
        }
        LogAssertPass("Placeholder", actual ?? "(null)", expected);
    }

    /// <summary>
    /// Assert the placeholder contains expected text.
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertPlaceholderContains(string expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetPlaceholder() ?? string.Empty;
        if (!actual.Contains(expected))
        {
            ThrowAssertionFailed("PlaceholderContains", actual, $"contains '{expected}'",
                message ?? $"Expected placeholder to contain '{expected}' but got '{actual}' for element '{AutomationId}'.");
        }
        LogAssertPass("PlaceholderContains", actual, expected);
    }

    #endregion
}
