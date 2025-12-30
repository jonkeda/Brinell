using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using Brinell.Core.Logging;
using Brinell.Core.Testing;
using Brinell.Html.Infrastructure;

namespace Brinell.Html.Testing;

/// <summary>
/// Supported browser types for HTML UI testing.
/// </summary>
public enum BrowserType
{
    /// <summary>Google Chrome browser.</summary>
    Chrome,
    
    /// <summary>Mozilla Firefox browser.</summary>
    Firefox,
    
    /// <summary>Microsoft Edge browser.</summary>
    Edge
}

/// <summary>
/// Base class for HTML/web UI tests using Selenium.
/// </summary>
public abstract class HtmlUITestBase : UITestBase<SeleniumTestContext>
{
    private SeleniumDriverAdapter? _driver;
    private IWebDriver? _webDriver;

    /// <summary>
    /// The Selenium driver adapter.
    /// </summary>
    protected SeleniumDriverAdapter? Driver => _driver;
    
    /// <summary>
    /// The underlying Selenium WebDriver.
    /// </summary>
    protected IWebDriver? WebDriver => _webDriver;

    /// <summary>
    /// The browser type to use. Override to change browser. Default is Chrome.
    /// </summary>
    protected virtual BrowserType Browser => BrowserType.Chrome;

    /// <summary>
    /// Whether to run browser in headless mode. Override to change. Default is false.
    /// </summary>
    protected virtual bool Headless => false;

    /// <summary>
    /// The base URL for the web application. Override this in your test class.
    /// </summary>
    protected abstract string BaseUrl { get; }

    /// <summary>
    /// Create a new HTML UI test base with optional output writer.
    /// </summary>
    protected HtmlUITestBase(Action<string>? outputWriter = null)
        : base(outputWriter)
    {
    }

    /// <summary>
    /// Launch browser and navigate to the base URL.
    /// </summary>
    protected void LaunchBrowser()
    {
        LaunchBrowser(BaseUrl);
    }

    /// <summary>
    /// Launch browser and navigate to a specific URL.
    /// </summary>
    protected void LaunchBrowser(string url)
    {
        Log($"Launching {Browser} browser...");
        
        _webDriver = CreateWebDriver();
        _driver = new SeleniumDriverAdapter(_webDriver);
        
        var logger = CsvTestLogger.CreateDefault(TestName);
        InitializeContext(new SeleniumTestContext(_driver, Log), logger);
        
        Log($"Navigating to: {url}");
        _driver.NavigateTo(url);
    }

    /// <summary>
    /// Launch browser with an existing WebDriver instance.
    /// </summary>
    protected void LaunchBrowser(IWebDriver webDriver)
    {
        _webDriver = webDriver;
        _driver = new SeleniumDriverAdapter(_webDriver);
        
        var logger = CsvTestLogger.CreateDefault(TestName);
        InitializeContext(new SeleniumTestContext(_driver, Log), logger);
    }

    /// <summary>
    /// Navigate to a URL relative to the base URL.
    /// </summary>
    protected void NavigateTo(string relativePath)
    {
        var url = CombineUrl(BaseUrl, relativePath);
        Log($"Navigating to: {url}");
        _driver?.NavigateTo(url);
    }

    /// <summary>
    /// Navigate to an absolute URL.
    /// </summary>
    protected void NavigateToAbsolute(string absoluteUrl)
    {
        Log($"Navigating to: {absoluteUrl}");
        _driver?.NavigateTo(absoluteUrl);
    }

    /// <summary>
    /// Refresh the current page.
    /// </summary>
    protected void RefreshPage()
    {
        Log("Refreshing page");
        _driver?.Refresh();
    }

    /// <summary>
    /// Navigate back.
    /// </summary>
    protected void NavigateBack()
    {
        Log("Navigating back");
        _driver?.Back();
    }

    /// <summary>
    /// Navigate forward.
    /// </summary>
    protected void NavigateForward()
    {
        Log("Navigating forward");
        _driver?.Forward();
    }

    /// <summary>
    /// Get current page URL.
    /// </summary>
    protected string GetCurrentUrl()
    {
        return _driver?.GetCurrentUrl() ?? string.Empty;
    }

    /// <summary>
    /// Get current page title.
    /// </summary>
    protected string GetPageTitle()
    {
        return _driver?.GetTitle() ?? string.Empty;
    }
    
