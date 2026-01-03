using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions.Controls;
using Brinell.FlaUI;
using Brinell.FlaUI.Controls.Base;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms TabControl control wrapper.
/// Provides tab navigation and selection.
/// </summary>
public class TabControlControl : ItemsControlBase, IItemsControl
{
    public TabControlControl(FlaUITestContext context, PageBase? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public TabControlControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId)
    {
    }

    /// <summary>
    /// Get tab item elements.
    /// </summary>
    protected override AutomationElement[] GetItemElements()
    {
        var element = FindElement();
        if (element != null)
        {
            var tab = element.AsTab();
            return tab?.TabItems.Cast<AutomationElement>().ToArray() ?? Array.Empty<AutomationElement>();
        }
        return Array.Empty<AutomationElement>();
    }

    /// <summary>
    /// Get selected tab text (immediate, no wait).
    /// </summary>
    public virtual string? GetSelectedTabText()
    {
        var element = FindElement();
        if (element != null)
        {
            var tab = element.AsTab();
            return tab?.SelectedTabItem?.Name;
        }
        return null;
    }

    /// <summary>
    /// Get selected tab index (immediate, no wait). Returns -1 if none selected.
    /// </summary>
    public virtual int GetSelectedTabIndex()
    {
        var element = FindElement();
        if (element != null)
        {
            var tab = element.AsTab();
            return tab?.SelectedTabItemIndex ?? -1;
        }
        return -1;
    }

    /// <summary>
    /// Select tab by index.
    /// </summary>
    public virtual void SelectTabByIndex(int index)
    {
        CheckVisible();
        
        var element = FindElement();
        if (element != null)
        {
            var tab = element.AsTab();
            if (tab != null && index < tab.TabItems.Length)
            {
                tab.SelectTabItem(index);
                LogAction("SelectTabByIndex", index.ToString());
            }
        }
    }

    /// <summary>
    /// Select tab by text.
    /// </summary>
    public virtual void SelectTabByText(string text)
    {
        CheckVisible();
        
        var element = FindElement();
        if (element != null)
        {
            var tab = element.AsTab();
            var tabItem = tab?.TabItems.FirstOrDefault(t => t.Name == text);
            if (tabItem != null)
            {
                tabItem.Select();
                LogAction("SelectTabByText", text);
            }
        }
    }

    /// <summary>
    /// Get all tab texts.
    /// </summary>
    public virtual string[] GetTabTexts()
    {
        var element = FindElement();
        if (element != null)
        {
            var tab = element.AsTab();
            return tab?.TabItems.Select(t => t.Name ?? "").ToArray() ?? Array.Empty<string>();
        }
        return Array.Empty<string>();
    }

    /// <summary>
    /// Get tab count.
    /// </summary>
    public virtual int GetTabCount()
    {
        var element = FindElement();
        if (element != null)
        {
            var tab = element.AsTab();
            return tab?.TabItems.Length ?? 0;
        }
        return 0;
    }

    /// <summary>
    /// Wait for tab to be selected.
    /// </summary>
    public bool WaitForTab(string tabText, int? timeoutMs = null)
    {
        var sw = Stopwatch.StartNew();
        var result = _context.WaitFor(
            () => GetSelectedTabText() == tabText,
            timeoutMs,
            $"tab = '{tabText}'");
        LogWait($"Tab={tabText}", result, (int)sw.ElapsedMilliseconds);
        return result;
    }

    /// <summary>
    /// Get selected tab text.
    /// </summary>
    public override string GetText()
    {
        return GetSelectedTabText() ?? string.Empty;
    }
}
