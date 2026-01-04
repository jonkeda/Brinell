using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.ControlObject6.Context;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for toolbar controls in MAUI.
/// </summary>
public abstract class ToolbarControlBase : ControlObjectBase, IToolbarControlObject
{
    /// <summary>
    /// Creates a new toolbar control.
    /// </summary>
    protected ToolbarControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new toolbar control using AutomationId.
    /// </summary>
    protected ToolbarControlBase(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// XPath pattern for finding toolbar items.
    /// </summary>
    protected virtual string ToolbarItemXPath => ".//*[@ClassName='ToolbarItem' or @ClassName='Button' or contains(@ClassName,'Toolbar')]";

    #region Toolbar Items

    /// <inheritdoc/>
    public virtual int GetToolbarItemCount(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var items = element.FindElements(OpenQA.Selenium.By.XPath(ToolbarItemXPath));
        Log($"GetToolbarItemCount: {items.Count}");
        return items.Count;
    }

    /// <inheritdoc/>
    public virtual IReadOnlyList<string> GetToolbarItemNames(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var items = element.FindElements(OpenQA.Selenium.By.XPath(ToolbarItemXPath));
        var names = items.Select(i => i.Text ?? ((AppiumElement)i).GetAttribute("Name") ?? string.Empty).ToList();
        Log($"GetToolbarItemNames: [{string.Join(", ", names)}]");
        return names.AsReadOnly();
    }

    /// <inheritdoc/>
    public virtual bool HasToolbarItem(string name, int? timeoutMs = null)
    {
        var names = GetToolbarItemNames(timeoutMs);
        return names.Contains(name);
    }

    #endregion

    #region Click Toolbar Item

    /// <inheritdoc/>
    public virtual void ClickToolbarItem(string? name, int? timeoutMs = null)
    {
        if (name is null) return;
        Log($"ClickToolbarItem(\"{name}\")");

        var element = FindElementRequired(timeoutMs);
        var item = element.FindElement(OpenQA.Selenium.By.XPath($".//*[(@ClassName='ToolbarItem' or @ClassName='Button') and (@Name='{name}' or @AutomationId='{name}')]"));
        item.Click();
    }

    /// <inheritdoc/>
    public virtual void ClickToolbarItem(int? index, int? timeoutMs = null)
    {
        if (index is null) return;
        Log($"ClickToolbarItem({index})");

        var element = FindElementRequired(timeoutMs);
        var items = element.FindElements(OpenQA.Selenium.By.XPath(ToolbarItemXPath));

        if (index.Value < 0 || index.Value >= items.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} out of range (0-{items.Count - 1})");

        items[index.Value].Click();
    }

    #endregion

    #region Toolbar Item State

    /// <inheritdoc/>
    public virtual bool IsToolbarItemEnabled(string name, int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var item = element.FindElement(OpenQA.Selenium.By.XPath($".//*[(@ClassName='ToolbarItem' or @ClassName='Button') and (@Name='{name}' or @AutomationId='{name}')]"));
        return item.Enabled;
    }

    /// <inheritdoc/>
    public virtual void AssertToolbarItemEnabled(string name, bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = IsToolbarItemEnabled(name, timeoutMs);
        if (actual != expected.Value)
        {
            var msg = message ?? $"Expected toolbar item '{name}' to be {(expected.Value ? "enabled" : "disabled")} but was {(actual ? "enabled" : "disabled")}";
            throw new AssertionException(msg, Locator.Value, "AssertToolbarItemEnabled");
        }
    }

    #endregion
}
