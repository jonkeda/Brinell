using Brinell.Html.Controls.Display;
using Brinell.Html.Controls.Selection;
using Brinell.Html.Controls.Text;
using Brinell.Html.Controls.Toggle;

namespace Brinell.Html.UITests.Uat.Pages;

[TestPage("Form Controls")]
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

    public TextAreaControl<FormControlsPage> Comments => new(this, "#comments-textarea");
}
