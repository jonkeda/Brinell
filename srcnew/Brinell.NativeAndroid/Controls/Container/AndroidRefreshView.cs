namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidRefreshView<TParent> : AndroidRefreshableContainerBase<TParent, AndroidRefreshView<TParent>>
    where TParent : INativeAndroidScope<TParent>
{
    public AndroidRefreshView(Locator locator, INativeAndroidScope<TParent> parentScope)
        : base(locator, parentScope)
    {
    }

    public AndroidRefreshView(string locatorValue, INativeAndroidScope<TParent> parentScope)
        : base(locatorValue, parentScope)
    {
    }
}
