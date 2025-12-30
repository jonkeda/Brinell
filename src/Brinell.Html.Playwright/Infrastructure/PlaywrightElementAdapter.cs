using Microsoft.Playwright;
using Brinell.Core.Abstractions;

namespace Brinell.Html.Playwright.Infrastructure;

/// <summary>
/// Represents an HTML element found by Playwright.
/// </summary>
public class PlaywrightElementAdapter : IElementAdapter
{
    private readonly ILocator _locator;

    /// <summary>
    /// The AutomationId (CSS selector) used to find this element.
    /// </summary>
    public string AutomationId { get; }

    /// <summary>
    /// The native Playwright locator.
    /// </summary>
    public object NativeElement => _locator;

    /// <summary>
    /// The native Playwright ILocator with proper typing.
    /// </summary>
    public ILocator Locator => _locator;

    /// <summary>
    /// Create an element adapter with a locator and automation ID.
    /// </summary>
    public PlaywrightElementAdapter(ILocator locator, string automationId)
    {
        _locator = locator ?? throw new ArgumentNullException(nameof(locator));
        AutomationId = automationId;
    }

    /// <summary>
    /// Create an element adapter with a locator. AutomationId will be the locator string.
    /// </summary>
    public PlaywrightElementAdapter(ILocator locator)
    {
        _locator = locator ?? throw new ArgumentNullException(nameof(locator));
        AutomationId = locator.ToString() ?? string.Empty;
    }
}
