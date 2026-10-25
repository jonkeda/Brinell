using Bruinell.Html.Controls.Buttons;
using Bruinell.Html.Controls.Display;

namespace Bruinell.Html.UITests.Uat.Pages;

[TestPage("Counter")]
public sealed class CounterPage : HtmlPageObjectBase<CounterPage>
{
    public CounterPage(IHtmlTestContext context)
        : base(context)
    {
    }

    public LabelControl<CounterPage> CountDisplay => new(this, "[data-testid='count-display']");

    public ButtonControl<CounterPage> IncrementButton => new(this, "[data-testid='increment-btn']");
}
