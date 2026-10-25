using Bruinell.Core.Locators;
using Bruinell.NativeAndroid.Interfaces;

namespace Bruinell.NativeAndroid.Controls.Selection;

/// <summary>A radio group (<c>android.widget.RadioGroup</c>): its radio buttons are its options.</summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class RadioGroup<TScope> : ChoiceGroupBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public RadioGroup(INativeAndroidScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public RadioGroup(INativeAndroidScope<TScope> scope, string resourceIdOrXPath)
        : base(scope, resourceIdOrXPath)
    {
    }

    /// <inheritdoc />
    protected override Locator ChoicesLocator => Locator.ByClassName("android.widget.RadioButton");

    /// <inheritdoc />
    protected override bool IsChosen(INativeAndroidElement choice) => choice.Checked == true;
}
