using Bruinet.Core.Locators;
using Bruinet.NativeAndroid.Interfaces;

namespace Bruinet.NativeAndroid.Controls.Toggle;

/// <summary>
/// A switch (<c>android.widget.Switch</c>, <c>SwitchCompat</c>, <c>MaterialSwitch</c>).
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class Switch<TScope> : ToggleControlBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public Switch(INativeAndroidScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public Switch(INativeAndroidScope<TScope> scope, string resourceIdOrXPath)
        : base(scope, resourceIdOrXPath)
    {
    }
}
