using Brinell.Html.Controls.Display;
using Brinell.Html.Controls.Selection;
using Brinell.Html.Controls.Toggle;

namespace Brinell.Html.Uat.Tests.Pages;

public sealed class FormControlsPage : HtmlPageObjectBase<FormControlsPage>
{
    public FormControlsPage(IHtmlTestContext context)
        : base(context)
    {
    }

    public CheckBoxControl<FormControlsPage> TermsCheckBox => new(this, "#terms-checkbox");

    public CheckBoxControl<FormControlsPage> NewsletterCheckBox => new(this, "#newsletter-checkbox");

    public SelectControl<FormControlsPage> CountrySelect => new(this, "#country-select");

    public LabelControl<FormControlsPage> SelectStatus => new(this, "#select-status");
}
