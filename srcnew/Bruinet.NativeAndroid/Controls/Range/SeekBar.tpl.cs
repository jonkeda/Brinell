using Bruinet.Core.Locators;
using Bruinet.NativeAndroid.Interfaces;

namespace Bruinet.NativeAndroid.Controls.Range;

/// <summary>
/// A seek bar (<c>android.widget.SeekBar</c>, Material's slider). Give its maximum; Android does not report it.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class SeekBar<TScope> : RangeControlBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public SeekBar(INativeAndroidScope<TScope> scope, Locator locator, double minimum = 0, double? maximum = null, double step = 1)
        : base(scope, locator, minimum, maximum, step)
    {
    }

    public SeekBar(INativeAndroidScope<TScope> scope, string resourceIdOrXPath, double minimum = 0, double? maximum = null, double step = 1)
        : base(scope, resourceIdOrXPath, minimum, maximum, step)
    {
    }
}
