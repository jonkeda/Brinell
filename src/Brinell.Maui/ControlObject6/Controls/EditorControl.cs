using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Editor/Multi-line TextBox control implementation for MAUI.
/// Inherits virtual text input capabilities from TextControlBase.
/// Unlike Entry, Editor supports multi-line text input.
/// </summary>
public class EditorControl : TextControlBase
{
    /// <summary>
    /// Creates a new editor control.
    /// </summary>
    public EditorControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new editor control using AutomationId.
    /// </summary>
    public EditorControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// Enters multi-line text by appending lines.
    /// </summary>
    /// <param name="lines">Lines to enter.</param>
    public virtual void EnterLines(params string[] lines)
    {
        var text = string.Join(Environment.NewLine, lines);
        Log($"EnterLines: {lines.Length} lines");
        Enter(text);
    }

    /// <summary>
    /// Appends a new line of text to the existing content.
    /// </summary>
    /// <param name="line">The line to append.</param>
    public virtual void AppendLine(string line)
    {
        Log($"AppendLine: {line}");
        var element = FindElementRequired();
        element.SendKeys(Environment.NewLine + line);
    }

    /// <summary>
    /// Gets the number of lines in the editor.
    /// </summary>
    /// <returns>Line count.</returns>
    public virtual int GetLineCount()
    {
        var text = GetText();
        if (string.IsNullOrEmpty(text))
            return 0;
        
        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        Log($"GetLineCount: {lines.Length}");
        return lines.Length;
    }

    /// <summary>
    /// Gets a specific line by index (0-based).
    /// </summary>
    /// <param name="index">The line index.</param>
    /// <returns>The line text, or null if index is out of range.</returns>
    public virtual string? GetLine(int index)
    {
        var text = GetText();
        if (string.IsNullOrEmpty(text))
            return null;
        
        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        if (index < 0 || index >= lines.Length)
            return null;
        
        Log($"GetLine({index}): {lines[index]}");
        return lines[index];
    }

    /// <summary>
    /// Gets all lines as an array.
    /// </summary>
    /// <returns>Array of lines.</returns>
    public virtual string[] GetLines()
    {
        var text = GetText();
        if (string.IsNullOrEmpty(text))
            return Array.Empty<string>();
        
        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        Log($"GetLines: {lines.Length} lines");
        return lines;
    }
}
