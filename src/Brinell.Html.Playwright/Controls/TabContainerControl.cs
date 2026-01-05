using Microsoft.Playwright;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Html.Playwright.Controls.Base;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls;

/// <summary>
/// Playwright control for tab containers (Bootstrap tabs, nav-tabs, etc.).
/// Supports standard Bootstrap tab markup with nav-tabs and tab-content.
/// </summary>
public class TabContainerControl : ControlBase, ITabControl
{
    /// <summary>
    /// CSS selector for tab buttons within this container.
    /// Default is Bootstrap nav-link pattern.
    /// </summary>
    protected virtual string TabButtonSelector => ".nav-link, [role='tab']";

    /// <summary>
    /// CSS class that indicates active/selected tab.
    /// </summary>
    protected virtual string ActiveTabClass => "active";

    public TabContainerControl(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public TabContainerControl(PlaywrightTestContext context, IPageObject? page, ILocator? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public TabContainerControl(PlaywrightTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the locator for all tab buttons.
    /// </summary>
    protected ILocator GetTabButtons()
    {
        return GetLocator().Locator(TabButtonSelector);
    }

    /// <summary>
    /// Get the number of tabs.
    /// </summary>
    public int GetTabCount()
    {
        return GetTabButtons().CountAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get the number of tabs asynchronously.
    /// </summary>
    public async Task<int> GetTabCountAsync()
    {
        return await GetTabButtons().CountAsync();
    }

    /// <summary>
    /// Get the index of the currently selected tab.
    /// </summary>
    public int GetSelectedTabIndex()
    {
        var tabs = GetTabButtons();
        var count = tabs.CountAsync().GetAwaiter().GetResult();

        for (int i = 0; i < count; i++)
        {
            var tab = tabs.Nth(i);
            var classAttr = tab.GetAttributeAsync("class").GetAwaiter().GetResult() ?? "";
            if (classAttr.Contains(ActiveTabClass))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Get the index of the currently selected tab asynchronously.
    /// </summary>
    public async Task<int> GetSelectedTabIndexAsync()
    {
        var tabs = GetTabButtons();
        var count = await tabs.CountAsync();

        for (int i = 0; i < count; i++)
        {
            var tab = tabs.Nth(i);
            var classAttr = await tab.GetAttributeAsync("class") ?? "";
            if (classAttr.Contains(ActiveTabClass))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Get the name/text of the currently selected tab.
    /// </summary>
    public string GetSelectedTabName()
    {
        var index = GetSelectedTabIndex();
        if (index < 0)
            return string.Empty;

        var tabs = GetTabButtons();
        return tabs.Nth(index).TextContentAsync().GetAwaiter().GetResult()?.Trim() ?? "";
    }

    /// <summary>
    /// Get the name/text of the currently selected tab asynchronously.
    /// </summary>
    public async Task<string> GetSelectedTabNameAsync()
    {
        var index = await GetSelectedTabIndexAsync();
        if (index < 0)
            return string.Empty;

        var tabs = GetTabButtons();
        return (await tabs.Nth(index).TextContentAsync())?.Trim() ?? "";
    }

    /// <summary>
    /// Get all tab names.
    /// </summary>
    public IReadOnlyList<string> GetTabNames()
    {
        var tabs = GetTabButtons();
        var count = tabs.CountAsync().GetAwaiter().GetResult();
        var names = new List<string>();

        for (int i = 0; i < count; i++)
        {
            var text = tabs.Nth(i).TextContentAsync().GetAwaiter().GetResult()?.Trim() ?? "";
            names.Add(text);
        }

        return names;
    }

    /// <summary>
    /// Select a tab by its zero-based index.
    /// </summary>
    public void SelectTab(int index)
    {
        LogAction("SelectTab", index.ToString());
        var tabs = GetTabButtons();
        var count = tabs.CountAsync().GetAwaiter().GetResult();

        if (index < 0 || index >= count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Tab index {index} is out of range (0-{count - 1}).");

        tabs.Nth(index).ClickAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Select a tab by its zero-based index asynchronously.
    /// </summary>
    public async Task SelectTabAsync(int index)
    {
        LogAction("SelectTab", index.ToString());
        var tabs = GetTabButtons();
        var count = await tabs.CountAsync();

        if (index < 0 || index >= count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Tab index {index} is out of range (0-{count - 1}).");

        await tabs.Nth(index).ClickAsync();
    }

    /// <summary>
    /// Select a tab by its name/text.
    /// </summary>
    public void SelectTab(string name)
    {
        LogAction("SelectTab", name);
        var tabs = GetTabButtons();
        var count = tabs.CountAsync().GetAwaiter().GetResult();

        for (int i = 0; i < count; i++)
        {
            var tab = tabs.Nth(i);
            var text = tab.TextContentAsync().GetAwaiter().GetResult()?.Trim() ?? "";
            if (text.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                tab.ClickAsync().GetAwaiter().GetResult();
                return;
            }
        }

        throw new InvalidOperationException($"Tab '{name}' not found.");
    }

    /// <summary>
    /// Select a tab by its name/text asynchronously.
    /// </summary>
    public async Task SelectTabAsync(string name)
    {
        LogAction("SelectTab", name);
        var tabs = GetTabButtons();
        var count = await tabs.CountAsync();

        for (int i = 0; i < count; i++)
        {
            var tab = tabs.Nth(i);
            var text = (await tab.TextContentAsync())?.Trim() ?? "";
            if (text.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                await tab.ClickAsync();
                return;
            }
        }

        throw new InvalidOperationException($"Tab '{name}' not found.");
    }

    /// <summary>
    /// Assert the selected tab has the expected name.
    /// </summary>
    public void AssertSelectedTab(string name, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetSelectedTabName();
        if (!actual.Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            ThrowAssertionFailed("SelectedTab", actual, name,
                message ?? $"Expected selected tab '{name}' but got '{actual}' for element '{AutomationId}'.");
        }
        LogAssertPass("SelectedTab", actual, name);
    }

    /// <summary>
    /// Assert the selected tab has the expected index.
    /// </summary>
    public void AssertSelectedTabIndex(int index, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetSelectedTabIndex();
        if (actual != index)
        {
            ThrowAssertionFailed("SelectedTabIndex", actual.ToString(), index.ToString(),
                message ?? $"Expected selected tab index {index} but got {actual} for element '{AutomationId}'.");
        }
        LogAssertPass("SelectedTabIndex", actual.ToString(), index.ToString());
    }

    /// <summary>
    /// Assert the tab count equals expected value.
    /// </summary>
    public void AssertTabCount(int expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetTabCount();
        if (actual != expected)
        {
            ThrowAssertionFailed("TabCount", actual.ToString(), expected.ToString(),
                message ?? $"Expected {expected} tabs but got {actual} for element '{AutomationId}'.");
        }
        LogAssertPass("TabCount", actual.ToString(), expected.ToString());
    }
}
