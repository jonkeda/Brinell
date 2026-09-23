namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidLabel<TScope> : AndroidText<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidLabel(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidLabel(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }
}
