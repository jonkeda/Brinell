namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidEditor<TScope> : AndroidEditText<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidEditor(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidEditor(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }
}
