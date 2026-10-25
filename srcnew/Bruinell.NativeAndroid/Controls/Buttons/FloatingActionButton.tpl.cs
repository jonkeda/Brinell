using Bruinell.Core.Locators;
using Bruinell.NativeAndroid.Interfaces;

namespace Bruinell.NativeAndroid.Controls.Buttons;

/// <summary>
/// A floating action button (Material); find it by its content description.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class FloatingActionButton<TScope> : ClickableControlBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public FloatingActionButton(INativeAndroidScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public FloatingActionButton(INativeAndroidScope<TScope> scope, string resourceIdOrXPath)
        : base(scope, resourceIdOrXPath)
    {
    }
}
