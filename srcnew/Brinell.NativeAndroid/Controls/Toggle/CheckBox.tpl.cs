using Brinell.Core.Locators;
using Brinell.NativeAndroid.Interfaces;

namespace Brinell.NativeAndroid.Controls.Toggle;

/// <summary>
/// A check box (<c>android.widget.CheckBox</c>).
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class CheckBox<TScope> : ToggleControlBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public CheckBox(INativeAndroidScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public CheckBox(INativeAndroidScope<TScope> scope, string resourceIdOrXPath)
        : base(scope, resourceIdOrXPath)
    {
    }
}
