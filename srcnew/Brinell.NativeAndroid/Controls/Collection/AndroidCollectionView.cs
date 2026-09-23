namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidCollectionView<TScope> : AndroidRecyclerView<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidCollectionView(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidCollectionView(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }
}
