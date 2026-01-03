using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions.Controls;
using Brinell.FlaUI;
using Brinell.FlaUI.Controls.Base;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms Label control wrapper.
/// Uses shared ContentControlBase (read-only content display).
/// </summary>
public class LabelControl : ContentControlBase, ILabel
{
    public LabelControl(FlaUITestContext context, PageBase? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <summary>
    /// Create a label control that searches within a container element.
    /// Use this for labels inside list items or repeated templates.
    /// </summary>
    public LabelControl(FlaUITestContext context, PageBase? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public LabelControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId)
    {
    }

    /// <summary>
    /// Get label text.
    /// </summary>
    public override string GetText()
    {
        var element = FindElement();
        if (element != null)
        {
            var label = element.AsLabel();
            return label?.Text ?? element.Name ?? string.Empty;
        }
        return string.Empty;
    }

    /// <summary>
    /// Wait for text to equal expected value.
    /// </summary>
    public bool WaitForText(string expected, int? timeoutMs = null)
    {
        var sw = Stopwatch.StartNew();
        var result = _context.WaitFor(
            () => GetText() == expected,
            timeoutMs,
            $"text = '{expected}'");
        LogWait($"Text={expected}", result, (int)sw.ElapsedMilliseconds);
        return result;
    }

    /// <summary>
    /// Wait for text to contain expected value.
    /// </summary>
    public bool WaitForTextContains(string expected, int? timeoutMs = null)
    {
        var sw = Stopwatch.StartNew();
        var result = _context.WaitFor(
            () => GetText().Contains(expected),
            timeoutMs,
            $"text contains '{expected}'");
        LogWait($"TextContains={expected}", result, (int)sw.ElapsedMilliseconds);
        return result;
    }
}
