namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidLink<TScope> : AndroidButton<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidLink(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidLink(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }
}
