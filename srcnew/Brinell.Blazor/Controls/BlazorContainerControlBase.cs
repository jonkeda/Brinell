using Brinell.Blazor.Interfaces;
using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using OpenQA.Selenium;

namespace Brinell.Blazor.Controls;

/// <summary>
/// Base class for Blazor container controls that can scope element finding
/// to their children. Implements IContainerControl to enable nested control patterns.
/// </summary>
public abstract class BlazorContainerControlBase : BlazorControlBase, IContainerControl<IWebElement>
{
    /// <summary>
    /// Initializes a new instance of the BlazorContainerControlBase class.
    /// </summary>
    /// <param name="locator">The locator used to find this container control.</param>
    /// <param name="scope">The element scope containing this control.</param>
    protected BlazorContainerControlBase(Locator locator, IBlazorElementScope scope)
        : base(locator, scope)
    {
    }

    /// <inheritdoc />
    public LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.Css;

    /// <inheritdoc />
    object IContainerControl.ContainerRoot => ContainerRoot!;

    /// <inheritdoc />
    public IWebElement ContainerRoot => FindElement()!;

    /// <inheritdoc />
    public IWebElement? TryFindElement(Locator locator)
    {
        var root = ContainerRoot;
        if (root == null)
            return null;

        try
        {
            var by = LocatorConverter.ToBy(locator);
            return root.FindElement(by);
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public IWebElement FindElement(Locator locator)
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
    public IReadOnlyList<IWebElement> FindElements(Locator locator)
    {
        var root = ContainerRoot;
        if (root == null)
            return Array.Empty<IWebElement>();

        try
        {
            var by = LocatorConverter.ToBy(locator);
            return root.FindElements(by).ToList();
        }
        catch (NoSuchElementException)
        {
            return Array.Empty<IWebElement>();
        }
    }
}
