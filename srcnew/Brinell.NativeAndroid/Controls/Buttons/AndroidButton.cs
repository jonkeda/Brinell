namespace Brinell.NativeAndroid.Controls;

public class AndroidButton<TScope> : AndroidClickableControlBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidButton(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidButton(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }
}
