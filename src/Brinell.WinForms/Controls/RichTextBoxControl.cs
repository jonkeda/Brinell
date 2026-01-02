using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.WinForms.Controls.Base;
using Brinell.WinForms.Infrastructure;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms RichTextBox control wrapper.
/// Inherits from InputControlBase which provides Clear, AppendText, IsReadOnly, GetTextLength, and WaitForTextEquals.
/// Provides rich text-specific operations for formatted text handling.
/// </summary>
public class RichTextBoxControl : InputControlBase
{
    public RichTextBoxControl(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public RichTextBoxControl(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public RichTextBoxControl(FlaUITestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Set the rich text content.
    /// Note: Sets plain text. To preserve formatting, use SetRtf().
    /// </summary>
    public void SetContent(string text)
    {
        SetText(text);
    }

    /// <summary>
    /// Get all the text content (plain text view).
    /// </summary>
    public string GetContent()
    {
        return GetText();
    }

    /// <summary>
    /// Get the RTF (Rich Text Format) content.
    /// Useful when you need to verify formatting in the rich text box.
    /// </summary>
    public string GetRtf()
    {
        var element = FindElement();
        if (element == null) return string.Empty;

        var richTextBox = element.AsTextBox();
        if (richTextBox != null)
        {
            // RichTextBox typically exposes RTF content via a property
            // This is a simplified approach - actual RTF extraction may require additional patterns
            return richTextBox.Text ?? string.Empty;
        }

        return string.Empty;
    }

    /// <summary>
    /// Set the RTF (Rich Text Format) content.
    /// Allows setting pre-formatted rich text.
    /// </summary>
    public void SetRtf(string rtf)
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("SetRtf", $"Element '{AutomationId}' not visible for RTF input.");
        }

        var richTextBox = element!.AsTextBox();
        if (richTextBox != null)
        {
            richTextBox.Text = rtf;
            System.Threading.Thread.Sleep(50);
            LogAction("SetRtf");
        }
        else
        {
            ThrowCheckFailed("SetRtf", $"Element '{AutomationId}' is not a RichTextBox.");
        }
    }

    /// <summary>
    /// Get the number of lines in the rich text box.
    /// </summary>
    public int GetLineCount()
    {
        var text = GetText();
        if (string.IsNullOrEmpty(text)) return 0;
        
        return text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None).Length;
    }

    /// <summary>
    /// Get a specific line of text by line number (1-based).
    /// </summary>
    public string GetLine(int lineNumber)
    {
        var text = GetText();
        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
        
        if (lineNumber < 1 || lineNumber > lines.Length)
        {
            ThrowCheckFailed("GetLine", $"Line number {lineNumber} out of range (1-{lines.Length}).");
        }

        return lines[lineNumber - 1];
    }

    /// <summary>
    /// Find text within the rich text box.
    /// </summary>
    public bool ContainsText(string searchText)
    {
        var content = GetContent();
        return content.Contains(searchText, System.StringComparison.Ordinal);
    }

    /// <summary>
    /// Assert that the rich text box contains specific text.
    /// </summary>
    public void AssertContainsText(string expectedText)
    {
        if (!ContainsText(expectedText))
        {
            var actual = GetContent();
            ThrowAssertionFailed("ContainsText", actual, expectedText,
                $"RichTextBox '{AutomationId}' does not contain '{expectedText}'.");
        }
        LogAssertPass("ContainsText", GetContent(), expectedText);
    }

    /// <summary>
    /// Assert that the rich text box content equals expected text.
    /// </summary>
    public void AssertContentEquals(string expected)
    {
        AssertTextEquals(expected);
    }

    /// <summary>
    /// Assert that the line count equals expected.
    /// </summary>
    public void AssertLineCount(int expected)
    {
        var actual = GetLineCount();
        if (actual != expected)
        {
            ThrowAssertionFailed("LineCount", actual.ToString(), expected.ToString(),
                $"RichTextBox '{AutomationId}' has {actual} lines, expected {expected}.");
        }
        LogAssertPass("LineCount", actual.ToString(), expected.ToString());
    }
}
