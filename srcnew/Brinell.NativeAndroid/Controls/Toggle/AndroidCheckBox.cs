namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidCheckBox<TScope> : AndroidToggleControlBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidCheckBox(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidCheckBox(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }
}
