using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.ControlObject6.Context;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for expandable controls in MAUI (expanders, accordions).
/// </summary>
public abstract class ExpanderControlBase : ContainerControlBase, IExpandableControlObject
{
    /// <summary>
    /// Creates a new expandable control.
    /// </summary>
    protected ExpanderControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new expandable control using AutomationId.
    /// </summary>
    protected ExpanderControlBase(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// XPath pattern for finding the header element.
    /// </summary>
    protected virtual string HeaderXPath => ".//*[contains(@ClassName,'Header') or @AutomationId='Header']";

    #region Expanded State

    /// <inheritdoc/>
    public virtual bool IsExpanded(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var state = element.GetAttribute("ExpandCollapse.ExpandCollapseState");
        var expanded = state == "Expanded" || state == "1";
        Log($"IsExpanded: {expanded}");
        return expanded;
    }

    /// <inheritdoc/>
    public virtual bool WaitExpanded(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;

        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var deadline = DateTime.Now.AddMilliseconds(timeout);

        while (DateTime.Now < deadline)
        {
            if (IsExpanded(timeoutMs) == expected.Value)
                return true;

            Thread.Sleep(DefaultPollingIntervalMs);
        }

        return false;
    }

    /// <inheritdoc/>
    public virtual void AssertExpanded(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = IsExpanded(timeoutMs);
        if (actual != expected.Value)
        {
            var msg = message ?? $"Expected expander to be {(expected.Value ? "expanded" : "collapsed")} but was {(actual ? "expanded" : "collapsed")}";
            throw new AssertionException(msg, Locator.Value, "AssertExpanded");
        }
    }

    #endregion

    #region Expand/Collapse

    /// <inheritdoc/>
    public virtual void Expand(int? timeoutMs = null)
    {
        Log("Expand()");
        if (!IsExpanded(timeoutMs))
        {
            ClickHeader(timeoutMs);
            WaitExpanded(true, timeoutMs);
        }
    }

    /// <inheritdoc/>
    public virtual void Collapse(int? timeoutMs = null)
    {
        Log("Collapse()");
        if (IsExpanded(timeoutMs))
        {
            ClickHeader(timeoutMs);
            WaitExpanded(false, timeoutMs);
        }
    }

    /// <inheritdoc/>
    public virtual void Toggle(int? timeoutMs = null)
    {
        Log("Toggle()");
        ClickHeader(timeoutMs);
    }

    #endregion

    #region Header

    /// <inheritdoc/>
    public virtual string GetHeaderText(int? timeoutMs = null)
    {
        var header = FindHeaderElement(timeoutMs);
        var text = header?.Text ?? string.Empty;
        Log($"GetHeaderText: {text}");
        return text;
    }

    /// <summary>
    /// Clicks the header element to toggle expansion.
    /// </summary>
    protected virtual void ClickHeader(int? timeoutMs = null)
    {
        var header = FindHeaderElement(timeoutMs);
        if (header is not null)
        {
            header.Click();
        }
        else
        {
            FindElementRequired(timeoutMs).Click();
        }
    }

    /// <summary>
    /// Finds the header element.
    /// </summary>
    protected virtual AppiumElement? FindHeaderElement(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var headers = element.FindElements(OpenQA.Selenium.By.XPath(HeaderXPath));
        return headers.Count > 0 ? (AppiumElement)headers[0] : null;
    }

    #endregion
}
