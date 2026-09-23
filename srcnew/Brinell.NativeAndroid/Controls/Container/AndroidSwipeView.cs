namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidSwipeView<TParent> : AndroidScrollableContainerBase<TParent, AndroidSwipeView<TParent>>
    where TParent : INativeAndroidScope<TParent>
{
    public AndroidSwipeView(Locator locator, INativeAndroidScope<TParent> parentScope)
        : base(locator, parentScope)
    {
    }

    public AndroidSwipeView(string locatorValue, INativeAndroidScope<TParent> parentScope)
        : base(locatorValue, parentScope)
    {
    }
}
