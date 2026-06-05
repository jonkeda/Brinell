namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidGrid<TParent> : AndroidContainerBase<TParent, AndroidGrid<TParent>>
    where TParent : INativeAndroidScope<TParent>
{
    public AndroidGrid(Locator locator, INativeAndroidScope<TParent> parentScope)
        : base(locator, parentScope)
    {
    }

    public AndroidGrid(string locatorValue, INativeAndroidScope<TParent> parentScope)
        : base(locatorValue, parentScope)
    {
    }
}
