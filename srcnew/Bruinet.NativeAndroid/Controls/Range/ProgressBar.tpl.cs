using Bruinet.Core.Locators;
using Bruinet.NativeAndroid.Interfaces;

namespace Bruinet.NativeAndroid.Controls.Range;

/// <summary>
/// A determinate progress bar (<c>android.widget.ProgressBar</c>): read and asserted; the app sets it.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class ProgressBar<TScope> : RangeControlBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public ProgressBar(INativeAndroidScope<TScope> scope, Locator locator, double minimum = 0, double? maximum = 100, double step = 1)
        : base(scope, locator, minimum, maximum, step)
    {
    }

    public ProgressBar(INativeAndroidScope<TScope> scope, string resourceIdOrXPath, double minimum = 0, double? maximum = 100, double step = 1)
        : base(scope, resourceIdOrXPath, minimum, maximum, step)
    {
    }
}
