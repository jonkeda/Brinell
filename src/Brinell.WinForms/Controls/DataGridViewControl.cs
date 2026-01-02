using System.Collections.Generic;
using System.Linq;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Brinell.Core.Abstractions;
using Brinell.WinForms.Controls.Base;
using Brinell.WinForms.Infrastructure;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms DataGridView control wrapper.
/// Provides table/grid data access and row/cell operations.
/// </summary>
public class DataGridViewControl : ControlBase
{
    public DataGridViewControl(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public DataGridViewControl(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public DataGridViewControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId)
    {
    }

    /// <summary>
    /// Get the number of rows in the grid (excluding header).
    /// </summary>
    public int GetRowCount()
    {
        var element = FindElement();
        if (element == null)
        {
            ThrowCheckFailed("GetRowCount", $"Element '{AutomationId}' not found.");
        }

        try
        {
            var rows = element!.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem)).ToList();
            LogAction("GetRowCount", rows.Count.ToString());
            return rows.Count;
        }
        catch (Exception ex)
        {
            ThrowCheckFailed("GetRowCount", $"Failed to get row count: {ex.Message}");
        }

        return 0;
    }

    /// <summary>
    /// Get the number of columns in the grid.
    /// </summary>
    public int GetColumnCount()
    {
        var element = FindElement();
        if (element == null)
        {
            ThrowCheckFailed("GetColumnCount", $"Element '{AutomationId}' not found.");
        }

        try
        {
            var headers = element!.FindAllChildren(cf => cf.ByControlType(ControlType.HeaderItem)).ToList();
            LogAction("GetColumnCount", headers.Count.ToString());
            return headers.Count;
        }
        catch (Exception ex)
        {
            LogDebug($"Failed to get column count: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Get the value of a cell at the specified row and column index.
    /// </summary>
    public string GetCellValue(int rowIndex, int columnIndex)
    {
        var element = FindElement();
        if (element == null)
        {
            ThrowCheckFailed("GetCellValue", $"Element '{AutomationId}' not found.");
        }

        try
        {
            var rows = element!.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem)).ToList();
            if (rowIndex < 0 || rowIndex >= rows.Count)
            {
                ThrowCheckFailed("GetCellValue", $"Row index {rowIndex} out of range (0-{rows.Count - 1}).");
            }

            var cells = rows[rowIndex].FindAllChildren(cf => cf.ByControlType(ControlType.Custom)).ToList();
            if (columnIndex < 0 || columnIndex >= cells.Count)
            {
                ThrowCheckFailed("GetCellValue", $"Column index {columnIndex} out of range (0-{cells.Count - 1}).");
            }

            var value = cells[columnIndex].Name ?? string.Empty;
            LogAction("GetCellValue", $"[{rowIndex},{columnIndex}]={value}");
            return value;
        }
        catch (Exception ex)
        {
            ThrowCheckFailed("GetCellValue", $"Failed to get cell value: {ex.Message}");
        }

        return string.Empty;
    }

    /// <summary>
    /// Get all values in a specific row.
    /// </summary>
    public List<string> GetRowValues(int rowIndex)
    {
        var values = new List<string>();
        var columnCount = GetColumnCount();

        for (int col = 0; col < columnCount; col++)
        {
            values.Add(GetCellValue(rowIndex, col));
        }

        LogAction("GetRowValues", $"Row {rowIndex}: {values.Count} columns");
        return values;
    }

    /// <summary>
    /// Get all values in a specific column.
    /// </summary>
    public List<string> GetColumnValues(int columnIndex)
    {
        var values = new List<string>();
        var rowCount = GetRowCount();

        for (int row = 0; row < rowCount; row++)
        {
            values.Add(GetCellValue(row, columnIndex));
        }

        LogAction("GetColumnValues", $"Column {columnIndex}: {values.Count} rows");
        return values;
    }

    /// <summary>
    /// Select a row by index.
    /// </summary>
    public void SelectRow(int rowIndex)
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("SelectRow", $"Element '{AutomationId}' not visible.");
        }

        try
        {
            var rows = element!.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem)).ToList();
            if (rowIndex < 0 || rowIndex >= rows.Count)
            {
                ThrowCheckFailed("SelectRow", $"Row index {rowIndex} out of range (0-{rows.Count - 1}).");
            }

            var selectionPattern = rows[rowIndex].Patterns.SelectionItem.PatternOrDefault;
            if (selectionPattern != null)
            {
                selectionPattern.Select();
                System.Threading.Thread.Sleep(100);
                LogAction("SelectRow", rowIndex.ToString());
            }
            else
            {
                rows[rowIndex].Click();
                System.Threading.Thread.Sleep(100);
                LogAction("SelectRow", rowIndex.ToString());
            }
        }
        catch (Exception ex)
        {
            ThrowCheckFailed("SelectRow", $"Failed to select row {rowIndex}: {ex.Message}");
        }
    }

    /// <summary>
    /// Double-click a cell to edit.
    /// </summary>
    public void DoubleClickCell(int rowIndex, int columnIndex)
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("DoubleClickCell", $"Element '{AutomationId}' not visible.");
        }

        try
        {
            var rows = element!.FindAllChildren(cf => cf.ByControlType(ControlType.DataItem)).ToList();
            if (rowIndex < 0 || rowIndex >= rows.Count)
            {
                ThrowCheckFailed("DoubleClickCell", $"Row index {rowIndex} out of range.");
            }

            var cells = rows[rowIndex].FindAllChildren(cf => cf.ByControlType(ControlType.Custom)).ToList();
            if (columnIndex < 0 || columnIndex >= cells.Count)
            {
                ThrowCheckFailed("DoubleClickCell", $"Column index {columnIndex} out of range.");
            }

            cells[columnIndex].DoubleClick();
            System.Threading.Thread.Sleep(100);
            LogAction("DoubleClickCell", $"[{rowIndex},{columnIndex}]");
        }
        catch (Exception ex)
        {
            ThrowCheckFailed("DoubleClickCell", $"Failed to double-click cell: {ex.Message}");
        }
    }

    /// <summary>
    /// Find a row that contains the specified value in any column.
    /// </summary>
    public int FindRow(string value)
    {
        var rowCount = GetRowCount();
        var columnCount = GetColumnCount();

        for (int row = 0; row < rowCount; row++)
        {
            for (int col = 0; col < columnCount; col++)
            {
                if (GetCellValue(row, col) == value)
                {
                    LogAction("FindRow", $"Found at [{row},{col}]");
                    return row;
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// Assert that the row count matches expected.
    /// </summary>
    public void AssertRowCount(int expected)
    {
        var actual = GetRowCount();
        if (actual != expected)
        {
            ThrowAssertionFailed("RowCount", actual.ToString(), expected.ToString(),
                $"DataGridView '{AutomationId}' has {actual} rows, expected {expected}.");
        }
        LogAssertPass("RowCount", actual.ToString(), expected.ToString());
    }

    /// <summary>
    /// Assert that a cell value matches expected.
    /// </summary>
    public void AssertCellValueEquals(int rowIndex, int columnIndex, string expected)
    {
        var actual = GetCellValue(rowIndex, columnIndex);
        if (actual != expected)
        {
            ThrowAssertionFailed("CellValueEquals", actual, expected,
                $"DataGridView '{AutomationId}' cell [{rowIndex},{columnIndex}] is '{actual}', expected '{expected}'.");
        }
        LogAssertPass("CellValueEquals", actual, expected);
    }

    /// <summary>
    /// Assert that the grid is not empty.
    /// </summary>
    public void AssertNotEmpty()
    {
        var rowCount = GetRowCount();
        if (rowCount == 0)
        {
            ThrowAssertionFailed("NotEmpty", "0", "> 0",
                $"DataGridView '{AutomationId}' is empty.");
        }
        LogAssertPass("NotEmpty", rowCount.ToString(), "> 0");
    }

    /// <summary>
    /// Get all rows from the data grid as text (legacy method).
    /// </summary>
    public IReadOnlyList<string> GetRowTexts()
    {
        var rows = new List<string>();
        var rowCount = GetRowCount();
        var columnCount = GetColumnCount();

        for (int row = 0; row < rowCount; row++)
        {
            var values = GetRowValues(row);
            rows.Add(string.Join(" | ", values));
        }

        return rows;
    }
}

