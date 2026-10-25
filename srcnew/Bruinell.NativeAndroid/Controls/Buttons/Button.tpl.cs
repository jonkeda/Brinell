using Bruinell.Core.Locators;
using Bruinell.NativeAndroid.Interfaces;

namespace Bruinell.NativeAndroid.Controls.Buttons;

/// <summary>
/// A button (<c>android.widget.Button</c>, a Material button).
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class Button<TScope> : ClickableControlBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public Button(INativeAndroidScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public Button(INativeAndroidScope<TScope> scope, string resourceIdOrXPath)
        : base(scope, resourceIdOrXPath)
    {
    }
}
