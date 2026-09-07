using Brinell.Core;
using Brinell.Core.Utilities;
using System.Linq;

namespace Brinell.Maui.Controls.DateTimes;

/// <summary>
/// MAUI TimePicker control for time selection.
/// Provides GetTime, SetTime, and time assertion methods.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class TimePicker<TScope> : Base.FocusableControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new time picker control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the time picker element.</param>
    public TimePicker(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new time picker control within the specified scope using a string locator value.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public TimePicker(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Format

    private string? _format;
    private System.Globalization.CultureInfo? _culture;

    /// <summary>
    /// Declares the format this control's time is rendered and entered in.
    /// </summary>
    /// <param name="format">A .NET time format string, e.g. <c>h:mm tt</c>.</param>
    /// <param name="culture">Culture for the format. Defaults to <see cref="DateTimeFormats.Culture"/>.</param>
    public TimePicker<TScope> WithFormat(string format, System.Globalization.CultureInfo? culture = null)
    {
        _format = format ?? throw new ArgumentNullException(nameof(format));
        _culture = culture;
        return this;
    }

    /// <summary>Gets the format in force: the control's own, else the suite default.</summary>
    protected string Format => _format ?? DateTimeFormats.Time;

    /// <summary>Gets the culture in force: the control's own, else the suite default.</summary>
    protected System.Globalization.CultureInfo Culture => _culture ?? DateTimeFormats.Culture;

    /// <summary>
    /// Parses a rendered time using the declared format.
    /// </summary>
    /// <remarks>
    /// WinUI appends ' time picker' to the flyout button's name, so the accessible string reads
    /// ' 3:06 PM time picker' where the screen shows 3:06 PM. That suffix comes off before parsing.
    /// A declared format is the only one tried; without one the suite default is tried first and
    /// the culture's own patterns second.
    /// </remarks>
    protected TimeSpan? ParseTime(string? text)
    {
        var cleaned = DateTimeFormats.Clean(text);
        cleaned = System.Text.RegularExpressions.Regex.Replace(
            cleaned, @"\s*time\s*picker\s*$", string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

        if (cleaned.Length == 0) return null;

        if (System.DateTime.TryParseExact(cleaned, Format, Culture,
                System.Globalization.DateTimeStyles.None, out var exact))
            return exact.TimeOfDay;

        if (_format != null)
        {
            throw new BrinellException(
                $"Could not read a time from '{cleaned}' using format '{Format}' " +
                $"({Culture.Name}). Locator: {Locator}");
        }

        return System.DateTime.TryParse(cleaned, Culture,
            System.Globalization.DateTimeStyles.None, out var loose)
            ? loose.TimeOfDay
            : null;
    }

    #endregion

    #region Time - Core Methods

    // Named ReadTime rather than GetTimeCore so the generated exact-equality
    // trio lands on TimeValue. AssertTime/WaitTime compare within a tolerance, which the
    // generated equality comparison cannot express, so those stay hand-written below
    // and keep their original signatures (including the defaulted toleranceSeconds).

    /// <summary>
    /// Reads the time the control is showing.
    /// </summary>
    /// <remarks>
    /// The TimePicker root publishes no patterns at all on Windows - the value lives on its
    /// FlyoutButton child, which is why this reads through to it rather than asking the root.
    /// </remarks>
    protected virtual TimeSpan? ReadTime(IMauiElement? element)
    {
        if (element == null) return null;

        foreach (var child in element.FindElements(Locator.ByAutomationId("FlyoutButton")))
        {
            var childTime = ParseTime(child.Name) ?? ParseTime(child.Text);
            if (childTime != null) return childTime;
        }

        return ParseTime(element.Name) ?? ParseTime(element.Text);
    }

    /// <summary>
    /// Sets the time without using the pointer.
    /// </summary>
    /// <remarks>
    /// Opens the flyout by Invoke, sets hour, minute and period by their SelectionItem patterns,
    /// and commits with Accept. No coordinates at any step.
    /// </remarks>
    protected virtual void SetTimeCore(IMauiElement element, TimeSpan? time, int? timeoutMs = null)
    {
        if (time == null) return;

        Scope.WaitReady(timeoutMs ?? DefaultTimeoutMs);

        var failure = TrySetByFlyout(element, time.Value, timeoutMs);
        if (failure == null) return;

        throw new BrinellException(
            $"Could not set time {time.Value} without the pointer: {failure}. Locator: {Locator}");
    }

    /// <summary>
    /// Opens the time flyout by pattern and picks hour, minute and period.
    /// </summary>
    /// <remarks>
    /// Measured against WinUI's TimePicker: the flyout carries HourLoopingSelector,
    /// MinuteLoopingSelector and PeriodLoopingSelector - each a List of ListItems with a
    /// SelectionItem pattern - plus AcceptButton and DismissButton, both Invoke-able. The hour
    /// list is a 12-hour clock named 12, 1 .. 11, so the hour has to be converted.
    /// </remarks>
    private string? TrySetByFlyout(IMauiElement element, TimeSpan time, int? timeoutMs)
    {
        var button = element.FindElements(Locator.ByAutomationId("FlyoutButton")).FirstOrDefault()
                     ?? element;

        if (button is not IInvokePatternElement invoke || !invoke.SupportsInvokePattern)
            return "the flyout button advertises no Invoke pattern";

        if (!invoke.InvokePattern())
            return "Invoke on the flyout button was refused";

        if (!WaitForFlyout(timeoutMs))
            return "the flyout did not open";

        var hour12 = time.Hours % 12 == 0 ? 12 : time.Hours % 12;
        var period = time.Hours < 12 ? 0 : 1;

        if (!SelectInLooper("HourLoopingSelector", n => n == hour12))
            return $"no selectable hour '{hour12}' in the flyout";

        if (!SelectInLooper("MinuteLoopingSelector", n => n == time.Minutes))
            return $"no selectable minute '{time.Minutes}' in the flyout";

        // A 24-hour locale renders no period list; its absence is not a failure.
        SelectPeriod(period);

        var accept = Context.TryFindElement(Locator.ByAutomationId("AcceptButton"));
        if (accept is not IInvokePatternElement acceptInvoke || !acceptInvoke.InvokePattern())
            return "Accept was not available or was refused";

        WaitHelper.Pause(PollingIntervalMs);

        if (WaitForTime(time)) return null;

        var actual = MauiScope.TryFindElement(Locator) is { } e ? ReadTime(e) : null;
        var seconds = time.Seconds != 0
            ? " The requested time carries seconds, and the WinUI flyout selects hours and minutes only."
            : string.Empty;
        return $"the control reports '{actual}' after Accept, not {time}.{seconds}";
    }

    private bool WaitForFlyout(int? timeoutMs)
    {
        var deadline = System.DateTime.UtcNow.AddMilliseconds(timeoutMs ?? DefaultTimeoutMs);
        while (System.DateTime.UtcNow < deadline)
        {
            if (Context.TryFindElement(Locator.ByAutomationId("AcceptButton")) != null)
                return true;
            WaitHelper.Pause(PollingIntervalMs);
        }

        return false;
    }

    private bool SelectInLooper(string automationId, Func<int, bool> matches)
    {
        var looper = Context.TryFindElement(Locator.ByAutomationId(automationId));
        if (looper == null) return false;

        foreach (var item in looper.FindElements(Locator.ByControlType("ListItem")))
        {
            var name = DateTimeFormats.Clean(item.Name);
            if (!int.TryParse(name, System.Globalization.NumberStyles.Integer, Culture, out var number))
                continue;
            if (!matches(number)) continue;

            return item is ISelectionItemPatternElement selectable
                   && selectable.SupportsSelectionItemPattern
                   && selectable.SelectItemPattern();
        }

        return false;
    }

    private void SelectPeriod(int index)
    {
        var looper = Context.TryFindElement(Locator.ByAutomationId("PeriodLoopingSelector"));
        if (looper == null) return;

        var items = looper.FindElements(Locator.ByControlType("ListItem"));
        if (index >= items.Count) return;

        if (items[index] is ISelectionItemPatternElement selectable
            && selectable.SupportsSelectionItemPattern)
        {
            selectable.SelectItemPattern();
        }
    }

    /// <summary>
    /// Polls until the control reports the time to the minute, or the wait runs out.
    /// </summary>
    /// <remarks>
    /// A value that disagrees is a failure. A value that cannot be read is not: at midnight the
    /// WinUI flyout button's name comes back as ' time picker' with no time in it and no text
    /// descendants to fall back on, so the control publishes nothing to check against even though
    /// the set worked. Treating unreadable as failure turned a working midnight set into an error.
    /// </remarks>
    private bool WaitForTime(TimeSpan time)
    {
        var deadline = System.DateTime.UtcNow.AddMilliseconds(DefaultTimeoutMs);
        var everRead = false;

        do
        {
            var element = MauiScope.TryFindElement(Locator);
            var actual = element == null ? null : ReadTime(element);
            if (actual != null)
            {
                everRead = true;
                if (actual.Value.Hours == time.Hours && actual.Value.Minutes == time.Minutes)
                    return true;
            }

            WaitHelper.Pause(PollingIntervalMs);
        }
        while (System.DateTime.UtcNow < deadline);

        return !everRead;
    }

    #endregion

    #region Hand-written Convenience Members

    // Tolerance-based comparison: a time picker's value moves while a test runs.

    /// <summary>
    /// Gets the currently selected time.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout for finding the element.</param>
    /// <returns>The selected time, or null if element not found.</returns>
    public TimeSpan? GetTime(int? timeoutMs = null)
        => RunGetWithElement(element => ReadTime(element), timeoutMs);

    /// <summary>
    /// Waits for the time to match the expected value.
    /// </summary>
    /// <param name="expected">Expected time. Null skips the wait.</param>
    /// <param name="toleranceSeconds">Tolerance in seconds for comparison.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>True if condition met, false if timeout.</returns>
    public bool WaitTime(TimeSpan? expected, int toleranceSeconds = 60, int? timeoutMs = null)
    {
        if (expected == null) return true;

        var tolerance = TimeSpan.FromSeconds(toleranceSeconds);

        return RunWaitWithElement(expected,
            e =>
            {
                var actual = ReadTime(e);
                if (!actual.HasValue) return false;
                var diff = (actual.Value - expected.Value).Duration();
                return diff <= tolerance;
            },
            timeoutMs);
    }

    /// <summary>
    /// Asserts the time matches the expected value.
    /// </summary>
    /// <param name="expected">Expected time. Null skips the assertion.</param>
    /// <param name="toleranceSeconds">Tolerance in seconds for comparison.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertTime(TimeSpan? expected, int toleranceSeconds = 60, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;

        var tolerance = TimeSpan.FromSeconds(toleranceSeconds);

        return RunAssertWithElement(expected,
            ReadTime, (actual, exp) =>
            {
                if (!actual.HasValue || !exp.HasValue) return false;
                var diff = (actual.Value - exp.Value).Duration();
                return diff <= tolerance;
            },
            message ?? $"Expected time {expected:hh\\:mm}. Locator: {Locator}", timeoutMs);
    }

    /// <summary>
    /// Gets the hours component of the selected time.
    /// </summary>
    /// <returns>The hours (0-23), or null if not available.</returns>
    public int? GetHours()
    {
        var time = GetTime();
        return time?.Hours;
    }

    /// <summary>
    /// Gets the minutes component of the selected time.
    /// </summary>
    /// <returns>The minutes (0-59), or null if not available.</returns>
    public int? GetMinutes()
    {
        var time = GetTime();
        return time?.Minutes;
    }

    #endregion
}
