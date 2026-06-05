namespace Brinell.NativeAndroid.Controls;

public class AndroidScrollView<TParent> : AndroidScrollableContainerBase<TParent, AndroidScrollView<TParent>>
    where TParent : INativeAndroidScope<TParent>
{
    public AndroidScrollView(Locator locator, INativeAndroidScope<TParent> parentScope)
        : base(locator, parentScope)
    {
    }

    public AndroidScrollView(string locatorValue, INativeAndroidScope<TParent> parentScope)
        : base(locatorValue, parentScope)
    {
    }
}
