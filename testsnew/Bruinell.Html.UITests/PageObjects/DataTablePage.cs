using Bruinell.Html.Controls.Collection;

namespace Bruinell.Html.UITests.PageObjects;

public sealed class DataTablePage : HtmlPageObjectBase<DataTablePage>
{
    public DataTablePage(IHtmlTestContext context)
        : base(context)
    {
    }

    public TableControl<DataTablePage> DataGrid => new(this, "[data-automation-id='DataTable']");
}