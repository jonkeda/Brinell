namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidSeekBar<TScope> : AndroidSlider<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public AndroidSeekBar(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidSeekBar(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }
}
