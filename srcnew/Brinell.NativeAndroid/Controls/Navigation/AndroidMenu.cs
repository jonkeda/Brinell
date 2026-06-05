namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidMenu<TScope> : NativeAndroidControl<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidMenu(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidMenu(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public TScope TapItemByText(string text, int? timeoutMs = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        FindElement(timeoutMs).FindElement(NativeAndroidLocator.ByTextOrDescription(text), timeoutMs ?? DefaultTimeoutMs).Click();
        return ContainingScope;
    }
}
