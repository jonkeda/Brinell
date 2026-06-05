namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidImageButton<TScope> : AndroidButton<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidImageButton(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidImageButton(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }
}
