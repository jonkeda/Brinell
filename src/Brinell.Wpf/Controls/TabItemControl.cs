using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Brinell.Wpf.Controls.Base;
using Brinell.Wpf.Infrastructure;

namespace Brinell.Wpf.Controls;

/// <summary>
/// WPF TabItem control wrapper.
/// TabItems use SelectionItemPattern, not InvokePattern like buttons.
/// </summary>
public class TabItemControl : ControlBase
{
    public TabItemControl(FlaUITestContext context, PageBase? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public TabItemControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId)
    {
    }

    /// <summary>
    /// Select this tab item using SelectionItemPattern or mouse click.
    /// </summary>
    public void Select()
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("Select", $"TabItem '{AutomationId}' not visible.");
            return;
        }
        
        // Try SelectionItemPattern first (proper way to select TabItems)
        var tabItem = element!.AsTabItem();
        if (tabItem != null)
        {
            try
            {
                // Use SelectionItemPattern to select the tab
                if (tabItem.Patterns.SelectionItem.IsSupported)
                {
                    tabItem.Patterns.SelectionItem.Pattern.Select();
                    LogAction("Select", "SelectionItemPattern");
                    return;
                }
            }
            catch
            {
                // Fall through to mouse click
            }
        }
        
        // Fallback to mouse click
        element!.Click();
        LogAction("Select", "MouseClick");
    }

    /// <summary>
    /// Click is an alias for Select on TabItems.
    /// </summary>
    public void Click()
    {
        Select();
    }

    /// <summary>
    /// Check if this tab is currently selected.
    /// </summary>
    public bool IsSelected()
    {
        var element = FindElement();
        if (element == null)
        {
            return false;
        }
        
        var tabItem = element.AsTabItem();
        if (tabItem != null)
        {
            return tabItem.IsSelected;
        }
        
        return false;
    }

    /// <summary>
    /// Get the tab header text.
    /// </summary>
    public override string GetText()
    {
        var element = FindElement();
        if (element != null)
        {
            var tabItem = element.AsTabItem();
            return tabItem?.Name ?? element.Name ?? string.Empty;
        }
        return string.Empty;
    }

    /// <summary>
    /// Assert that this tab is selected.
    /// </summary>
    public void AssertSelected(string? message = null)
    {
        CheckVisible(expected: true);
        if (!IsSelected())
        {
            ThrowAssertionFailed("Selected", "false", "true",
                message ?? $"Expected tab '{AutomationId}' to be selected but it is not.");
        }
        LogAssertPass("Selected", "true", "true");
    }

    /// <summary>
    /// Assert that this tab is not selected.
    /// </summary>
    public void AssertNotSelected(string? message = null)
    {
        CheckVisible(expected: true);
        if (IsSelected())
        {
            ThrowAssertionFailed("NotSelected", "true", "false",
                message ?? $"Expected tab '{AutomationId}' to not be selected but it is.");
        }
        LogAssertPass("NotSelected", "false", "false");
    }
}
