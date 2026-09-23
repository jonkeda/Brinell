namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidContentDialog<TScope> : AndroidDialog<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidContentDialog(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidContentDialog(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }
}
