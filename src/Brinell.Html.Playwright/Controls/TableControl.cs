using Microsoft.Playwright;
using Brinell.Core.Abstractions;
using Brinell.Html.Playwright.Controls.Base;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls;

/// <summary>
/// Playwright control for HTML table elements.
/// Provides row and cell access for testing table-based data displays.
/// </summary>
public class TableControl : ItemsControlBase
{
    /// <summary>
    /// CSS selector for table rows (items).
    /// Uses tbody tr to get data rows, excluding header rows.
    /// </summary>
    protected override string ItemSelector => "tbody tr, [role='row']:not([role='row']:first-child)";

    /// <summary>
    /// CSS selector for header cells.
    /// </summary>
    protected virtual string HeaderSelector => "thead th, thead td, [role='columnheader']";

    /// <summary>
    /// CSS selector for data cells within a row.
    /// </summary>
    protected virtual string CellSelector => "td, [role='cell'], [role='gridcell']";

    public TableControl(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public TableControl(PlaywrightTestContext context, IPageObject? page, ILocator? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public TableControl(PlaywrightTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the number of rows in the table (excluding headers).
    /// </summary>
    public virtual int GetRowCount() => GetItemCount();

    /// <summary>
    /// Get the number of rows in the table asynchronously.
    /// </summary>
    public virtual Task<int> GetRowCountAsync() => GetItemCountAsync();

    /// <summary>
    /// Get header cell texts.
    /// </summary>
    public virtual IReadOnlyList<string> GetHeaders()
    {
        return GetHeadersAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get header cell texts asynchronously.
    /// </summary>
    public virtual async Task<IReadOnlyList<string>> GetHeadersAsync()
    {
        var table = GetLocator();
        var count = await table.CountAsync();
        if (count == 0) return Array.Empty<string>();

        var headers = table.Locator(HeaderSelector);
        var texts = await headers.AllTextContentsAsync();
        return texts.Select(t => t.Trim()).ToList();
    }

    /// <summary>
    /// Get the number of columns based on header count.
    /// </summary>
    public virtual int GetColumnCount()
    {
        return GetHeaders().Count;
    }

    /// <summary>
    /// Get the number of columns asynchronously.
    /// </summary>
    public virtual async Task<int> GetColumnCountAsync()
    {
        var headers = await GetHeadersAsync();
        return headers.Count;
    }

    /// <summary>
    /// Get cell text at specific row and column (0-based indices).
    /// </summary>
    public virtual string GetCellText(int rowIndex, int columnIndex)
    {
        return GetCellTextAsync(rowIndex, columnIndex).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get cell text at specific row and column asynchronously.
    /// </summary>
    public virtual async Task<string> GetCellTextAsync(int rowIndex, int columnIndex)
    {
        var rowsLocator = GetItemsLocator();
        var rowCount = await rowsLocator.CountAsync();
        
        if (rowIndex < 0 || rowIndex >= rowCount)
            throw new ArgumentOutOfRangeException(nameof(rowIndex), $"Row index {rowIndex} is out of range. Table has {rowCount} rows.");

        var row = rowsLocator.Nth(rowIndex);
        var cells = row.Locator(CellSelector);
        var cellCount = await cells.CountAsync();
        
        if (columnIndex < 0 || columnIndex >= cellCount)
            throw new ArgumentOutOfRangeException(nameof(columnIndex), $"Column index {columnIndex} is out of range. Row has {cellCount} cells.");

        var text = await cells.Nth(columnIndex).TextContentAsync();
        return text?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Get all cell texts for a specific row (0-based index).
    /// </summary>
    public virtual IReadOnlyList<string> GetRowCells(int rowIndex)
    {
        return GetRowCellsAsync(rowIndex).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get all cell texts for a specific row asynchronously.
    /// </summary>
    public virtual async Task<IReadOnlyList<string>> GetRowCellsAsync(int rowIndex)
    {
        var rowsLocator = GetItemsLocator();
        var rowCount = await rowsLocator.CountAsync();
        
        if (rowIndex < 0 || rowIndex >= rowCount)
            throw new ArgumentOutOfRangeException(nameof(rowIndex), $"Row index {rowIndex} is out of range. Table has {rowCount} rows.");

        var row = rowsLocator.Nth(rowIndex);
        var cells = row.Locator(CellSelector);
        var texts = await cells.AllTextContentsAsync();
        return texts.Select(t => t.Trim()).ToList();
    }

    /// <summary>
    /// Get all cell texts for a specific column (0-based index).
    /// </summary>
    public virtual IReadOnlyList<string> GetColumnCells(int columnIndex)
    {
        return GetColumnCellsAsync(columnIndex).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get all cell texts for a specific column asynchronously.
    /// </summary>
    public virtual async Task<IReadOnlyList<string>> GetColumnCellsAsync(int columnIndex)
    {
        var rowsLocator = GetItemsLocator();
        var rowCount = await rowsLocator.CountAsync();
        var result = new List<string>();

        for (int i = 0; i < rowCount; i++)
        {
            var row = rowsLocator.Nth(i);
            var cells = row.Locator(CellSelector);
            var cellCount = await cells.CountAsync();
            
            if (columnIndex >= 0 && columnIndex < cellCount)
            {
                var text = await cells.Nth(columnIndex).TextContentAsync();
                result.Add(text?.Trim() ?? string.Empty);
            }
        }

        return result;
    }

    /// <summary>
    /// Click a cell at specific row and column (0-based indices).
    /// </summary>
    public virtual void ClickCell(int rowIndex, int columnIndex)
    {
        ClickCellAsync(rowIndex, columnIndex).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Click a cell at specific row and column asynchronously.
    /// </summary>
    public virtual async Task ClickCellAsync(int rowIndex, int columnIndex)
    {
        LogAction("ClickCell", $"row={rowIndex}, column={columnIndex}");
        
        var rowsLocator = GetItemsLocator();
        var rowCount = await rowsLocator.CountAsync();
        
        if (rowIndex < 0 || rowIndex >= rowCount)
            throw new ArgumentOutOfRangeException(nameof(rowIndex));

        var row = rowsLocator.Nth(rowIndex);
        var cells = row.Locator(CellSelector);
        var cellCount = await cells.CountAsync();
        
        if (columnIndex < 0 || columnIndex >= cellCount)
            throw new ArgumentOutOfRangeException(nameof(columnIndex));

        await cells.Nth(columnIndex).ClickAsync();
    }

    /// <summary>
    /// Click a row by index (0-based).
    /// </summary>
    public virtual void ClickRow(int rowIndex)
    {
        LogAction("ClickRow", rowIndex.ToString());
        ClickItem(rowIndex);
    }

    /// <summary>
    /// Click a row by index asynchronously.
    /// </summary>
    public virtual async Task ClickRowAsync(int rowIndex)
    {
        LogAction("ClickRow", rowIndex.ToString());
        await ClickItemAsync(rowIndex);
    }

    /// <summary>
    /// Find row index containing text in any cell. Returns -1 if not found.
    /// </summary>
    public virtual int FindRowContaining(string text)
    {
        return FindRowContainingAsync(text).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Find row index containing text in any cell asynchronously. Returns -1 if not found.
    /// </summary>
    public virtual async Task<int> FindRowContainingAsync(string text)
    {
        var rowsLocator = GetItemsLocator();
        var rowCount = await rowsLocator.CountAsync();
        
        for (int i = 0; i < rowCount; i++)
        {
            var rowText = await rowsLocator.Nth(i).TextContentAsync();
            if (rowText?.Contains(text, StringComparison.OrdinalIgnoreCase) == true)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Check if the table contains a row with the specified text.
    /// </summary>
    public virtual bool HasRowContaining(string text)
    {
        return FindRowContaining(text) >= 0;
    }

    /// <summary>
    /// Check if the table contains a row with the specified text asynchronously.
    /// </summary>
    public virtual async Task<bool> HasRowContainingAsync(string text)
    {
        return await FindRowContainingAsync(text) >= 0;
    }

    /// <summary>
    /// Assert the table has the expected number of rows.
    /// </summary>
    public virtual void AssertRowCount(int expected)
    {
        AssertItemCount(expected);
    }

    /// <summary>
    /// Assert the table has the expected number of rows asynchronously.
    /// </summary>
    public virtual Task AssertRowCountAsync(int expected)
    {
        return AssertItemCountAsync(expected);
    }

    /// <summary>
    /// Assert a cell contains the expected text.
    /// </summary>
    public virtual void AssertCellText(int rowIndex, int columnIndex, string expected)
    {
        AssertCellTextAsync(rowIndex, columnIndex, expected).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Assert a cell contains the expected text asynchronously.
    /// </summary>
    public virtual async Task AssertCellTextAsync(int rowIndex, int columnIndex, string expected)
    {
        var actual = await GetCellTextAsync(rowIndex, columnIndex);
        if (!actual.Contains(expected, StringComparison.OrdinalIgnoreCase))
        {
            ThrowAssertionFailed("CellText", actual, expected,
                $"Cell [{rowIndex},{columnIndex}] text was '{actual}', expected to contain '{expected}'.");
        }
        LogAssertPass("CellText", actual, expected);
    }
}
