namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidTimePicker<TScope> : AndroidEditText<TScope>, ITimeControlObject<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    private static readonly string[] TimeFormats =
    [
        @"hh\:mm",
        @"h\:mm",
        @"hh\:mm\:ss",
        @"h\:mm\:ss"
    ];

    public AndroidTimePicker(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidTimePicker(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public TimeSpan? GetTime()
    {
        var text = GetText();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (TimeSpan.TryParseExact(text, TimeFormats, CultureInfo.InvariantCulture, out var exact))
        {
            return exact;
        }

        if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.NoCurrentDateDefault, out var parsed))
        {
            return parsed.TimeOfDay;
        }

        return TimeSpan.TryParse(text, CultureInfo.CurrentCulture, out var time)
            ? time
            : null;
    }

    public TScope SetTime(TimeSpan? time, int? timeoutMs = null)
    {
        if (time is null)
        {
            return ContainingScope;
        }

        return SetText(time.Value.ToString(@"hh\:mm", CultureInfo.InvariantCulture), timeoutMs);
    }

    public bool WaitTime(TimeSpan? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(() => GetTime() == expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    public TScope AssertTime(TimeSpan? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        if (!WaitTime(expected, timeoutMs))
        {
            Fail(message ?? $"Expected time '{expected}', actual '{GetTime()}'.", expected, GetTime());
        }

        return ContainingScope;
    }
}
