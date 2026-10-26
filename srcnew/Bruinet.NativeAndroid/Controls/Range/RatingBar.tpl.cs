using System.Globalization;
using Bruinet.Core.Locators;
using Bruinet.NativeAndroid.Interfaces;

namespace Bruinet.NativeAndroid.Controls.Range;

/// <summary>
/// A rating bar (<c>android.widget.RatingBar</c>): its value is the rating in stars. Android reports
/// its progress in steps, so the step size the page object gives converts it (3.5 stars in half
/// steps is progress 7). It is set by a tap on the star, as a user does: Android's set-progress
/// action moves a rating bar without telling the app.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class RatingBar<TScope> : RangeControlBase<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    public RatingBar(INativeAndroidScope<TScope> scope, Locator locator, double minimum = 0, double? maximum = 5, double step = 0.5)
        : base(scope, locator, minimum, maximum, step)
    {
    }

    public RatingBar(INativeAndroidScope<TScope> scope, string resourceIdOrXPath, double minimum = 0, double? maximum = 5, double step = 0.5)
        : base(scope, resourceIdOrXPath, minimum, maximum, step)
    {
    }

    /// <summary>The rating in stars: the progress in steps, times the step size.</summary>
    /// <param name="element">The pre-found view.</param>
    protected override double? GetValueCore(INativeAndroidElement? element)
        => base.GetValueCore(element) * GetStepCore(element);

    /// <summary>
    /// Taps the rating bar once inside the last step of <paramref name="value"/> stars, and confirms
    /// the rating. A touch on a step's edge goes up a step (measured on the emulator: 70% of five
    /// stars in half steps gave 4), so the tap lands a quarter step before the edge.
    /// </summary>
    /// <param name="element">The pre-found view.</param>
    /// <param name="value">The rating in stars; null skips.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected override void SetValueCore(INativeAndroidElement element, double? value, int? timeoutMs = null)
    {
        if (value is not { } stars || GetMaximumCore(element) is not { } maximum || GetStepCore(element) is not { } step)
        {
            return;
        }

        var across = Math.Clamp((stars - (step / 4)) / maximum, 0.01, 0.99);
        ActAndConfirm(
            "SetValue", () => element.TapAt(across, 0.5),
            () => GetValueCore(element), actual => actual == stars,
            $"{stars.ToString(CultureInfo.InvariantCulture)} stars", timeoutMs);
    }
}
