using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.ControlObject6.Context;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for tab controls in MAUI.
/// </summary>
public abstract class TabControlBase : ControlObjectBase, ITabControlObject
{
    /// <summary>
    /// Creates a new tab control.
    /// </summary>
    protected TabControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new tab control using AutomationId.
    /// </summary>
    protected TabControlBase(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// XPath pattern for finding tab elements.
    /// </summary>
    protected virtual string TabXPath => ".//*[@ClassName='TabItem' or @ClassName='ShellTab' or contains(@ClassName,'Tab')]";

    #region Tab Count

    /// <inheritdoc/>
    public virtual int GetTabCount(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var tabs = element.FindElements(OpenQA.Selenium.By.XPath(TabXPath));
        Log($"GetTabCount: {tabs.Count}");
        return tabs.Count;
    }

    /// <inheritdoc/>
    public virtual void AssertTabCount(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetTabCount(timeoutMs);
        if (actual != expected.Value)
        {
            var msg = message ?? $"Expected tab count {expected} but was {actual}";
            throw new AssertionException(msg, Locator.Value, "AssertTabCount");
        }
    }

    /// <inheritdoc/>
    public virtual IReadOnlyList<string> GetTabNames(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var tabs = element.FindElements(OpenQA.Selenium.By.XPath(TabXPath));
        var names = tabs.Select(t => t.Text ?? ((AppiumElement)t).GetAttribute("Name") ?? string.Empty).ToList();
        Log($"GetTabNames: [{string.Join(", ", names)}]");
        return names.AsReadOnly();
    }

    #endregion

    #region Selected Tab

    /// <inheritdoc/>
    public virtual int GetSelectedTabIndex(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var tabs = element.FindElements(OpenQA.Selenium.By.XPath(TabXPath));

        for (int i = 0; i < tabs.Count; i++)
        {
            var tab = (AppiumElement)tabs[i];
            var isSelected = tab.GetAttribute("SelectionItem.IsSelected");
            if (isSelected == "True" || isSelected == "true")
            {
                Log($"GetSelectedTabIndex: {i}");
                return i;
            }
        }

        Log("GetSelectedTabIndex: -1 (none selected)");
        return -1;
    }

    /// <inheritdoc/>
    public virtual void AssertSelectedTabIndex(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetSelectedTabIndex(timeoutMs);
        if (actual != expected.Value)
        {
            var msg = message ?? $"Expected selected tab index {expected} but was {actual}";
            throw new AssertionException(msg, Locator.Value, "AssertSelectedTabIndex");
        }
    }

    /// <inheritdoc/>
    public virtual string? GetSelectedTabName(int? timeoutMs = null)
    {
        var index = GetSelectedTabIndex(timeoutMs);
        if (index < 0) return null;

        var names = GetTabNames(timeoutMs);
        return index < names.Count ? names[index] : null;
    }

    /// <inheritdoc/>
    public virtual void AssertSelectedTabName(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetSelectedTabName(timeoutMs);
        if (actual != expected)
        {
            var msg = message ?? $"Expected selected tab name '{expected}' but was '{actual}'";
            throw new AssertionException(msg, Locator.Value, "AssertSelectedTabName");
        }
    }

    #endregion

    #region Select Tab

    /// <inheritdoc/>
    public virtual void SelectTab(int? index, int? timeoutMs = null)
    {
        if (index is null) return;
        Log($"SelectTab({index})");

        var element = FindElementRequired(timeoutMs);
        var tabs = element.FindElements(OpenQA.Selenium.By.XPath(TabXPath));

        if (index.Value < 0 || index.Value >= tabs.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} out of range (0-{tabs.Count - 1})");

        tabs[index.Value].Click();
    }

    /// <inheritdoc/>
    public virtual void SelectTab(string? name, int? timeoutMs = null)
    {
        if (name is null) return;
        Log($"SelectTab(\"{name}\")");

        var names = GetTabNames(timeoutMs);
        var index = -1;
        for (int i = 0; i < names.Count; i++)
        {
            if (names[i] == name)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
            throw new Brinell.Core.Exceptions.ElementNotFoundException($"Tab with name '{name}' not found");

        SelectTab(index, timeoutMs);
    }

    /// <inheritdoc/>
    public virtual bool WaitTabSelected(int index, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var deadline = DateTime.Now.AddMilliseconds(timeout);

        while (DateTime.Now < deadline)
        {
            if (GetSelectedTabIndex(timeoutMs) == index)
                return true;

            Thread.Sleep(DefaultPollingIntervalMs);
        }

        return false;
    }

    #endregion
}
