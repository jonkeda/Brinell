using Microsoft.Playwright;
using Brinell.Core.Abstractions;
using Brinell.Html.Playwright.Controls.Base;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls;

/// <summary>
/// Playwright control for radio button groups.
/// Manages a set of radio buttons with the same name.
/// </summary>
public class RadioGroupControl : ControlBase
{
    /// <summary>
    /// CSS selector for radio inputs within this group.
    /// </summary>
    protected virtual string RadioSelector => "input[type='radio']";

    public RadioGroupControl(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public RadioGroupControl(PlaywrightTestContext context, IPageObject? page, ILocator? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public RadioGroupControl(PlaywrightTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the locator for all radio buttons in this group.
    /// </summary>
    protected ILocator GetRadioButtons()
    {
        return GetLocator().Locator(RadioSelector);
    }

    /// <summary>
    /// Get the number of radio buttons in the group.
    /// </summary>
    public int GetOptionCount()
    {
        return GetRadioButtons().CountAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get the selected value in the radio group.
    /// </summary>
    public string GetSelectedValue()
    {
        var radios = GetRadioButtons();
        var count = radios.CountAsync().GetAwaiter().GetResult();

        for (int i = 0; i < count; i++)
        {
            var radio = radios.Nth(i);
            if (radio.IsCheckedAsync().GetAwaiter().GetResult())
            {
                return radio.GetAttributeAsync("value").GetAwaiter().GetResult() ?? "";
            }
        }

        return "";
    }

    /// <summary>
    /// Get the selected value in the radio group asynchronously.
    /// </summary>
    public async Task<string> GetSelectedValueAsync()
    {
        var radios = GetRadioButtons();
        var count = await radios.CountAsync();

        for (int i = 0; i < count; i++)
        {
            var radio = radios.Nth(i);
            if (await radio.IsCheckedAsync())
            {
                return await radio.GetAttributeAsync("value") ?? "";
            }
        }

        return "";
    }

    /// <summary>
    /// Get the index of the selected radio button.
    /// </summary>
    public int GetSelectedIndex()
    {
        var radios = GetRadioButtons();
        var count = radios.CountAsync().GetAwaiter().GetResult();

        for (int i = 0; i < count; i++)
        {
            var radio = radios.Nth(i);
            if (radio.IsCheckedAsync().GetAwaiter().GetResult())
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Select a radio button by value.
    /// </summary>
    public void SelectByValue(string value)
    {
        LogAction("SelectByValue", value);
        var radio = GetLocator().Locator($"input[type='radio'][value='{value}']");
        radio.CheckAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Select a radio button by value asynchronously.
    /// </summary>
    public async Task SelectByValueAsync(string value)
    {
        LogAction("SelectByValue", value);
        var radio = GetLocator().Locator($"input[type='radio'][value='{value}']");
        await radio.CheckAsync();
    }

    /// <summary>
    /// Select a radio button by index.
    /// </summary>
    public void SelectByIndex(int index)
    {
        LogAction("SelectByIndex", index.ToString());
        var radios = GetRadioButtons();
        radios.Nth(index).CheckAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Select a radio button by index asynchronously.
    /// </summary>
    public async Task SelectByIndexAsync(int index)
    {
        LogAction("SelectByIndex", index.ToString());
        var radios = GetRadioButtons();
        await radios.Nth(index).CheckAsync();
    }

    /// <summary>
    /// Select a radio button by its label text.
    /// </summary>
    public void SelectByLabel(string labelText)
    {
        LogAction("SelectByLabel", labelText);
        var label = GetLocator().Locator($"label:has-text('{labelText}')");
        label.ClickAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Select a radio button by its label text asynchronously.
    /// </summary>
    public async Task SelectByLabelAsync(string labelText)
    {
        LogAction("SelectByLabel", labelText);
        var label = GetLocator().Locator($"label:has-text('{labelText}')");
        await label.ClickAsync();
    }

    /// <summary>
    /// Assert the selected value equals expected.
    /// </summary>
    public void AssertSelectedValue(string expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetSelectedValue();
        if (actual != expected)
        {
            ThrowAssertionFailed("SelectedValue", actual, expected,
                message ?? $"Expected selected value '{expected}' but got '{actual}' for element '{AutomationId}'.");
        }
        LogAssertPass("SelectedValue", actual, expected);
    }

    /// <summary>
    /// Assert the selected index equals expected.
    /// </summary>
    public void AssertSelectedIndex(int expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetSelectedIndex();
        if (actual != expected)
        {
            ThrowAssertionFailed("SelectedIndex", actual.ToString(), expected.ToString(),
                message ?? $"Expected selected index {expected} but got {actual} for element '{AutomationId}'.");
        }
        LogAssertPass("SelectedIndex", actual.ToString(), expected.ToString());
    }

    /// <summary>
    /// Assert the option count equals expected.
    /// </summary>
    public void AssertOptionCount(int expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetOptionCount();
        if (actual != expected)
        {
            ThrowAssertionFailed("OptionCount", actual.ToString(), expected.ToString(),
                message ?? $"Expected {expected} options but got {actual} for element '{AutomationId}'.");
        }
        LogAssertPass("OptionCount", actual.ToString(), expected.ToString());
    }
}
