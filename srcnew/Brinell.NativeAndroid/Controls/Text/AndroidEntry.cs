namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidEntry<TScope> : AndroidEditText<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidEntry(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidEntry(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }
}
