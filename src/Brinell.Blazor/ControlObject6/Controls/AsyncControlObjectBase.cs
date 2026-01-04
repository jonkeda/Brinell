using System.Text.RegularExpressions;
using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Microsoft.Playwright;

namespace Brinell.Blazor.ControlObject6.Controls;

/// <summary>
/// Base class for all Blazor control objects.
/// Provides async functionality using Playwright.
/// </summary>
public abstract class AsyncControlObjectBase : IAsyncInteractiveControlObject
{
    private readonly BlazorTestContext _context;

    /// <inheritdoc />
    public ControlLocator Locator { get; }

    /// <inheritdoc />
    public IAsyncPageObject? Page { get; }

    /// <summary>
    /// Gets the test context.
    /// </summary>
    protected BlazorTestContext Context => _context;

    /// <summary>
    /// Gets the Playwright page.
    /// </summary>
    protected IPage PlaywrightPage => _context.Page;

    /// <summary>
    /// Gets the default timeout in milliseconds.
    /// </summary>
    protected int DefaultTimeoutMs => _context.DefaultTimeoutMs;

    /// <summary>
    /// Creates a new control object.
    /// </summary>
    protected AsyncControlObjectBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        Locator = locator ?? throw new ArgumentNullException(nameof(locator));
        Page = page;
    }

    /// <summary>
    /// Creates a new control object using TestId.
    /// </summary>
    protected AsyncControlObjectBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : this(context, By.TestId(testId), page)
    {
    }

    #region Logging

    /// <summary>
    /// Logs a message using the test context.
    /// </summary>
    protected void Log(string message)
    {
        Context.Log($"[{GetType().Name}] {Locator}: {message}");
    }

    #endregion

    #region Element Finding

    /// <summary>
    /// Gets the Playwright locator for this control.
    /// </summary>
    protected ILocator GetLocator()
    {
        return ConvertLocator(Locator);
    }

    /// <summary>
    /// Converts a ControlLocator to a Playwright ILocator.
    /// </summary>
    protected ILocator ConvertLocator(ControlLocator locator)
    {
        return locator.Strategy switch
        {
            LocatorStrategy.AutomationId => PlaywrightPage.Locator($"[data-automation-id='{locator.Value}']"),
            LocatorStrategy.TestId => PlaywrightPage.GetByTestId(locator.Value),
            LocatorStrategy.Id => PlaywrightPage.Locator($"#{locator.Value}"),
            LocatorStrategy.Name => PlaywrightPage.Locator($"[name='{locator.Value}']"),
            LocatorStrategy.ClassName => PlaywrightPage.Locator($".{locator.Value}"),
            LocatorStrategy.XPath => PlaywrightPage.Locator($"xpath={locator.Value}"),
            LocatorStrategy.Css => PlaywrightPage.Locator(locator.Value),
            LocatorStrategy.TagName => PlaywrightPage.Locator(locator.Value),
            LocatorStrategy.Text => PlaywrightPage.GetByText(locator.Value, new() { Exact = true }),
            LocatorStrategy.PartialText => PlaywrightPage.GetByText(locator.Value),
            LocatorStrategy.Label => PlaywrightPage.GetByLabel(locator.Value),
            LocatorStrategy.Placeholder => PlaywrightPage.GetByPlaceholder(locator.Value),
            LocatorStrategy.Title => PlaywrightPage.GetByTitle(locator.Value),
            LocatorStrategy.Role => PlaywrightPage.GetByRole(ParseAriaRole(locator.Value)),
            LocatorStrategy.DataAttribute => PlaywrightPage.Locator(
                $"[{locator.DataAttributeName ?? "data-test"}='{locator.Value}']"),
            LocatorStrategy.AccessibilityId => PlaywrightPage.Locator($"[data-accessibility-id='{locator.Value}']"),
            _ => throw new NotSupportedException($"Locator strategy '{locator.Strategy}' is not supported.")
        };
    }

    private static AriaRole ParseAriaRole(string role)
    {
        return Enum.TryParse<AriaRole>(role, ignoreCase: true, out var ariaRole)
            ? ariaRole
            : AriaRole.Generic;
    }

    #endregion

    #region Existence

    /// <inheritdoc />
    public async Task<bool> IsExistsAsync(CancellationToken ct = default)
    {
        return await GetLocator().CountAsync() > 0;
    }

    /// <inheritdoc />
    public async Task<bool> WaitExistsAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return true;

        try
        {
            var timeout = timeoutMs ?? DefaultTimeoutMs;
            var locator = GetLocator();

            if (expected.Value)
            {
                await locator.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = timeout });
            }
            else
            {
                await locator.WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = timeout });
            }
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task CheckExistsAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        if (!await WaitExistsAsync(expected, timeoutMs, ct))
        {
            var timeout = timeoutMs ?? DefaultTimeoutMs;
            throw new UITestTimeoutException(
                $"Element {(expected.Value ? "does not exist" : "still exists")}",
                Locator.Value,
                timeout,
                "CheckExists",
                $"Exists={await IsExistsAsync(ct)}");
        }
    }

    /// <inheritdoc />
    public async Task AssertExistsAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        await CheckExistsAsync(expected, timeoutMs, ct);

        var actual = await IsExistsAsync(ct);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected element to {(expected.Value ? "exist" : "not exist")}",
                Locator.Value,
                "AssertExists");
        }
    }

    #endregion

    #region Visibility

    /// <inheritdoc />
    public async Task<bool> IsVisibleAsync(CancellationToken ct = default)
    {
        return await GetLocator().IsVisibleAsync();
    }

    /// <inheritdoc />
    public async Task<bool> WaitVisibleAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return true;

        try
        {
            var timeout = timeoutMs ?? DefaultTimeoutMs;
            var locator = GetLocator();

            if (expected.Value)
            {
                await locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeout });
            }
            else
            {
                await locator.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = timeout });
            }
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task CheckVisibleAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        if (!await WaitVisibleAsync(expected, timeoutMs, ct))
        {
            var timeout = timeoutMs ?? DefaultTimeoutMs;
            throw new UITestTimeoutException(
                $"Element is {(expected.Value ? "not visible" : "still visible")}",
                Locator.Value,
                timeout,
                "CheckVisible",
                $"Visible={await IsVisibleAsync(ct)}");
        }
    }

    /// <inheritdoc />
    public async Task AssertVisibleAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        await CheckVisibleAsync(expected, timeoutMs, ct);

        var actual = await IsVisibleAsync(ct);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected element to be {(expected.Value ? "visible" : "not visible")}",
                Locator.Value,
                "AssertVisible");
        }
    }

    #endregion

    #region Enabled

    /// <inheritdoc />
    public async Task<bool> IsEnabledAsync(CancellationToken ct = default)
    {
        return await GetLocator().IsEnabledAsync();
    }

    /// <inheritdoc />
    public async Task<bool> WaitEnabledAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return true;

        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var deadline = DateTime.UtcNow.AddMilliseconds(timeout);

        while (DateTime.UtcNow < deadline)
        {
            if (await IsEnabledAsync(ct) == expected.Value)
                return true;

            await Task.Delay(_context.DefaultPollingIntervalMs, ct);
        }

        return false;
    }

    /// <inheritdoc />
    public async Task CheckEnabledAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        if (!await WaitEnabledAsync(expected, timeoutMs, ct))
        {
            var timeout = timeoutMs ?? DefaultTimeoutMs;
            throw new UITestTimeoutException(
                $"Element is {(expected.Value ? "not enabled" : "still enabled")}",
                Locator.Value,
                timeout,
                "CheckEnabled",
                $"Enabled={await IsEnabledAsync(ct)}");
        }
    }

    /// <inheritdoc />
    public async Task AssertEnabledAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        await CheckEnabledAsync(expected, timeoutMs, ct);

        var actual = await IsEnabledAsync(ct);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected element to be {(expected.Value ? "enabled" : "disabled")}",
                Locator.Value,
                "AssertEnabled");
        }
    }

    #endregion

    #region Text

    /// <inheritdoc />
    public virtual async Task<string> GetTextAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().InnerTextAsync() ?? string.Empty;
    }

    /// <inheritdoc />
    public async Task AssertTextAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetTextAsync(timeoutMs, ct);
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Expected text '{expected}', but was '{actual}'",
                Locator.Value,
                "AssertText");
        }
    }

    /// <inheritdoc />
    public async Task AssertTextContainsAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetTextAsync(timeoutMs, ct);
        if (!actual.Contains(expected, StringComparison.Ordinal))
        {
            throw new AssertionException(
                message ?? $"Expected text to contain '{expected}', but was '{actual}'",
                Locator.Value,
                "AssertTextContains");
        }
    }

    /// <inheritdoc />
    public async Task AssertTextStartsWithAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetTextAsync(timeoutMs, ct);
        if (!actual.StartsWith(expected, StringComparison.Ordinal))
        {
            throw new AssertionException(
                message ?? $"Expected text to start with '{expected}', but was '{actual}'",
                Locator.Value,
                "AssertTextStartsWith");
        }
    }

    /// <inheritdoc />
    public async Task AssertTextEndsWithAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetTextAsync(timeoutMs, ct);
        if (!actual.EndsWith(expected, StringComparison.Ordinal))
        {
            throw new AssertionException(
                message ?? $"Expected text to end with '{expected}', but was '{actual}'",
                Locator.Value,
                "AssertTextEndsWith");
        }
    }

    /// <inheritdoc />
    public async Task AssertTextMatchesAsync(string? pattern, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (pattern is null) return;

        var actual = await GetTextAsync(timeoutMs, ct);
        if (!Regex.IsMatch(actual, pattern))
        {
            throw new AssertionException(
                message ?? $"Expected text to match pattern '{pattern}', but was '{actual}'",
                Locator.Value,
                "AssertTextMatches");
        }
    }

    /// <inheritdoc />
    public async Task AssertTextEmptyAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetTextAsync(timeoutMs, ct);
        var isEmpty = string.IsNullOrEmpty(actual);

        if (isEmpty != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected text to be {(expected.Value ? "empty" : "not empty")}, but was '{actual}'",
                Locator.Value,
                "AssertTextEmpty");
        }
    }

    #endregion
}
