using Bruinell.Core.Interfaces;
using Bruinell.Core.Locators;
using Bruinell.Html.Interfaces;

namespace Bruinell.Html.Controls.Display;

/// <summary>
/// HTML label/text display control. Wraps any element used for text display
/// (&lt;label&gt;, &lt;span&gt;, &lt;p&gt;, &lt;h1-h6&gt;, etc.). Its text members come from
/// <see cref="ControlBase{TScope}"/>.
/// </summary>
public class LabelControl<TScope> : ControlBase<TScope>, ITextControlObject<TScope>
    where TScope : IHtmlScope<TScope>
{
    public LabelControl(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public LabelControl(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }
}
