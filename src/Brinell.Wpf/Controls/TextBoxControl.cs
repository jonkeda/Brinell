using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions.Controls;
using Brinell.Wpf.Controls.Base;
using Brinell.Wpf.Infrastructure;

namespace Brinell.Wpf.Controls;

/// <summary>
/// WPF TextBox control wrapper.
/// Uses WPF-specific TextControlBase for FlaUI integration.
/// </summary>
public class TextBoxControl : TextControlBase, ITextBox
{
    public TextBoxControl(FlaUITestContext context, PageBase? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <summary>
    /// Create a textbox control that searches within a container element.
    /// Use this for textboxes inside list items or repeated templates.
    /// </summary>
    public TextBoxControl(FlaUITestContext context, PageBase? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public TextBoxControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId)
    {
    }
}
