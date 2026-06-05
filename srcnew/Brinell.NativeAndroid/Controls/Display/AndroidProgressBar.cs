namespace Brinell.NativeAndroid.Controls;

public class AndroidProgressBar<TScope> : AndroidProgressControlBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidProgressBar(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidProgressBar(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }
}