    /// <summary>
    /// Assert current URL equals expected value.
    /// </summary>
    protected void AssertUrl(string expected, string? message = null)
    {
        var actual = GetCurrentUrl();
        if (actual != expected)
        {
            throw new Brinell.Core.Logging.AssertionException(
                message ?? $"Expected URL '{expected}' but got '{actual}'.");
        }
    }
    
    /// <summary>
    /// Assert current URL contains expected substring.
    /// </summary>
    protected void AssertUrlContains(string expected, string? message = null)
    {
        var actual = GetCurrentUrl();
        if (!actual.Contains(expected))
        {
            throw new Brinell.Core.Logging.AssertionException(
                message ?? $"Expected URL to contain '{expected}' but got '{actual}'.");
        }
    }
    
    /// <summary>
    /// Assert page title equals expected value.
    /// </summary>
    protected void AssertTitle(string expected, string? message = null)
    {
        var actual = GetPageTitle();
        if (actual != expected)
        {
            throw new Brinell.Core.Logging.AssertionException(
                message ?? $"Expected title '{expected}' but got '{actual}'.");
        }
    }
    
    /// <summary>
    /// Assert page title contains expected substring.
    /// </summary>
    protected void AssertTitleContains(string expected, string? message = null)
    {
        var actual = GetPageTitle();
        if (!actual.Contains(expected))
        {
            throw new Brinell.Core.Logging.AssertionException(
                message ?? $"Expected title to contain '{expected}' but got '{actual}'.");
        }
    }

    /// <summary>
    /// Execute JavaScript on the page.
    /// </summary>
    protected object? ExecuteScript(string script, params object[] args)
    {
        return _driver?.ExecuteScript(script, args);
    }

    /// <summary>
    /// Close the browser.
    /// </summary>
    protected void CloseBrowser()
    {
        Log("Closing browser");
        _driver?.Dispose();
        _driver = null;
        _webDriver = null;
    }

    /// <summary>
    /// Create the WebDriver instance based on browser type.
    /// Override to customize browser options.
    /// </summary>
    protected virtual IWebDriver CreateWebDriver()
    {
        return Browser switch
        {
            BrowserType.Chrome => CreateChromeDriver(),
            BrowserType.Firefox => CreateFirefoxDriver(),
            BrowserType.Edge => CreateEdgeDriver(),
            _ => throw new NotSupportedException($"Browser type '{Browser}' is not supported.")
        };
    }

    /// <summary>
    /// Create Chrome WebDriver. Override to customize options.
    /// </summary>
    protected virtual IWebDriver CreateChromeDriver()
    {
        new DriverManager().SetUpDriver(new ChromeConfig());
        
        var options = new ChromeOptions();
        
        if (Headless)
        {
            options.AddArgument("--headless=new");
        }
        
        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--window-size=1920,1080");
        
        return new ChromeDriver(options);
    }

    /// <summary>
    /// Create Firefox WebDriver. Override to customize options.
    /// </summary>
    protected virtual IWebDriver CreateFirefoxDriver()
    {
        new DriverManager().SetUpDriver(new FirefoxConfig());
        
        var options = new FirefoxOptions();
        
        if (Headless)
        {
            options.AddArgument("--headless");
        }
        
        options.AddArgument("--width=1920");
        options.AddArgument("--height=1080");
        
        return new FirefoxDriver(options);
    }

    /// <summary>
    /// Create Edge WebDriver. Override to customize options.
    /// </summary>
    protected virtual IWebDriver CreateEdgeDriver()
    {
        new DriverManager().SetUpDriver(new EdgeConfig());
        
        var options = new EdgeOptions();
        
        if (Headless)
        {
            options.AddArgument("--headless=new");
        }
        
        options.AddArgument("--disable-gpu");
        options.AddArgument("--window-size=1920,1080");
        
        return new EdgeDriver(options);
    }

    /// <summary>
    /// Combine base URL with relative path.
    /// </summary>
    private static string CombineUrl(string baseUrl, string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return baseUrl;
            
        baseUrl = baseUrl.TrimEnd('/');
        relativePath = relativePath.TrimStart('/');
        return $"{baseUrl}/{relativePath}";
    }

    /// <summary>
    /// Dispose resources.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                CloseBrowser();
            }
            catch (Exception ex)
            {
                Log($"Error closing browser: {ex.Message}");
            }
        }
        
        base.Dispose(disposing);
    }
}
