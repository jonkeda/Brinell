using Bruinell.Html.Controls.Display;
using Bruinell.Html.Controls.Selection;
using Bruinell.Html.Controls.Text;
using Bruinell.Html.Controls.Toggle;

namespace Bruinell.Html.UITests.Uat.Pages;

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
