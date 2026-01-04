using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.ControlObject6.Context;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for navigation page controls in MAUI.
/// </summary>
public abstract class NavigationPageControlBase : ControlObjectBase, INavigationPageControlObject
{
    /// <summary>
    /// Creates a new navigation page control.
    /// </summary>
    protected NavigationPageControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new navigation page control using AutomationId.
    /// </summary>
    protected NavigationPageControlBase(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// XPath pattern for finding the navigation bar.
    /// </summary>
    protected virtual string NavigationBarXPath => ".//*[@ClassName='NavigationBar' or contains(@ClassName,'TitleBar') or contains(@ClassName,'AppBar')]";

    /// <summary>
    /// XPath pattern for finding the back button.
    /// </summary>
    protected virtual string BackButtonXPath => ".//*[@ClassName='Button' and (@AutomationId='Back' or @Name='Back' or contains(@AutomationId,'NavigateBack'))]";

    #region Navigation Stack

    /// <inheritdoc/>
    public virtual string? GetCurrentPageTitle(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        try
        {
            var navBar = element.FindElement(OpenQA.Selenium.By.XPath(NavigationBarXPath));
            var titleElement = navBar.FindElement(OpenQA.Selenium.By.XPath(".//*[@ClassName='TextBlock' or @ClassName='Label']"));
            var title = titleElement?.Text;
            Log($"GetCurrentPageTitle: {title}");
            return title;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public virtual void AssertCurrentPageTitle(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetCurrentPageTitle(timeoutMs);
        if (actual != expected)
        {
            var msg = message ?? $"Expected page title '{expected}' but was '{actual}'";
            throw new AssertionException(msg, Locator.Value, "AssertCurrentPageTitle");
        }
    }

    /// <inheritdoc/>
    public virtual bool CanGoBack(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        try
        {
            var backButton = element.FindElement(OpenQA.Selenium.By.XPath(BackButtonXPath));
            return backButton.Displayed && backButton.Enabled;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Navigation Actions

    /// <inheritdoc/>
    public virtual void GoBack(int? timeoutMs = null)
    {
        Log("GoBack()");
        var element = FindElementRequired(timeoutMs);
        var backButton = element.FindElement(OpenQA.Selenium.By.XPath(BackButtonXPath));
        backButton.Click();
    }

    /// <inheritdoc/>
    public virtual void WaitNavigationComplete(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        Thread.Sleep(500);
    }

    #endregion
}
