using Brinell.Html.Controls.Collection;

namespace Brinell.Html.UITests.PageObjects;

public sealed class DataTablePage : HtmlPageObjectBase<DataTablePage>
{
    public DataTablePage(IHtmlTestContext context)
        : base(context)
    {
    }

    public TableControl<DataTablePage> DataGrid => new(this, "[data-automation-id='DataTable']");
}