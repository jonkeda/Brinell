namespace Brinell.NativeAndroid.Controls;

public class AndroidPicker<TScope> : AndroidSelectorControlBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidPicker(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidPicker(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }
}
