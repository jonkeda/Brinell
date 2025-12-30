using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Core.Logging;
using Brinell.Stride.Communication;
using Brinell.Stride.Infrastructure;

namespace Brinell.Stride.Controls.Base;

/// <summary>
/// Base class for selector controls (ListBox, ComboBox).
/// </summary>
public abstract class StrideSelectorControlBase : StrideControlBase, ISelectorControl, IItemsControl
{
    /// <summary>
    /// Create a new selector control.
    /// </summary>
    protected StrideSelectorControlBase(StrideTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetItems() => GetState().Items ?? [];

    /// <inheritdoc />
    public int GetItemCount() => GetItems().Count;

    /// <inheritdoc />
    public string? GetSelectedText() => GetState().SelectedText;

    /// <inheritdoc />
    public int GetSelectedIndex() => GetState().SelectedIndex;

    /// <inheritdoc />
    public string GetItemText(int index)
    {
        var items = GetItems();
        if (index < 0 || index >= items.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                $"Index {index} is outside range [0, {items.Count - 1}]");
        }
        return items[index];
    }

    /// <inheritdoc />
    public void ClickItem(int index)
    {
        SelectByIndex(index);
    }

    /// <inheritdoc />
    public void ClickItem(string text)
    {
        SelectByText(text);
    }

    /// <inheritdoc />
    public bool HasItem(string text)
    {
        return GetItems().Contains(text);
    }

    /// <inheritdoc />
    public void SelectByIndex(int index)
    {
        CheckEnabled();

        var items = GetItems();
        if (index < 0 || index >= items.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                $"Index {index} is outside range [0, {items.Count - 1}]");
        }

        Context.SendCommand(AutomationCommand.Action("SelectIndex", _automationId, index));
        LogAction("SelectByIndex", index.ToString());
    }

    /// <inheritdoc />
    public void SelectByText(string text)
    {
        var items = GetItems();
        var index = ((List<string>)items).IndexOf(text);

        if (index < 0)
        {
            throw new InvalidOperationException(
                $"Item '{text}' not found in list. Available: {string.Join(", ", items)}");
        }

        SelectByIndex(index);
        LogAction("SelectByText", text);
    }

    /// <summary>
    /// Wait for selected text.
    /// </summary>
    public bool WaitSelectedText(string expected, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => GetSelectedText() == expected,
            timeoutMs,
            $"element '{AutomationId}' selected='{expected}'");
    }

    /// <inheritdoc />
    public void AssertSelectedText(string expected, string? message = null)
    {
        var actual = GetSelectedText();
        LogAssertion("AssertSelectedText", expected, actual ?? "");

        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Control '{AutomationId}' selection mismatch. Expected: '{expected}', Actual: '{actual}'");
        }
    }

    /// <summary>
    /// Assert item count.
    /// </summary>
    public void AssertItemCount(int expected, string? message = null)
    {
        var actual = GetItemCount();
        LogAssertion("AssertItemCount", expected, actual);

        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Control '{AutomationId}' item count mismatch. Expected: {expected}, Actual: {actual}");
        }
    }

    /// <summary>
    /// Assert item exists.
    /// </summary>
    public void AssertItemExists(string text, string? message = null)
    {
        var items = GetItems();
        var exists = items.Contains(text);
        LogAssertion("AssertItemExists", text, exists);

        if (!exists)
        {
            throw new AssertionException(
                message ?? $"Control '{AutomationId}' should contain item '{text}' but does not. " +
                          $"Available: {string.Join(", ", items)}");
        }
    }
}
