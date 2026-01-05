using Brinell.Html.Controls;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Samples.Blazor.UITests.PageObjects;

/// <summary>
/// Page object for the DataTable page.
/// </summary>
public class DataTablePage : PageBase
{
    public override string AutomationId => "[data-automation-id='DataTableTitle']";

    // ═══════════════════════════════════════════════════════════════
    // HEADER CONTROLS
    // ═══════════════════════════════════════════════════════════════

    public LabelControl DataTableTitle { get; }

    // ═══════════════════════════════════════════════════════════════
    // FILTER CONTROLS
    // ═══════════════════════════════════════════════════════════════

    public TextInputControl SearchInput { get; }
    public SelectControl CategoryFilter { get; }
    public SelectControl StatusFilter { get; }
    public ButtonControl ClearFiltersButton { get; }

    // ═══════════════════════════════════════════════════════════════
    // BULK ACTIONS
    // ═══════════════════════════════════════════════════════════════

    public ButtonControl SelectAllButton { get; }
    public ButtonControl ClearSelectionButton { get; }
    public ButtonControl DeleteSelectedButton { get; }
    public LabelControl RecordCount { get; }

    // ═══════════════════════════════════════════════════════════════
    // TABLE
    // ═══════════════════════════════════════════════════════════════

    public TableControl DataTable { get; }
    public CheckBoxControl SelectAllCheckbox { get; }
    public LabelControl NoDataMessage { get; }

    // ═══════════════════════════════════════════════════════════════
    // PAGINATION
    // ═══════════════════════════════════════════════════════════════

    public ButtonControl PrevPageButton { get; }
    public ButtonControl NextPageButton { get; }
    public SelectControl PageSizeSelector { get; }
    public ButtonControl RefreshButton { get; }

    public DataTablePage(SeleniumTestContext context) : base(context)
    {
        DataTableTitle = new LabelControl(context, this, "[data-automation-id='DataTableTitle']");

        // Filter
        SearchInput = new TextInputControl(context, this, "[data-automation-id='SearchInput']");
        CategoryFilter = new SelectControl(context, this, "[data-automation-id='CategoryFilter']");
        StatusFilter = new SelectControl(context, this, "[data-automation-id='StatusFilter']");
        ClearFiltersButton = new ButtonControl(context, this, "[data-automation-id='ClearFiltersButton']");

        // Bulk actions
        SelectAllButton = new ButtonControl(context, this, "[data-automation-id='SelectAllButton']");
        ClearSelectionButton = new ButtonControl(context, this, "[data-automation-id='ClearSelectionButton']");
        DeleteSelectedButton = new ButtonControl(context, this, "[data-automation-id='DeleteSelectedButton']");
        RecordCount = new LabelControl(context, this, "[data-automation-id='RecordCount']");

        // Table
        DataTable = new TableControl(context, this, "[data-automation-id='DataTable']");
        SelectAllCheckbox = new CheckBoxControl(context, this, "[data-automation-id='SelectAllCheckbox']");
        NoDataMessage = new LabelControl(context, this, "[data-automation-id='NoDataMessage']");

        // Pagination
        PrevPageButton = new ButtonControl(context, this, "[data-automation-id='PrevPageButton']");
        NextPageButton = new ButtonControl(context, this, "[data-automation-id='NextPageButton']");
        PageSizeSelector = new SelectControl(context, this, "[data-automation-id='PageSizeSelector']");
        RefreshButton = new ButtonControl(context, this, "[data-automation-id='RefreshButton']");
    }

    public override bool IsDisplayed()
    {
        return DataTableTitle.IsVisible();
    }

    // ═══════════════════════════════════════════════════════════════
    // WORKFLOW METHODS
    // ═══════════════════════════════════════════════════════════════

    public DataTablePage SearchFor(string text)
    {
        Log($"SearchFor({text})");
        SearchInput.ClearAndEnter(text);
        return this;
    }

    public DataTablePage FilterByCategory(string category)
    {
        Log($"FilterByCategory({category})");
        CategoryFilter.SelectByText(category);
        return this;
    }

    public DataTablePage FilterByStatus(string status)
    {
        Log($"FilterByStatus({status})");
        StatusFilter.SelectByText(status);
        return this;
    }

    public DataTablePage ClearFilters()
    {
        Log("ClearFilters()");
        ClearFiltersButton.Click();
        return this;
    }

    public DataTablePage GoToNextPage()
    {
        Log("GoToNextPage()");
        NextPageButton.Click();
        return this;
    }

    public DataTablePage GoToPrevPage()
    {
        Log("GoToPrevPage()");
        PrevPageButton.Click();
        return this;
    }

    public DataTablePage SetPageSize(int size)
    {
        Log($"SetPageSize({size})");
        PageSizeSelector.SelectByText(size.ToString());
        return this;
    }

    public DataTablePage Refresh()
    {
        Log("Refresh()");
        RefreshButton.Click();
        return this;
    }
}
