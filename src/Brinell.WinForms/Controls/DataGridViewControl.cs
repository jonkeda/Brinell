using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions.Controls;
using Brinell.FlaUI;
using Brinell.FlaUI.Controls.Base;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms DataGridView control wrapper.
/// Provides row/column navigation and cell value access.
/// </summary>
public class DataGridViewControl : ItemsControlBase, IItemsControl
{
    public DataGridViewControl(FlaUITestContext context, PageBase? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public DataGridViewControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId)
    {
    }

    /// <summary>
    /// Get row elements from the grid.
    /// </summary>
    protected override AutomationElement[] GetItemElements()
    {
        var element = FindElement();
        if (element != null)
        {
            var grid = element.AsGrid();
            if (grid != null)
            {
                // Get all rows
                var rows = new List<AutomationElement>();
                for (int i = 0; i < grid.RowCount; i++)
                {
                    var row = grid.GetRowByIndex(i);
                    if (row != null)
                    {
                        rows.Add(row);
                    }
                }
                return rows.ToArray();
            }
        }
        return Array.Empty<AutomationElement>();
    }

    /// <summary>
    /// Get row count.
    /// </summary>
    public virtual int GetRowCount()
    {
        var element = FindElement();
        if (element != null)
        {
            var grid = element.AsGrid();
            return grid?.RowCount ?? 0;
        }
        return 0;
    }

    /// <summary>
    /// Get column count.
    /// </summary>
    public virtual int GetColumnCount()
    {
        var element = FindElement();
        if (element != null)
        {
            var grid = element.AsGrid();
            return grid?.ColumnCount ?? 0;
        }
        return 0;
    }

    /// <summary>
    /// Get cell value by row and column index.
    /// </summary>
    public virtual string GetCellValue(int row, int column)
    {
        var element = FindElement();
        if (element != null)
        {
            var grid = element.AsGrid();
            if (grid != null && row < grid.RowCount && column < grid.ColumnCount)
            {
                var cell = grid.GetRowByIndex(row)?.Cells[column];
                if (cell != null)
                {
                    // Try value pattern first
                    var valuePattern = cell.Patterns.Value.PatternOrDefault;
                    if (valuePattern != null)
                    {
                        return valuePattern.Value.Value ?? string.Empty;
                    }
                    // Fall back to Name
                    return cell.Name ?? string.Empty;
                }
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// Set cell value by row and column index.
    /// </summary>
    public virtual void SetCellValue(int row, int column, string value)
    {
        CheckVisible();
        
        var element = FindElement();
        if (element != null)
        {
            var grid = element.AsGrid();
            if (grid != null && row < grid.RowCount && column < grid.ColumnCount)
            {
                var cell = grid.GetRowByIndex(row)?.Cells[column];
                if (cell != null)
                {
                    var valuePattern = cell.Patterns.Value.PatternOrDefault;
                    if (valuePattern != null)
                    {
                        valuePattern.SetValue(value);
                        LogAction("SetCellValue", $"[{row},{column}]={value}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Select a row by index.
    /// </summary>
    public virtual void SelectRow(int row)
    {
        CheckVisible();
        
        var element = FindElement();
        if (element != null)
        {
            var grid = element.AsGrid();
            if (grid != null && row < grid.RowCount)
            {
                var gridRow = grid.GetRowByIndex(row);
                if (gridRow != null)
                {
                    var selectionPattern = gridRow.Patterns.SelectionItem.PatternOrDefault;
                    selectionPattern?.Select();
                    LogAction("SelectRow", row.ToString());
                }
            }
        }
    }

    /// <summary>
    /// Get selected row index. Returns -1 if none selected.
    /// </summary>
    public virtual int GetSelectedRowIndex()
    {
        var element = FindElement();
        if (element != null)
        {
            var grid = element.AsGrid();
            if (grid != null)
            {
                for (int i = 0; i < grid.RowCount; i++)
                {
                    var row = grid.GetRowByIndex(i);
                    var selectionPattern = row?.Patterns.SelectionItem.PatternOrDefault;
                    if (selectionPattern?.IsSelected.Value == true)
                    {
                        return i;
                    }
                }
            }
        }
        return -1;
    }

    /// <summary>
    /// Get column headers as array.
    /// </summary>
    public virtual string[] GetColumnHeaders()
    {
        var element = FindElement();
        if (element != null)
        {
            var grid = element.AsGrid();
            if (grid != null)
            {
                return grid.Header?.Columns.Select(c => c.Text ?? c.Name ?? "").ToArray() 
                    ?? Array.Empty<string>();
            }
        }
        return Array.Empty<string>();
    }

    /// <summary>
    /// Click a cell to select or edit it.
    /// </summary>
    public virtual void ClickCell(int row, int column)
    {
        CheckVisible();
        
        var element = FindElement();
        if (element != null)
        {
            var grid = element.AsGrid();
            if (grid != null && row < grid.RowCount && column < grid.ColumnCount)
            {
                var cell = grid.GetRowByIndex(row)?.Cells[column];
                cell?.Click();
                LogAction("ClickCell", $"[{row},{column}]");
            }
        }
    }

    /// <summary>
    /// Double-click a cell (often to edit).
    /// </summary>
    public virtual void DoubleClickCell(int row, int column)
    {
        CheckVisible();
        
        var element = FindElement();
        if (element != null)
        {
            var grid = element.AsGrid();
            if (grid != null && row < grid.RowCount && column < grid.ColumnCount)
            {
                var cell = grid.GetRowByIndex(row)?.Cells[column];
                cell?.DoubleClick();
                LogAction("DoubleClickCell", $"[{row},{column}]");
            }
        }
    }

    /// <summary>
    /// Wait for row count.
    /// </summary>
    public bool WaitForRowCount(int expectedCount, int? timeoutMs = null)
    {
        var sw = Stopwatch.StartNew();
        var result = _context.WaitFor(
            () => GetRowCount() == expectedCount,
            timeoutMs,
            $"row count = {expectedCount}");
        LogWait($"RowCount={expectedCount}", result, (int)sw.ElapsedMilliseconds);
        return result;
    }

    /// <summary>
    /// Get grid info as text (row x column).
    /// </summary>
    public override string GetText()
    {
        return $"{GetRowCount()} rows x {GetColumnCount()} columns";
    }
}
