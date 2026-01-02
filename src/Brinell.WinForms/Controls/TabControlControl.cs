using System.Collections.Generic;
using System.Linq;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Brinell.Core.Abstractions;
using Brinell.WinForms.Controls.Base;
using Brinell.WinForms.Infrastructure;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms TabControl control wrapper.
/// Provides tab navigation and selection operations.
/// </summary>
public class TabControlControl : ControlBase
{
    public TabControlControl(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public TabControlControl(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public TabControlControl(FlaUITestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the number of tabs in the control.
    /// </summary>
    public int GetTabCount()
    {
        var element = FindElement();
        if (element == null)
        {
            ThrowCheckFailed("GetTabCount", $"Element '{AutomationId}' not found.");
        }

        try
        {
            var tabs = element!.FindAllChildren(cf => cf.ByControlType(ControlType.TabItem)).ToList();
            var count = tabs.Count();
            LogAction("GetTabCount", count.ToString());
            return count;
        }
        catch (Exception ex)
        {
            ThrowCheckFailed("GetTabCount", $"Failed to get tab count: {ex.Message}");
        }

        return 0;
    }

    /// <summary>
    /// Get tab names/headers.
    /// </summary>
    public List<string> GetTabNames()
    {
        var element = FindElement();
        if (element == null)
        {
            ThrowCheckFailed("GetTabNames", $"Element '{AutomationId}' not found.");
        }

        var names = new List<string>();
        try
        {
            var tabs = element!.FindAllChildren(cf => cf.ByControlType(ControlType.TabItem)).ToList();
            foreach (var tab in tabs)
            {
                var name = tab.Name ?? $"Tab {names.Count + 1}";
                names.Add(name);
            }
            LogAction("GetTabNames", $"{names.Count} tabs");
        }
        catch (Exception ex)
        {
            ThrowCheckFailed("GetTabNames", $"Failed to get tab names: {ex.Message}");
        }

        return names;
    }

    /// <summary>
    /// Select a tab by index (0-based).
    /// </summary>
    public void SelectTab(int index)
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("SelectTab", $"Element '{AutomationId}' not visible.");
        }

        try
        {
            var tabs = element!.FindAllChildren(cf => cf.ByControlType(ControlType.TabItem)).ToList();
            if (index < 0 || index >= tabs.Count)
            {
                ThrowCheckFailed("SelectTab", $"Tab index {index} out of range (0-{tabs.Count - 1}).");
            }

            tabs[index].Click();
            System.Threading.Thread.Sleep(100);
            LogAction("SelectTab", index.ToString());
        }
        catch (Exception ex)
        {
            ThrowCheckFailed("SelectTab", $"Failed to select tab {index}: {ex.Message}");
        }
    }

    /// <summary>
    /// Select a tab by name/header text.
    /// </summary>
    public void SelectTabByName(string tabName)
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("SelectTabByName", $"Element '{AutomationId}' not visible.");
        }

        try
        {
            var tabs = element!.FindAllChildren(cf => cf.ByControlType(ControlType.TabItem)).ToList();
            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i].Name == tabName)
                {
                    tabs[i].Click();
                    System.Threading.Thread.Sleep(100);
                    LogAction("SelectTabByName", tabName);
                    return;
                }
            }
            ThrowCheckFailed("SelectTabByName", $"Tab '{tabName}' not found.");
        }
        catch (Exception ex)
        {
            ThrowCheckFailed("SelectTabByName", $"Failed to select tab '{tabName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Get the currently selected tab index.
    /// </summary>
    public int GetSelectedTabIndex()
    {
        var element = FindElement();
        if (element == null)
        {
            ThrowCheckFailed("GetSelectedTabIndex", $"Element '{AutomationId}' not found.");
        }

        try
        {
            var tabs = element!.FindAllChildren(cf => cf.ByControlType(ControlType.TabItem)).ToList();
            for (int i = 0; i < tabs.Count; i++)
            {
                var selectionPattern = tabs[i].Patterns.SelectionItem.PatternOrDefault;
                if (selectionPattern != null && selectionPattern.IsSelected)
                {
                    LogAction("GetSelectedTabIndex", i.ToString());
                    return i;
                }
            }
        }
        catch (Exception ex)
        {
            LogDebug($"Failed to get selected tab index: {ex.Message}");
        }

        return -1;
    }

    /// <summary>
    /// Get the currently selected tab name.
    /// </summary>
    public string GetSelectedTabName()
    {
        var element = FindElement();
        if (element == null)
        {
            ThrowCheckFailed("GetSelectedTabName", $"Element '{AutomationId}' not found.");
        }

        try
        {
            var tabs = element!.FindAllChildren(cf => cf.ByControlType(ControlType.TabItem)).ToList();
            for (int i = 0; i < tabs.Count; i++)
            {
                var selectionPattern = tabs[i].Patterns.SelectionItem.PatternOrDefault;
                if (selectionPattern != null && selectionPattern.IsSelected)
                {
                    var name = tabs[i].Name ?? $"Tab {i}";
                    LogAction("GetSelectedTabName", name);
                    return name;
                }
            }
        }
        catch (Exception ex)
        {
            ThrowCheckFailed("GetSelectedTabName", $"Failed to get selected tab name: {ex.Message}");
        }

        return string.Empty;
    }

    /// <summary>
    /// Assert that the selected tab index matches expected.
    /// </summary>
    public void AssertSelectedTabIs(int expectedIndex)
    {
        var actual = GetSelectedTabIndex();
        if (actual != expectedIndex)
        {
            ThrowAssertionFailed("SelectedTabIs", actual.ToString(), expectedIndex.ToString(),
                $"TabControl '{AutomationId}' selected tab is {actual}, expected {expectedIndex}.");
        }
        LogAssertPass("SelectedTabIs", actual.ToString(), expectedIndex.ToString());
    }

    /// <summary>
    /// Assert that the selected tab name matches expected.
    /// </summary>
    public void AssertSelectedTabNameIs(string expectedName)
    {
        var actual = GetSelectedTabName();
        if (actual != expectedName)
        {
            ThrowAssertionFailed("SelectedTabNameIs", actual, expectedName,
                $"TabControl '{AutomationId}' selected tab is '{actual}', expected '{expectedName}'.");
        }
        LogAssertPass("SelectedTabNameIs", actual, expectedName);
    }

    /// <summary>
    /// Assert that the tab count matches expected.
    /// </summary>
    public void AssertTabCount(int expected)
    {
        var actual = GetTabCount();
        if (actual != expected)
        {
            ThrowAssertionFailed("TabCount", actual.ToString(), expected.ToString(),
                $"TabControl '{AutomationId}' has {actual} tabs, expected {expected}.");
        }
        LogAssertPass("TabCount", actual.ToString(), expected.ToString());
    }

    /// <summary>
    /// Check if a tab exists by name.
    /// </summary>
    public bool TabExists(string tabName)
    {
        var names = GetTabNames();
        var exists = names.Contains(tabName);
        LogAction("TabExists", $"{tabName}: {exists}");
        return exists;
    }
}
