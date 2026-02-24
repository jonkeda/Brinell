using Brinell.Html.Controls.Display;

namespace Brinell.Blazor.UITests.PageObjects;

public sealed class CounterPage : BlazorPageObjectBase<CounterPage>
{
    public CounterPage(IHtmlTestContext context)
        : base(context)
    {
    }

    public LabelControl<CounterPage> CounterTitle => new(this, "[data-testid='counter-title']");

    public LabelControl<CounterPage> CountDisplay => new(this, "[data-testid='count-display']");

    public ButtonControl<CounterPage> IncrementButton => new(this, "[data-testid='increment-btn']");

    public ButtonControl<CounterPage> ResetButton => new(this, "[data-testid='reset-btn']");
}
