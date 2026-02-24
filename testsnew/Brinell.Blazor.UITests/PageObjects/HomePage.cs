using Brinell.Html.Controls.Display;

namespace Brinell.Blazor.UITests.PageObjects;

public sealed class HomePage : BlazorPageObjectBase<HomePage>
{
    public HomePage(IHtmlTestContext context)
        : base(context)
    {
    }

    public LabelControl<HomePage> HomeTitle => new(this, "[data-testid='home-title']");

    public LinkControl<HomePage> CounterLink => new(this, "[data-testid='counter-link']");

    public LinkControl<HomePage> LoginLink => new(this, "[data-testid='login-link']");

    public LabelControl<HomePage> WelcomeMessage => new(this, "[data-testid='welcome-message']");
}
