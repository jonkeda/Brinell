using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.WinForms.Controls.Base;
using Brinell.WinForms.Infrastructure;

namespace Brinell.WinForms.Controls;

public class TreeViewControl : ControlBase
{
    public TreeViewControl(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId) { }
    public TreeViewControl(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId) { }
    public TreeViewControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId) { }
}

public class SliderControl : ControlBase
{
    public SliderControl(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId) { }
    public SliderControl(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId) { }
    public SliderControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId) { }
}

public class MenuItemControl : ControlBase
{
    public MenuItemControl(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId) { }
    public MenuItemControl(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId) { }
    public MenuItemControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId) { }
}

public class PictureBoxControl : ControlBase
{
    public PictureBoxControl(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId) { }
    public PictureBoxControl(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId) { }
    public PictureBoxControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId) { }
}

public class MessageBoxDialog : ControlBase
{
    public MessageBoxDialog(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId) { }
    public MessageBoxDialog(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId) { }
    public MessageBoxDialog(FlaUITestContext context, string automationId)
        : base(context, null, automationId) { }
}
