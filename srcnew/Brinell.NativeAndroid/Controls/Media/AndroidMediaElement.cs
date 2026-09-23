namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidMediaElement<TScope> : AndroidButton<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidMediaElement(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidMediaElement(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public TScope TogglePlayback(int? timeoutMs = null)
        => Click(timeoutMs);
}
