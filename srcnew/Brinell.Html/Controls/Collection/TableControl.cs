using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Controls.Collection;

public class TableControl<TScope> : ControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public TableControl(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public TableControl(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    public int RowCount
    {
        get
        {
            var root = FindElement();
            return root.FindElements(Locator.ByCss("tbody tr")).Count;
        }
    }

    public int ColumnCount
    {
        get
        {
            var root = FindElement();
            var headerCells = root.FindElements(Locator.ByCss("thead th"));
            if (headerCells.Count > 0)
            {
                return headerCells.Count;
            }

            var firstRowCells = root.FindElements(Locator.ByCss("tbody tr:first-child td"));
            return firstRowCells.Count;
        }
    }

    public string? GetCellText(int row, int column)
    {
        if (row < 0 || column < 0)
        {
            return null;
        }

        var root = FindElement();
        var cells = root.FindElements(Locator.ByCss($"tbody tr:nth-child({row + 1}) td"));
        return column < cells.Count ? cells[column].Text : null;
    }

    public string? GetHeaderText(int column)
    {
        if (column < 0)
        {
            return null;
        }

        var root = FindElement();
        var headers = root.FindElements(Locator.ByCss("thead th"));
        return column < headers.Count ? headers[column].Text : null;
    }

    public IReadOnlyList<string?> GetRowTexts(int row)
    {
        if (row < 0)
        {
            return [];
        }

        var root = FindElement();
        var cells = root.FindElements(Locator.ByCss($"tbody tr:nth-child({row + 1}) td"));
        return cells.Select(cell => cell.Text).ToList();
    }
}
