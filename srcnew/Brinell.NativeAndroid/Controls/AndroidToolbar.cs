namespace Brinell.NativeAndroid.Controls;

public class AndroidToolbar<TScope> : NativeAndroidControl<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidToolbar(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidToolbar(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public TScope TapActionByContentDescription(string contentDescription, int? timeoutMs = null)
    {
        var root = FindElement(timeoutMs);
        root.FindElement(NativeAndroidLocator.ByContentDescription(contentDescription), timeoutMs ?? DefaultTimeoutMs).Click();
        return ContainingScope;
    }

    public TScope TapActionByText(string text, int? timeoutMs = null)
    {
        var root = FindElement(timeoutMs);
        root.FindElement(NativeAndroidLocator.ByTextOrDescription(text), timeoutMs ?? DefaultTimeoutMs).Click();
        return ContainingScope;
    }
}
