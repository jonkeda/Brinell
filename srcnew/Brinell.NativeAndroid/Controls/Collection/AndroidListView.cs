namespace Brinell.NativeAndroid.Controls;

public class AndroidListView<TScope> : AndroidRecyclerView<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidListView(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidListView(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }
}
