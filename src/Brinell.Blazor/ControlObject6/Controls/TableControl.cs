using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;

namespace Brinell.Blazor.ControlObject6.Controls;

/// <summary>
/// Table control for Blazor.
/// Wraps &lt;table&gt; elements.
/// </summary>
public class TableControl : AsyncControlObjectBase
{
    /// <summary>
    /// Creates a new Table control.
    /// </summary>
    public TableControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new Table control using TestId.
    /// </summary>
    public TableControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page)
    {
    }

    #region Row/Column Count

    /// <summary>
    /// Gets the number of rows in the table body.
    /// </summary>
    public virtual async Task<int> GetRowCountAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().Locator("tbody tr").CountAsync();
    }

    /// <summary>
    /// Gets the number of columns in the table (from first row).
    /// </summary>
    public virtual async Task<int> GetColumnCountAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        var firstRow = GetLocator().Locator("thead tr, tbody tr").First;
        return await firstRow.Locator("th, td").CountAsync();
    }

    /// <summary>
    /// Gets the number of header rows.
    /// </summary>
    public virtual async Task<int> GetHeaderRowCountAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().Locator("thead tr").CountAsync();
    }

    #endregion

    #region Cell Access

    /// <summary>
    /// Gets the cell text at the specified row and column (0-based indices).
    /// </summary>
    public virtual async Task<string> GetCellTextAsync(int row, int column, int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        var cell = GetLocator().Locator($"tbody tr:nth-child({row + 1}) td:nth-child({column + 1})");
        return await cell.InnerTextAsync();
    }

    /// <summary>
    /// Gets all cell texts in a row (0-based index).
    /// </summary>
    public virtual async Task<IReadOnlyList<string>> GetRowTextAsync(int row, int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        var cells = GetLocator().Locator($"tbody tr:nth-child({row + 1}) td");
        var count = await cells.CountAsync();
        var texts = new List<string>();

        for (int i = 0; i < count; i++)
        {
            texts.Add(await cells.Nth(i).InnerTextAsync());
        }

        return texts;
    }

    /// <summary>
    /// Gets all cell texts in a column (0-based index).
    /// </summary>
    public virtual async Task<IReadOnlyList<string>> GetColumnTextAsync(int column, int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        var cells = GetLocator().Locator($"tbody tr td:nth-child({column + 1})");
        var count = await cells.CountAsync();
        var texts = new List<string>();

        for (int i = 0; i < count; i++)
        {
            texts.Add(await cells.Nth(i).InnerTextAsync());
        }

        return texts;
    }

    /// <summary>
    /// Gets the header text at the specified column (0-based index).
    /// </summary>
    public virtual async Task<string> GetHeaderTextAsync(int column, int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        var header = GetLocator().Locator($"thead tr th:nth-child({column + 1})");
        return await header.InnerTextAsync();
    }

    /// <summary>
    /// Gets all header texts.
    /// </summary>
    public virtual async Task<IReadOnlyList<string>> GetHeadersAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        var headers = GetLocator().Locator("thead tr th");
        var count = await headers.CountAsync();
        var texts = new List<string>();

        for (int i = 0; i < count; i++)
        {
            texts.Add(await headers.Nth(i).InnerTextAsync());
        }

        return texts;
    }

    #endregion

    #region Row Click

    /// <summary>
    /// Clicks a row in the table (0-based index).
    /// </summary>
    public virtual async Task ClickRowAsync(int row, int? timeoutMs = null, CancellationToken ct = default)
    {
        Log($"ClickRowAsync({row})");
        await CheckVisibleAsync(true, timeoutMs, ct);

        var rowElement = GetLocator().Locator($"tbody tr:nth-child({row + 1})");
        await rowElement.ClickAsync();
    }

    /// <summary>
    /// Clicks a cell in the table (0-based indices).
    /// </summary>
    public virtual async Task ClickCellAsync(int row, int column, int? timeoutMs = null, CancellationToken ct = default)
    {
        Log($"ClickCellAsync({row}, {column})");
        await CheckVisibleAsync(true, timeoutMs, ct);

        var cell = GetLocator().Locator($"tbody tr:nth-child({row + 1}) td:nth-child({column + 1})");
        await cell.ClickAsync();
    }

    /// <summary>
    /// Clicks a header cell in the table (0-based index).
    /// </summary>
    public virtual async Task ClickHeaderAsync(int column, int? timeoutMs = null, CancellationToken ct = default)
    {
        Log($"ClickHeaderAsync({column})");
        await CheckVisibleAsync(true, timeoutMs, ct);

        var header = GetLocator().Locator($"thead tr th:nth-child({column + 1})");
        await header.ClickAsync();
    }

    #endregion

    #region Assertions

    /// <summary>
    /// Asserts the row count.
    /// </summary>
    public virtual async Task AssertRowCountAsync(int? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetRowCountAsync(timeoutMs, ct);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected row count {expected}, but was {actual}",
                Locator.Value,
                "AssertRowCount");
        }
    }

    /// <summary>
    /// Asserts the column count.
    /// </summary>
    public virtual async Task AssertColumnCountAsync(int? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetColumnCountAsync(timeoutMs, ct);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected column count {expected}, but was {actual}",
                Locator.Value,
                "AssertColumnCount");
        }
    }

    /// <summary>
    /// Asserts a cell value.
    /// </summary>
    public virtual async Task AssertCellTextAsync(int row, int column, string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetCellTextAsync(row, column, timeoutMs, ct);
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Expected cell[{row},{column}] text '{expected}', but was '{actual}'",
                Locator.Value,
                "AssertCellText");
        }
    }

    #endregion
}
