namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidSearchBar<TScope> : AndroidEditText<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidSearchBar(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidSearchBar(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public TScope SubmitSearch(int? timeoutMs = null)
    {
        FindElementForAction(timeoutMs).SendKeys(Keys.Enter);
        return ContainingScope;
    }

    public TScope ClearSearch(int? timeoutMs = null)
        => Clear(timeoutMs);
}
