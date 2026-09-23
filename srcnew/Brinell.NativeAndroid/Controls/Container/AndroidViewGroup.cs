namespace Brinell.NativeAndroid.Controls;

public class AndroidViewGroup<TParent> : AndroidContainerBase<TParent, AndroidViewGroup<TParent>>
    where TParent : INativeAndroidScope<TParent>
{
    public AndroidViewGroup(Locator locator, INativeAndroidScope<TParent> parentScope)
        : base(locator, parentScope)
    {
    }

    public AndroidViewGroup(string locatorValue, INativeAndroidScope<TParent> parentScope)
        : base(locatorValue, parentScope)
    {
    }
}
