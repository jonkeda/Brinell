using Brinell.Html.Controls.Buttons;
using Brinell.Html.Controls.Display;

namespace Brinell.Html.Uat.Tests.Pages;

public sealed class CounterPage : HtmlPageObjectBase<CounterPage>
{
    public CounterPage(IHtmlTestContext context)
        : base(context)
    {
    }

    public LabelControl<CounterPage> CountDisplay => new(this, "[data-testid='count-display']");

    public ButtonControl<CounterPage> IncrementButton => new(this, "[data-testid='increment-btn']");
}
