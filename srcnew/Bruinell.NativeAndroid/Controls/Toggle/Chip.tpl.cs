using Bruinell.Core.Locators;
using Bruinell.NativeAndroid.Interfaces;

namespace Bruinell.NativeAndroid.Controls.Toggle;

/// <summary>
/// A checkable chip (Material <c>Chip</c>). A chip that is not checkable is a button.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class Chip<TScope> : ToggleControlBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public Chip(INativeAndroidScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public Chip(INativeAndroidScope<TScope> scope, string resourceIdOrXPath)
        : base(scope, resourceIdOrXPath)
    {
    }
}
