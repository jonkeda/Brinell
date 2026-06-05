namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidRoundButton<TScope> : AndroidButton<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidRoundButton(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidRoundButton(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }
}
