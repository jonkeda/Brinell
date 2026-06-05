namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidActivityIndicator<TScope> : AndroidProgressBar<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidActivityIndicator(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidActivityIndicator(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public override bool? IsIndeterminate()
        => TryFindElement() is null ? null : true;
}
