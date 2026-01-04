using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using System.Collections.ObjectModel;

namespace Brinell.Maui.Tests.ControlObject6.Mocks;

/// <summary>
/// Testable wrapper interface for AppiumDriver.
/// Since AppiumDriver has non-virtual members that can't be mocked,
/// we use this abstraction for testing.
/// </summary>
public interface IAppiumDriverWrapper
{
    IWebElement FindElement(OpenQA.Selenium.By by);
    ReadOnlyCollection<IWebElement> FindElements(OpenQA.Selenium.By by);
    INavigation Navigate();
    Screenshot GetScreenshot();
}

/// <summary>
/// Testable wrapper interface for AppiumElement.
/// </summary>
public interface IAppiumElementWrapper
{
    string Text { get; }
    bool Displayed { get; }
    bool Enabled { get; }
    void Click();
    void Clear();
    void SendKeys(string text);
    string GetAttribute(string attributeName);
}

/// <summary>
/// Real implementation that wraps an actual AppiumDriver.
/// Used in production to pass through to the real driver.
/// </summary>
public class AppiumDriverWrapper : IAppiumDriverWrapper
{
    private readonly AppiumDriver _driver;

    public AppiumDriverWrapper(AppiumDriver driver)
    {
        _driver = driver;
    }

    public IWebElement FindElement(OpenQA.Selenium.By by) => _driver.FindElement(by);
    public ReadOnlyCollection<IWebElement> FindElements(OpenQA.Selenium.By by)
    {
        var appiumElements = _driver.FindElements(by);
        var webElements = appiumElements.Cast<IWebElement>().ToList();
        return new ReadOnlyCollection<IWebElement>(webElements);
    }
    public INavigation Navigate() => _driver.Navigate();
    public Screenshot GetScreenshot() => _driver.GetScreenshot();
}

/// <summary>
/// Real implementation that wraps an actual AppiumElement.
/// </summary>
public class AppiumElementWrapper : IAppiumElementWrapper
{
    private readonly AppiumElement _element;

    public AppiumElementWrapper(AppiumElement element)
    {
        _element = element;
    }

    public string Text => _element.Text;
    public bool Displayed => _element.Displayed;
    public bool Enabled => _element.Enabled;
    public void Click() => _element.Click();
    public void Clear() => _element.Clear();
    public void SendKeys(string text) => _element.SendKeys(text);
    public string GetAttribute(string attributeName) => _element.GetAttribute(attributeName);
}

/// <summary>
/// Factory for creating mock Appium driver wrappers for unit testing.
/// Uses interfaces instead of concrete types to avoid Moq non-virtual member issues.
/// </summary>
public static class MockAppiumFactory
{
    /// <summary>
    /// Creates a mock IAppiumDriverWrapper with basic setup.
    /// </summary>
    public static Mock<IAppiumDriverWrapper> CreateMockDriverWrapper()
    {
        var mockDriver = new Mock<IAppiumDriverWrapper>();
        var mockNavigation = new Mock<INavigation>();
        
        // Setup default navigation behavior
        mockDriver.Setup(d => d.Navigate()).Returns(mockNavigation.Object);
        
        return mockDriver;
    }

    /// <summary>
    /// Creates a mock IAppiumElementWrapper for testing.
    /// </summary>
    public static Mock<IAppiumElementWrapper> CreateMockElementWrapper(
        string text = "Test Text",
        bool displayed = true,
        bool enabled = true)
    {
        var mockElement = new Mock<IAppiumElementWrapper>();
        
        mockElement.Setup(e => e.Text).Returns(text);
        mockElement.Setup(e => e.Displayed).Returns(displayed);
        mockElement.Setup(e => e.Enabled).Returns(enabled);
        
        return mockElement;
    }

    /// <summary>
    /// Creates a mock IWebElement that behaves like an AppiumElement.
    /// </summary>
    public static Mock<IWebElement> CreateMockElement(
        string text = "Test Text",
        bool displayed = true,
        bool enabled = true)
    {
        var mockElement = new Mock<IWebElement>();
        
        mockElement.Setup(e => e.Text).Returns(text);
        mockElement.Setup(e => e.Displayed).Returns(displayed);
        mockElement.Setup(e => e.Enabled).Returns(enabled);
        
        return mockElement;
    }

    /// <summary>
    /// Configures the driver wrapper to return an element when FindElement is called.
    /// </summary>
    public static void SetupFindElement(
        Mock<IAppiumDriverWrapper> mockDriver,
        Mock<IWebElement> mockElement,
        OpenQA.Selenium.By? by = null)
    {
        if (by is null)
        {
            mockDriver.Setup(d => d.FindElement(It.IsAny<OpenQA.Selenium.By>()))
                .Returns(mockElement.Object);
        }
        else
        {
            mockDriver.Setup(d => d.FindElement(by))
                .Returns(mockElement.Object);
        }
    }

    /// <summary>
    /// Configures the driver wrapper to throw NoSuchElementException when FindElement is called.
    /// </summary>
    public static void SetupElementNotFound(Mock<IAppiumDriverWrapper> mockDriver, OpenQA.Selenium.By? by = null)
    {
        if (by is null)
        {
            mockDriver.Setup(d => d.FindElement(It.IsAny<OpenQA.Selenium.By>()))
                .Throws<NoSuchElementException>();
        }
        else
        {
            mockDriver.Setup(d => d.FindElement(by))
                .Throws<NoSuchElementException>();
        }
    }

    /// <summary>
    /// Configures the driver wrapper to return multiple elements.
    /// </summary>
    public static void SetupFindElements(
        Mock<IAppiumDriverWrapper> mockDriver,
        IList<IWebElement> elements)
    {
        var collection = new ReadOnlyCollection<IWebElement>(elements.ToList());
        mockDriver.Setup(d => d.FindElements(It.IsAny<OpenQA.Selenium.By>()))
            .Returns(collection);
    }
}
