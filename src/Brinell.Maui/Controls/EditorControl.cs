using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI Editor control wrapper.
/// Provides multi-line text editing functionality.
/// </summary>
public class EditorControl : TextControlBase
{
    public EditorControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public EditorControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the number of lines in the editor.
    /// </summary>
    public int GetLineCount()
    {
        var text = GetText();
        if (string.IsNullOrEmpty(text)) return 0;
        return text.Split('\n').Length;
    }

    /// <summary>
    /// Append text without clearing existing content.
    /// </summary>
    /// <param name="text">Text to append.</param>
    public void AppendText(string text)
    {
        LogAction("AppendText", text);
        var element = WaitForElementVisible();
        if (element == null)
            throw new InvalidOperationException($"Editor '{AutomationId}' not visible.");
        
        element.SendKeys(text);
    }

    /// <summary>
    /// Insert a new line and text.
    /// </summary>
    /// <param name="text">Text to add on new line.</param>
    public void AddLine(string text)
    {
        LogAction("AddLine", text);
        AppendText("\n" + text);
    }

    /// <summary>
    /// Get maximum allowed length.
    /// </summary>
    public int GetMaxLength()
    {
        var element = FindElement();
        if (element != null)
        {
            var maxLength = element.GetAttribute("maxLength");
            if (int.TryParse(maxLength, out var result))
                return result;
        }
        return int.MaxValue;
    }

    /// <summary>
    /// Check if auto-size is enabled.
    /// </summary>
    public bool IsAutoSizeEnabled()
    {
        var element = FindElement();
        var autoSize = element?.GetAttribute("autoSize");
        return autoSize?.Equals("TextChanges", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    #region Assert Methods

    /// <summary>
    /// Assert the line count.
    /// </summary>
    public void AssertLineCount(int expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetLineCount();
        if (actual != expected)
        {
            ThrowAssertionFailed("LineCount", actual.ToString(), expected.ToString(),
                message ?? $"Expected {expected} lines but got {actual}.");
        }
        LogAssertPass("LineCount", actual.ToString(), expected.ToString());
    }

    /// <summary>
    /// Assert minimum line count.
    /// </summary>
    public void AssertMinLineCount(int minimum, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetLineCount();
        if (actual < minimum)
        {
            ThrowAssertionFailed("MinLineCount", actual.ToString(), $">={minimum}",
                message ?? $"Expected at least {minimum} lines but got {actual}.");
        }
        LogAssertPass("MinLineCount", actual.ToString(), $">={minimum}");
    }

    #endregion
}
