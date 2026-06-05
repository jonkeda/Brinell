namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidExpander<TParent> : AndroidExpandableContainerBase<TParent, AndroidExpander<TParent>>
    where TParent : INativeAndroidScope<TParent>
{
    public AndroidExpander(Locator locator, INativeAndroidScope<TParent> parentScope)
        : base(locator, parentScope)
    {
    }

    public AndroidExpander(string locatorValue, INativeAndroidScope<TParent> parentScope)
        : base(locatorValue, parentScope)
    {
    }
}
