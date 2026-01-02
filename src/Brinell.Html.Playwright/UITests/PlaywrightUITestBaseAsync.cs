using Brinell.Core.Abstractions;
using Brinell.Core.Logging;
using Brinell.Html.Playwright.Infrastructure;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Html.Playwright.UITests;

/// <summary>
/// Base class for async Playwright UI tests using async/await patterns.
/// </summary>
/// <remarks>
/// Use this base class when writing async UI tests with Playwright:
/// <code>
/// public class LoginTests : PlaywrightUITestBaseAsync
/// {
///     public LoginTests(ITestOutputHelper output) : base(output) { }
///
///     [Fact]
///     public async Task Login_ValidCredentials_DisplaysWelcome()
///     {
///         // Arrange
///         var loginPage = new LoginPage(Context);
///         await loginPage.CheckActiveAsync();
///
///         // Act
///         await loginPage.UsernameInput.SetTextAsync("user@example.com");
///         await loginPage.PasswordInput.SetTextAsync("password123");
///         await loginPage.LoginButton.ClickAsync();
///
///         // Assert
///         await loginPage.WelcomeMessage.AssertVisibleAsync();
///         await loginPage.WelcomeMessage.AssertTextContainsAsync("Welcome");
///     }
/// }
/// </code>
/// </remarks>
public abstract class PlaywrightUITestBaseAsync : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private IBrowser? _browser;
    private IBrowserContext? _browserContext;
    private IPage? _page;
    private PlaywrightDriverAdapter? _driver;

    /// <summary>
    /// The test context for async operations.
    /// </summary>
    protected PlaywrightTestContext Context { get; private set; } = null!;

    /// <summary>
    /// The browser instance.
    /// </summary>
    protected IBrowser Browser => _browser!;

    /// <summary>
    /// The page instance.
    /// </summary>
    protected IPage Page => _page!;

    /// <summary>
    /// Browser type (chromium, firefox, webkit).
    /// Override to use different browser.
    /// </summary>
    protected virtual BrowserTypeLaunchOptions BrowserOptions => new() { Headless = true };

    /// <summary>
    /// Browser type selector (chromium, firefox, webkit).
    /// Override to use different browser.
    /// </summary>
    protected virtual string BrowserType => "chromium";

    protected PlaywrightUITestBaseAsync(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _output.WriteLine($"Starting async test: {GetType().Name}");

        // Launch browser
        var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = BrowserType switch
        {
            "firefox" => await playwright.Firefox.LaunchAsync(BrowserOptions),
            "webkit" => await playwright.Webkit.LaunchAsync(BrowserOptions),
            _ => await playwright.Chromium.LaunchAsync(BrowserOptions)
        };

        // Create context and page
        _browserContext = await _browser.NewContextAsync();
        _page = await _browserContext.NewPageAsync();

        // Create driver adapter and test context
        _driver = new PlaywrightDriverAdapter(_page, _browser, _browserContext);
        Context = new PlaywrightTestContext(_driver, msg => _output.WriteLine(msg))
        {
            TestName = GetType().Name,
            DefaultTimeoutMs = 10000
        };

        _output.WriteLine("Browser and page initialized");
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        try
        {
            if (_page != null)
            {
                await _page.CloseAsync();
                _output.WriteLine("Page closed");
            }

            if (_browser != null)
            {
                await _browser.CloseAsync();
                _output.WriteLine("Browser closed");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Error cleaning up: {ex.Message}");
        }
    }

    /// <summary>
    /// Navigate to a URL.
    /// </summary>
    protected async Task GotoAsync(string url)
    {
        await _page!.GotoAsync(url);
        _output.WriteLine($"Navigated to: {url}");
    }

    /// <summary>
    /// Wait for a specific timeout.
    /// </summary>
    protected async Task DelayAsync(int milliseconds)
    {
        await Task.Delay(milliseconds);
    }

    /// <summary>
    /// Log a message to test output.
    /// </summary>
    protected void Log(string message)
    {
        _output.WriteLine(message);
    }
}
