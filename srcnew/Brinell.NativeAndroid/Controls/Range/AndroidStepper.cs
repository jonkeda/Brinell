namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidStepper<TScope> : AndroidRangeControlBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidStepper(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidStepper(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public TScope TapIncrement(int? timeoutMs = null)
        => TapChildByTextOrDescription("+", timeoutMs);

    public TScope TapDecrement(int? timeoutMs = null)
        => TapChildByTextOrDescription("-", timeoutMs);

    private TScope TapChildByTextOrDescription(string text, int? timeoutMs)
    {
        var root = FindElementForAction(timeoutMs);
        root.FindElement(NativeAndroidLocator.ByTextOrDescription(text), timeoutMs ?? DefaultTimeoutMs).Click();
        return ContainingScope;
    }
}
