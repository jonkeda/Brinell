using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.ControlObject6.Context;
using By = Brinell.Core.ControlObject6.Locators.By;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for container controls in MAUI.
/// Provides common functionality for controls that contain child controls.
/// </summary>
public abstract class ContainerControlBase : ControlObjectBase, IContainerControlObject
{
    /// <summary>
    /// Creates a new container control.
    /// </summary>
    protected ContainerControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new container control using AutomationId.
    /// </summary>
    protected ContainerControlBase(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    #region Child Count

    /// <inheritdoc/>
    public virtual int GetChildCount(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var children = element.FindElements(OpenQA.Selenium.By.XPath("./*"));
        Log($"GetChildCount: {children.Count}");
        return children.Count;
    }

    /// <inheritdoc/>
    public virtual void AssertChildCount(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetChildCount(timeoutMs);
        if (actual != expected.Value)
        {
            var msg = message ?? $"Expected child count {expected} but was {actual}";
            throw new AssertionException(msg, Locator.Value, "AssertChildCount");
        }
    }

    #endregion

    #region Find Child

    /// <inheritdoc/>
    public virtual T FindChild<T>(ControlLocator locator) where T : IControlObject
    {
        return (T)Activator.CreateInstance(typeof(T), Context, locator, Page)!;
    }

    /// <inheritdoc/>
    public virtual T FindChild<T>(string automationId) where T : IControlObject
    {
        var locator = By.AutomationId(automationId);
        return FindChild<T>(locator);
    }

    /// <inheritdoc/>
    public virtual IReadOnlyList<T> FindChildren<T>(ControlLocator locator) where T : IControlObject
    {
        var element = FindElementRequired();
        var children = element.FindElements(OpenQA.Selenium.By.XPath(locator.Value));
        var results = new List<T>();

        foreach (var child in children)
        {
            var childLocator = By.AutomationId(child.GetAttribute("AutomationId") ?? string.Empty);
            results.Add((T)Activator.CreateInstance(typeof(T), Context, childLocator, Page)!);
        }

        return results.AsReadOnly();
    }

    #endregion
}
