namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidWebView<TParent> : AndroidContainerBase<TParent, AndroidWebView<TParent>>
    where TParent : INativeAndroidScope<TParent>
{
    public AndroidWebView(Locator locator, INativeAndroidScope<TParent> parentScope)
        : base(locator, parentScope)
    {
    }

    public AndroidWebView(string locatorValue, INativeAndroidScope<TParent> parentScope)
        : base(locatorValue, parentScope)
    {
    }

    public TParent SwitchToWebContext(string? contains = null, int timeoutMs = 5000)
    {
        if (!Context.Driver.TrySwitchToWebContext(contains, timeoutMs))
        {
            throw new TimeoutException("No Android WEBVIEW context became available.");
        }

        return ContainingScope;
    }

    public TParent SwitchToNativeApp()
    {
        Context.Driver.SwitchToNativeApp();
        return ContainingScope;
    }
}
