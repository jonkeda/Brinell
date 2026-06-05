namespace Brinell.NativeAndroid.Controls;

public class AndroidSlider<TScope> : AndroidRangeControlBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidSlider(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidSlider(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }
}
