using Brinell.Core.Locators;
using Brinell.NativeAndroid.Interfaces;

namespace Brinell.NativeAndroid.Controls.Toggle;

/// <summary>
/// A radio button (<c>android.widget.RadioButton</c>). Checking one unchecks the others in its group; unchecking one has no effect on Android.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class RadioButton<TScope> : ToggleControlBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public RadioButton(INativeAndroidScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public RadioButton(INativeAndroidScope<TScope> scope, string resourceIdOrXPath)
        : base(scope, resourceIdOrXPath)
    {
    }
}
