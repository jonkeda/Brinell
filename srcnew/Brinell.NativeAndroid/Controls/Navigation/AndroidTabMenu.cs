namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidTabMenu<TScope> : NativeAndroidControl<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidTabMenu(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidTabMenu(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public AndroidTab<TScope> Tab(string title)
        => new(NativeAndroidLocator.ByTextOrDescription(title), ContainingScope);

    public TScope Select(string title, int? timeoutMs = null)
    {
        Tab(title).Click(timeoutMs);
        return ContainingScope;
    }
}
