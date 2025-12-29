using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Brinell.Core.Abstractions;

namespace Brinell.Html.Infrastructure;

/// <summary>
/// Selenium driver adapter for web UI automation.
/// </summary>
public class SeleniumDriverAdapter : IDriverAdapter
{
    private readonly IWebDriver _driver;
    private readonly string _automationIdAttribute;
    private bool _disposed;

    /// <summary>
    /// The underlying Selenium WebDriver.
    /// </summary>
    public IWebDriver WebDriver => _driver;
    
    /// <summary>
    /// The underlying Selenium WebDriver (alias for WebDriver).
    /// </summary>
    public IWebDriver Driver => _driver;

    /// <summary>
    /// Create driver adapter with an existing WebDriver instance.
    /// </summary>
    /// <param name="driver">The Selenium WebDriver instance.</param>
    /// <param name="automationIdAttribute">The HTML attribute used for automation IDs (default: data-automation-id).</param>
    public SeleniumDriverAdapter(IWebDriver driver, string automationIdAttribute = "data-automation-id")
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _automationIdAttribute = automationIdAttribute;
    }

    /// <summary>
    /// Find element by AutomationId (data-automation-id attribute by default).
    /// </summary>
    public IElementAdapter? FindElement(string automationId)
    {
        try
        {
            // First try data-automation-id attribute
            var element = _driver.FindElement(By.CssSelector($"[{_automationIdAttribute}='{automationId}']"));
            return new SeleniumElementAdapter(element, automationId);
        }
        catch (NoSuchElementException)
        {
            // Fall back to id attribute
            try
            {
                var element = _driver.FindElement(By.Id(automationId));
                return new SeleniumElementAdapter(element, automationId);
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Find element by AutomationId and return the raw IWebElement directly.
    /// </summary>
    public IWebElement? FindElementDirect(string automationId)
    {
        try
        {
            // First try data-automation-id attribute
            return _driver.FindElement(By.CssSelector($"[{_automationIdAttribute}='{automationId}']"));
        }
        catch (NoSuchElementException)
        {
            // Fall back to id attribute
            try
            {
                return _driver.FindElement(By.Id(automationId));
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Find element by XPath.
    /// </summary>
    public IElementAdapter? FindElementByXPath(string xpath)
    {
        try
        {
            var element = _driver.FindElement(By.XPath(xpath));
            return new SeleniumElementAdapter(element);
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }

    /// <summary>
    /// Find element by CSS selector.
    /// </summary>
    public IElementAdapter? FindElementByCss(string cssSelector)
    {
        try
        {
            var element = _driver.FindElement(By.CssSelector(cssSelector));
            return new SeleniumElementAdapter(element);
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }

    /// <summary>
    /// Find all elements matching AutomationId.
    /// </summary>
    public IReadOnlyCollection<IElementAdapter> FindElements(string automationId)
    {
        var elements = _driver.FindElements(By.CssSelector($"[{_automationIdAttribute}='{automationId}']"));
        
        // If none found, try id attribute
        if (elements.Count == 0)
        {
            elements = _driver.FindElements(By.Id(automationId));
        }
        
        return elements.Select(e => new SeleniumElementAdapter(e, automationId)).ToList();
    }

    /// <summary>
    /// Click an element.
    /// </summary>
    public void Click(IElementAdapter element)
    {
        if (element is SeleniumElementAdapter seleniumElement)
        {
            seleniumElement.WebElement.Click();
        }
    }

    /// <summary>
    /// Send keys/text to an element (appends to existing text).
    /// </summary>
    public void SendKeys(IElementAdapter element, string text)
    {
        if (element is SeleniumElementAdapter seleniumElement)
        {
            seleniumElement.WebElement.SendKeys(text);
        }
    }

    /// <summary>
    /// Clear an element's text.
    /// </summary>
    public void Clear(IElementAdapter element)
    {
        if (element is SeleniumElementAdapter seleniumElement)
        {
            seleniumElement.WebElement.Clear();
        }
    }

    /// <summary>
    /// Get element's text content.
    /// </summary>
    public string? GetText(IElementAdapter element)
    {
        if (element is SeleniumElementAdapter seleniumElement)
        {
            var webElement = seleniumElement.WebElement;
            
            // For input/textarea, get the value attribute
            var tagName = webElement.TagName.ToLowerInvariant();
            if (tagName == "input" || tagName == "textarea")
            {
                return webElement.GetAttribute("value");
            }
            
            // For select, get selected option text
            if (tagName == "select")
            {
                var select = new SelectElement(webElement);
                return select.SelectedOption?.Text;
            }
            
            // For other elements, get inner text
            return webElement.Text;
        }
        return null;
    }

    /// <summary>
    /// Get an attribute value from an element.
    /// </summary>
    public string? GetAttribute(IElementAdapter element, string name)
    {
        if (element is SeleniumElementAdapter seleniumElement)
        {
            return seleniumElement.WebElement.GetAttribute(name);
        }
        return null;
    }

    /// <summary>
    /// Check if element is displayed.
    /// </summary>
    public bool IsDisplayed(IElementAdapter element)
    {
        if (element is SeleniumElementAdapter seleniumElement)
        {
            try
            {
                return seleniumElement.WebElement.Displayed;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// Check if element is enabled.
    /// </summary>
    public bool IsEnabled(IElementAdapter element)
    {
        if (element is SeleniumElementAdapter seleniumElement)
        {
            try
            {
                return seleniumElement.WebElement.Enabled;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// Navigate to a URL.
    /// </summary>
    public void NavigateTo(string url)
    {
        _driver.Navigate().GoToUrl(url);
    }

    /// <summary>
    /// Get current URL.
    /// </summary>
    public string GetCurrentUrl()
    {
        return _driver.Url;
    }

    /// <summary>
    /// Get page title.
    /// </summary>
    public string GetTitle()
    {
        return _driver.Title;
    }

    /// <summary>
    /// Refresh the current page.
    /// </summary>
    public void Refresh()
    {
        _driver.Navigate().Refresh();
    }

    /// <summary>
    /// Navigate back.
    /// </summary>
    public void Back()
    {
        _driver.Navigate().Back();
    }

    /// <summary>
    /// Navigate forward.
    /// </summary>
    public void Forward()
    {
        _driver.Navigate().Forward();
    }

    /// <summary>
    /// Execute JavaScript.
    /// </summary>
    public object? ExecuteScript(string script, params object[] args)
    {
        if (_driver is IJavaScriptExecutor jsExecutor)
        {
            return jsExecutor.ExecuteScript(script, args);
        }
        return null;
    }

    /// <summary>
    /// Take a screenshot.
    /// </summary>
    public byte[]? TakeScreenshot()
    {
        if (_driver is ITakesScreenshot screenshotDriver)
        {
            var screenshot = screenshotDriver.GetScreenshot();
            return screenshot.AsByteArray;
        }
        return null;
    }

    /// <summary>
    /// Find element within a container element by AutomationId.
    /// </summary>
    /// <param name="container">The container element to search within.</param>
    /// <param name="automationId">The AutomationId/selector to find.</param>
    /// <returns>The element if found, null otherwise.</returns>
    public IWebElement? FindElementInContainer(IWebElement container, string automationId)
    {
        try
        {
            // First try data-automation-id attribute within container
            return container.FindElement(By.CssSelector($"[{_automationIdAttribute}='{automationId}']"));
        }
        catch (NoSuchElementException)
        {
            // Fall back to id attribute within container
            try
            {
                return container.FindElement(By.Id(automationId));
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Find all elements within a container element by AutomationId.
    /// </summary>
    /// <param name="container">The container element to search within.</param>
    /// <param name="automationId">The AutomationId/selector to find.</param>
    /// <returns>Collection of matching elements.</returns>
    public IReadOnlyCollection<IWebElement> FindElementsInContainer(IWebElement container, string automationId)
    {
        var elements = container.FindElements(By.CssSelector($"[{_automationIdAttribute}='{automationId}']"));
        
        // If none found, try id attribute
        if (elements.Count == 0)
        {
            elements = container.FindElements(By.Id(automationId));
        }
        
        return elements;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _driver?.Quit();
            _driver?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
