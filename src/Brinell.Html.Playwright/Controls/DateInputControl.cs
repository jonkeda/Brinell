using Microsoft.Playwright;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Html.Playwright.Controls.Base;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls;

/// <summary>
/// Playwright control for HTML date input elements.
/// Supports input[type="date"] elements.
/// </summary>
public class DateInputControl : ControlBase, IDateControl
{
    public DateInputControl(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public DateInputControl(PlaywrightTestContext context, IPageObject? page, ILocator? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public DateInputControl(PlaywrightTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the current date value.
    /// </summary>
    public DateTime GetDate()
    {
        var element = FindElement();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not found.");

        var value = element.InputValueAsync().GetAwaiter().GetResult();
        if (string.IsNullOrEmpty(value))
            return DateTime.MinValue;

        return DateTime.Parse(value);
    }

    /// <summary>
    /// Get the current date value asynchronously.
    /// </summary>
    public async Task<DateTime> GetDateAsync()
    {
        var element = await FindElementAsync();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not found.");

        var value = await element.InputValueAsync();
        if (string.IsNullOrEmpty(value))
            return DateTime.MinValue;

        return DateTime.Parse(value);
    }

    /// <summary>
    /// Set the date value.
    /// </summary>
    public void SetDate(DateTime date)
    {
        LogAction("SetDate", date.ToString("yyyy-MM-dd"));
        var element = FindElement();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not found.");

        var formatted = date.ToString("yyyy-MM-dd");
        element.FillAsync(formatted).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Set the date value asynchronously.
    /// </summary>
    public async Task SetDateAsync(DateTime date)
    {
        LogAction("SetDate", date.ToString("yyyy-MM-dd"));
        var element = await FindElementAsync();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not found.");

        var formatted = date.ToString("yyyy-MM-dd");
        await element.FillAsync(formatted);
    }

    /// <summary>
    /// Set the date value using year, month, and day components.
    /// </summary>
    public void SetDate(int year, int month, int day)
    {
        SetDate(new DateTime(year, month, day));
    }

    /// <summary>
    /// Set the date value using components asynchronously.
    /// </summary>
    public Task SetDateAsync(int year, int month, int day)
    {
        return SetDateAsync(new DateTime(year, month, day));
    }

    /// <summary>
    /// Assert the date equals expected value.
    /// </summary>
    public void AssertDate(DateTime expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetDate();
        if (actual.Date != expected.Date)
        {
            ThrowAssertionFailed("Date", actual.ToString("yyyy-MM-dd"), expected.ToString("yyyy-MM-dd"),
                message ?? $"Expected date '{expected:yyyy-MM-dd}' but got '{actual:yyyy-MM-dd}' for element '{AutomationId}'.");
        }
        LogAssertPass("Date", actual.ToString("yyyy-MM-dd"), expected.ToString("yyyy-MM-dd"));
    }

    /// <summary>
    /// Assert the date equals expected value asynchronously.
    /// </summary>
    public async Task AssertDateAsync(DateTime expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = await GetDateAsync();
        if (actual.Date != expected.Date)
        {
            ThrowAssertionFailed("Date", actual.ToString("yyyy-MM-dd"), expected.ToString("yyyy-MM-dd"),
                message ?? $"Expected date '{expected:yyyy-MM-dd}' but got '{actual:yyyy-MM-dd}' for element '{AutomationId}'.");
        }
        LogAssertPass("Date", actual.ToString("yyyy-MM-dd"), expected.ToString("yyyy-MM-dd"));
    }

    /// <summary>
    /// Assert the date is within an expected range.
    /// </summary>
    public void AssertDateInRange(DateTime minDate, DateTime maxDate, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetDate();
        if (actual.Date < minDate.Date || actual.Date > maxDate.Date)
        {
            ThrowAssertionFailed("DateInRange", actual.ToString("yyyy-MM-dd"), 
                $"between {minDate:yyyy-MM-dd} and {maxDate:yyyy-MM-dd}",
                message ?? $"Expected date between '{minDate:yyyy-MM-dd}' and '{maxDate:yyyy-MM-dd}' but got '{actual:yyyy-MM-dd}'.");
        }
        LogAssertPass("DateInRange", actual.ToString("yyyy-MM-dd"), $"{minDate:yyyy-MM-dd} to {maxDate:yyyy-MM-dd}");
    }

    /// <summary>
    /// Clear the date input.
    /// </summary>
    public void Clear()
    {
        LogAction("Clear");
        var element = FindElement();
        element?.ClearAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Clear the date input asynchronously.
    /// </summary>
    public async Task ClearAsync()
    {
        LogAction("Clear");
        var element = await FindElementAsync();
        if (element != null)
        {
            await element.ClearAsync();
        }
    }
}
