using Bruinet.Core.Locators;
using Bruinet.NativeAndroid.Interfaces;

namespace Bruinet.NativeAndroid.Controls.Selection;

/// <summary>
/// A spinner (<c>android.widget.Spinner</c>, <c>AppCompatSpinner</c>): a tap opens its options in a popup list.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class Spinner<TScope> : SelectorControlBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public Spinner(INativeAndroidScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public Spinner(INativeAndroidScope<TScope> scope, string resourceIdOrXPath)
        : base(scope, resourceIdOrXPath)
    {
    }
}
