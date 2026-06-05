namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidSwitch<TScope> : AndroidToggleControlBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidSwitch(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidSwitch(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }
}
