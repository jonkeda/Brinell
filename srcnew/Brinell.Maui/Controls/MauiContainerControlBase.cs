using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.Maui.Interfaces;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.Controls;

/// <summary>
/// Base class for MAUI container controls that can scope element finding
/// to their children. Implements IContainerControl to enable nested control patterns.
/// </summary>
public abstract class MauiContainerControlBase : MauiControlBase, IContainerControl<AppiumElement>
{
    /// <summary>
    /// Initializes a new instance of the MauiContainerControlBase class.
    /// </summary>
    /// <param name="locator">The locator used to find this container control.</param>
    /// <param name="scope">The element scope containing this control.</param>
    protected MauiContainerControlBase(Locator locator, IMauiElementScope scope)
        : base(locator, scope)
    {
    }

    /// <inheritdoc />
    public LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;

    /// <inheritdoc />
    object IContainerControl.ContainerRoot => ContainerRoot!;

    /// <inheritdoc />
    public AppiumElement ContainerRoot => FindElement()!;

    /// <inheritdoc />
    public AppiumElement? TryFindElement(Locator locator)
    {
        var root = ContainerRoot;
        if (root == null)
            return null;

        try
        {
            var by = LocatorConverter.ToBy(locator, Context.Platform);
            return root.FindElement(by) as AppiumElement;
        }
        catch (OpenQA.Selenium.NoSuchElementException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public AppiumElement FindElement(Locator locator)
    {
        var element = TryFindElement(locator);
        if (element == null)
        {
            throw new Core.Exceptions.ElementNotFoundException(locator, 
                $"Element not found within container: {locator}");
        }
        return element;
    }

    /// <inheritdoc />
    public IReadOnlyList<AppiumElement> FindElements(Locator locator)
    {
        var root = ContainerRoot;
        if (root == null)
            return Array.Empty<AppiumElement>();

        try
        {
            var by = LocatorConverter.ToBy(locator, Context.Platform);
            return root.FindElements(by).Cast<AppiumElement>().ToList();
        }
        catch (OpenQA.Selenium.NoSuchElementException)
        {
            return Array.Empty<AppiumElement>();
        }
    }
}
