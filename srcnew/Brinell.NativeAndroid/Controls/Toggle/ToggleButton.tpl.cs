using Brinell.Core.Locators;
using Brinell.NativeAndroid.Interfaces;

namespace Brinell.NativeAndroid.Controls.Toggle;

/// <summary>
/// A toggle button (<c>android.widget.ToggleButton</c>).
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class ToggleButton<TScope> : ToggleControlBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public ToggleButton(INativeAndroidScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public ToggleButton(INativeAndroidScope<TScope> scope, string resourceIdOrXPath)
        : base(scope, resourceIdOrXPath)
    {
    }
}
