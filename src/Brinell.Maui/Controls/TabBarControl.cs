using OpenQA.Selenium;
using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI TabBar control wrapper.
/// Provides access to bottom tab navigation.
/// </summary>
public class TabBarControl : ControlBase
{
    public TabBarControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public TabBarControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the currently selected tab index.
    /// </summary>
    public int GetSelectedIndex()
    {
        var element = FindElement();
        if (element == null) return -1;
        
        var index = element.GetAttribute("selectedIndex") ?? element.GetAttribute("selected_tab");
        if (int.TryParse(index, out var result))
            return result;
        
        return -1;
    }

    /// <summary>
    /// Get the currently selected tab title.
    /// </summary>
    public string? GetSelectedTabTitle()
    {
        var element = FindElement();
        if (element == null) return null;
        
        return element.GetAttribute("selectedTab") ?? element.GetAttribute("selected_tab_title");
    }

    /// <summary>
    /// Select a tab by index.
    /// </summary>
    /// <param name="index">Zero-based index of the tab.</param>
    public void SelectTab(int index)
    {
        LogAction("SelectTab", index.ToString());
        var tabs = FindTabs();
        if (index >= 0 && index < tabs.Count)
        {
            tabs[index].Click();
        }
        else
        {
            throw new InvalidOperationException($"Tab index {index} is out of range. Found {tabs.Count} tabs.");
        }
    }

    /// <summary>
    /// Select a tab by title.
    /// </summary>
    /// <param name="title">The title of the tab to select.</param>
    public void SelectTab(string title)
    {
        LogAction("SelectTab", title);
        
        var tabItem = _context.Driver.Driver.FindElements(
            By.XPath($"//*[@text='{title}' or @name='{title}' or @content-desc='{title}']"))
            .FirstOrDefault();
        
        if (tabItem != null)
        {
            tabItem.Click();
        }
        else
        {
            throw new InvalidOperationException($"Tab with title '{title}' not found.");
        }
    }

    /// <summary>
    /// Get the number of tabs.
    /// </summary>
    public int GetTabCount()
    {
        return FindTabs().Count;
    }

    private IReadOnlyList<OpenQA.Selenium.Appium.AppiumElement> FindTabs()
    {
        var element = FindElement();
        if (element == null) return Array.Empty<OpenQA.Selenium.Appium.AppiumElement>();
        
        // Find child tab items
        var tabs = element.FindElements(By.XPath(".//*[@clickable='true']"));
        return tabs.Cast<OpenQA.Selenium.Appium.AppiumElement>().ToList();
    }

    #region Assert Methods

    /// <summary>
    /// Assert the selected tab index.
    /// </summary>
    public void AssertSelectedIndex(int expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetSelectedIndex();
        if (actual != expected)
        {
            ThrowAssertionFailed("SelectedIndex", actual.ToString(), expected.ToString(),
                message ?? $"Expected tab index {expected} but got {actual}.");
        }
        LogAssertPass("SelectedIndex", actual.ToString(), expected.ToString());
    }

    /// <summary>
    /// Assert the tab count.
    /// </summary>
    public void AssertTabCount(int expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetTabCount();
        if (actual != expected)
        {
            ThrowAssertionFailed("TabCount", actual.ToString(), expected.ToString(),
                message ?? $"Expected {expected} tabs but found {actual}.");
        }
        LogAssertPass("TabCount", actual.ToString(), expected.ToString());
    }

    #endregion
}
