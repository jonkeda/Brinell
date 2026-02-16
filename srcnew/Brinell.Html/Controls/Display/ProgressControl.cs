using System.Globalization;
using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Controls.Display;

public class ProgressControl<TScope> : ControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public ProgressControl(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public ProgressControl(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    public double GetValue()
    {
        return double.TryParse(GetAttribute("value"), NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }

    public double GetMax()
    {
        return double.TryParse(GetAttribute("max"), NumberStyles.Any, CultureInfo.InvariantCulture, out var max)
            ? max
            : 100;
    }

    public double GetPercentage()
    {
        var max = GetMax();
        return max > 0 ? (GetValue() / max) * 100 : 0;
    }

    public TScope AssertValue(double expected)
    {
        var actual = GetValue();
        if (Math.Abs(actual - expected) > 0.01)
        {
            throw new AssertionException(
                $"Progress value mismatch. Expected: {expected}, Actual: {actual}");
        }

        return ContainingScope;
    }
}
