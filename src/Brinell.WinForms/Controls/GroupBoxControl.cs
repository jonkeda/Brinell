using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.FlaUI;
using Brinell.FlaUI.Controls.Base;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms GroupBox control wrapper.
/// Provides a container for grouping related controls.
/// Can be used as a container for finding child controls.
/// </summary>
public class GroupBoxControl : ControlBase
{
    public GroupBoxControl(FlaUITestContext context, PageBase? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public GroupBoxControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId)
    {
    }

    /// <summary>
    /// Get the GroupBox header/title text.
    /// </summary>
    public override string GetText()
    {
        var element = FindElement();
        return element?.Name ?? string.Empty;
    }

    /// <summary>
    /// Get the GroupBox element to use as a container for child controls.
    /// </summary>
    public AutomationElement? GetContainer()
    {
        return FindElement();
    }

    /// <summary>
    /// Create a child control within this GroupBox.
    /// </summary>
    public TControl CreateChild<TControl>(string automationId) 
        where TControl : ControlBase
    {
        var container = GetContainer();
        if (container == null)
        {
            throw new InvalidOperationException($"GroupBox '{AutomationId}' not found, cannot create child control.");
        }
        
        // Use Activator to create the control with container
        var ctor = typeof(TControl).GetConstructor(new[] 
        { 
            typeof(FlaUITestContext), 
            typeof(PageBase), 
            typeof(AutomationElement), 
            typeof(string) 
        });
        
        if (ctor != null)
        {
            return (TControl)ctor.Invoke(new object?[] { _context, _page, container, automationId });
        }
        
        // Fallback: try constructor without container
        var fallbackCtor = typeof(TControl).GetConstructor(new[] 
        { 
            typeof(FlaUITestContext), 
            typeof(PageBase), 
            typeof(string) 
        });
        
        if (fallbackCtor != null)
        {
            return (TControl)fallbackCtor.Invoke(new object?[] { _context, _page, automationId });
        }
        
        throw new InvalidOperationException($"Cannot find suitable constructor for control type {typeof(TControl).Name}");
    }
}
