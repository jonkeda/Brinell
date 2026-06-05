namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidRadioButton<TScope> : AndroidToggleControlBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidRadioButton(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidRadioButton(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public TScope Select(int? timeoutMs = null)
        => Check(timeoutMs);
}
