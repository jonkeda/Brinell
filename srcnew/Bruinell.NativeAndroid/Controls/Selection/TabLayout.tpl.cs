using Bruinell.Core.Locators;
using Bruinell.NativeAndroid.Interfaces;

namespace Bruinell.NativeAndroid.Controls.Selection;

/// <summary>
/// Material tabs (<c>TabLayout</c>): each tab is a view that carries its title as its content
/// description, and the selected one reports itself selected.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class TabLayout<TScope> : ChoiceGroupBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public TabLayout(INativeAndroidScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public TabLayout(INativeAndroidScope<TScope> scope, string resourceIdOrXPath)
        : base(scope, resourceIdOrXPath)
    {
    }

    /// <inheritdoc />
    protected override Locator ChoicesLocator => Locator.ByXPath(".//android.widget.LinearLayout[@content-desc]");

    /// <inheritdoc />
    protected override bool IsChosen(INativeAndroidElement choice) => choice.Selected;
}
