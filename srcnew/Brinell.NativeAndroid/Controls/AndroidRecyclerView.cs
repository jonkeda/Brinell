namespace Brinell.NativeAndroid.Controls;

public class AndroidRecyclerView<TScope> : NativeAndroidControl<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidRecyclerView(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidRecyclerView(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public IReadOnlyList<NativeAndroidElement> Items(int? timeoutMs = null)
    {
        var root = FindElement(timeoutMs);
        return root.FindElements(NativeAndroidLocator.ByClass("android.view.ViewGroup"));
    }

    public NativeAndroidElement FindItemByText(string text, int? timeoutMs = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var root = FindElement(timeoutMs);
        var literal = NativeAndroidByExtensions.ToXPathLiteral(text);
        var locator = Locator.ByXPath($".//*[contains(@text, {literal}) or contains(@content-desc, {literal})]");
        return root.FindElement(locator, timeoutMs ?? DefaultTimeoutMs);
    }

    public bool ContainsText(string text, int? timeoutMs = null)
    {
        try
        {
            FindItemByText(text, timeoutMs);
            return true;
        }
        catch (ElementNotFoundException)
        {
            return false;
        }
    }

    public TScope TapItemByText(string text, int? timeoutMs = null)
    {
        FindItemByText(text, timeoutMs).Click();
        return ContainingScope;
    }

    public TScope AssertContainsText(string text, string? message = null, int? timeoutMs = null)
    {
        if (!ContainsText(text, timeoutMs))
        {
            Fail(message ?? $"Expected list to contain text '{text}'.", text, null);
        }

        return ContainingScope;
    }
}
