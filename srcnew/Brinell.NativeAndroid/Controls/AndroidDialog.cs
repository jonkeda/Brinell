namespace Brinell.NativeAndroid.Controls;

public class AndroidDialog<TScope> : NativeAndroidControl<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidDialog(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidDialog(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public bool HasText(string text, int? timeoutMs = null)
    {
        try
        {
            var root = FindElement(timeoutMs);
            root.FindElement(NativeAndroidLocator.ByTextContains(text), timeoutMs ?? DefaultTimeoutMs);
            return true;
        }
        catch (ElementNotFoundException)
        {
            return false;
        }
    }

    public TScope TapButtonByText(string text, int? timeoutMs = null)
    {
        var root = FindElement(timeoutMs);
        root.FindElement(NativeAndroidLocator.ByTextOrDescription(text), timeoutMs ?? DefaultTimeoutMs).Click();
        return ContainingScope;
    }
}
