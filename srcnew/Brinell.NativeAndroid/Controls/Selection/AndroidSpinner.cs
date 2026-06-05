namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidSpinner<TScope> : AndroidPicker<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidSpinner(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidSpinner(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }
}
