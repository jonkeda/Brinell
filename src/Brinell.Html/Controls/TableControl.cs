using OpenQA.Selenium;
using Brinell.Core.Abstractions;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Html.Controls;

/// <summary>
/// HTML/Selenium control for HTML table elements.
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

    public TableControl(SeleniumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public TableControl(SeleniumTestContext context, IPageObject? page, IWebElement? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public TableControl(SeleniumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the number of rows in the table (excluding headers).
    /// </summary>
    public virtual int GetRowCount() => GetItemCount();

    /// <summary>
    /// Get header cell texts.
    /// </summary>
    public virtual IReadOnlyList<string> GetHeaders()
    {
        var table = FindElement();
        if (table == null) return Array.Empty<string>();

        var headers = table.FindElements(By.CssSelector(HeaderSelector));
        return headers.Select(h => h.Text.Trim()).ToList();
    }

    /// <summary>
    /// Get the number of columns based on header count.
    /// </summary>
    public virtual int GetColumnCount()
    {
        return GetHeaders().Count;
    }

    /// <summary>
    /// Get cell text at specific row and column (0-based indices).
    /// </summary>
    public virtual string GetCellText(int rowIndex, int columnIndex)
    {
        var rows = FindItems();
        if (rowIndex < 0 || rowIndex >= rows.Count)
            throw new ArgumentOutOfRangeException(nameof(rowIndex), $"Row index {rowIndex} is out of range. Table has {rows.Count} rows.");

        var row = rows[rowIndex];
        var cells = row.FindElements(By.CssSelector(CellSelector));
        
        if (columnIndex < 0 || columnIndex >= cells.Count)
            throw new ArgumentOutOfRangeException(nameof(columnIndex), $"Column index {columnIndex} is out of range. Row has {cells.Count} cells.");

        return cells[columnIndex].Text.Trim();
    }

    /// <summary>
    /// Get all cell texts for a specific row (0-based index).
    /// </summary>
    public virtual IReadOnlyList<string> GetRowCells(int rowIndex)
    {
        var rows = FindItems();
        if (rowIndex < 0 || rowIndex >= rows.Count)
            throw new ArgumentOutOfRangeException(nameof(rowIndex), $"Row index {rowIndex} is out of range. Table has {rows.Count} rows.");

        var row = rows[rowIndex];
        var cells = row.FindElements(By.CssSelector(CellSelector));
        return cells.Select(c => c.Text.Trim()).ToList();
    }

    /// <summary>
    /// Get all cell texts for a specific column (0-based index).
    /// </summary>
    public virtual IReadOnlyList<string> GetColumnCells(int columnIndex)
    {
        var rows = FindItems();
        var result = new List<string>();

        foreach (var row in rows)
        {
            var cells = row.FindElements(By.CssSelector(CellSelector));
            if (columnIndex >= 0 && columnIndex < cells.Count)
            {
                result.Add(cells[columnIndex].Text.Trim());
            }
        }

        return result;
    }

    /// <summary>
    /// Click a cell at specific row and column (0-based indices).
    /// </summary>
    public virtual void ClickCell(int rowIndex, int columnIndex)
    {
        LogAction("ClickCell", $"row={rowIndex}, column={columnIndex}");
        
        var rows = FindItems();
        if (rowIndex < 0 || rowIndex >= rows.Count)
            throw new ArgumentOutOfRangeException(nameof(rowIndex));

        var row = rows[rowIndex];
        var cells = row.FindElements(By.CssSelector(CellSelector));
        
        if (columnIndex < 0 || columnIndex >= cells.Count)
            throw new ArgumentOutOfRangeException(nameof(columnIndex));

        cells[columnIndex].Click();
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
    /// Find row index containing text in any cell. Returns -1 if not found.
    /// </summary>
    public virtual int FindRowContaining(string text)
    {
        var rows = FindItems();
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].Text.Contains(text, StringComparison.OrdinalIgnoreCase))
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
    /// Assert the table has the expected number of rows.
    /// </summary>
    public virtual void AssertRowCount(int expected)
    {
        AssertItemCount(expected);
    }

    /// <summary>
    /// Assert a cell contains the expected text.
    /// </summary>
    public virtual void AssertCellText(int rowIndex, int columnIndex, string expected)
    {
        var actual = GetCellText(rowIndex, columnIndex);
        if (!actual.Contains(expected, StringComparison.OrdinalIgnoreCase))
        {
            ThrowAssertionFailed("CellText", actual, expected,
                $"Cell [{rowIndex},{columnIndex}] text was '{actual}', expected to contain '{expected}'.");
        }
        LogAssertPass("CellText", actual, expected);
    }
}
