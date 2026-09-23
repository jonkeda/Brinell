namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidTableView<TScope> : AndroidRecyclerView<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidTableView(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidTableView(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }
}
