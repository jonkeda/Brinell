namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidCarouselView<TScope> : AndroidRecyclerView<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidCarouselView(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidCarouselView(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }
}
