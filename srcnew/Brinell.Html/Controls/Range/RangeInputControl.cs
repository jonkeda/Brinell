using System.Globalization;
using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Controls.Range;

public class RangeInputControl<TScope> : RangeControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public RangeInputControl(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public RangeInputControl(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    public double GetNumericValue()
    {
        return double.TryParse(GetValue(), NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }

    public TScope SetNumericValue(double value)
    {
        return SetValue(value.ToString(CultureInfo.InvariantCulture));
    }

    public TScope AssertNumericValue(double expected)
    {
        var actual = GetNumericValue();
        if (Math.Abs(actual - expected) > 0.01)
        {
            throw new AssertionException(
                $"Range value mismatch. Expected: {expected}, Actual: {actual}");
        }

        return ContainingScope;
    }
}
