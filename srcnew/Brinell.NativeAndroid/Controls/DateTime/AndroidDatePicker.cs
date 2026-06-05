namespace Brinell.NativeAndroid.Controls;

public sealed class AndroidDatePicker<TScope> : AndroidEditText<TScope>, IDateControlObject<TScope>
    where TScope : INativeAndroidScope<TScope>
{
    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "MM/dd/yyyy",
        "M/d/yyyy",
        "dd/MM/yyyy",
        "d/M/yyyy",
        "dd-MM-yyyy",
        "d-M-yyyy"
    ];

    public AndroidDatePicker(Locator locator, INativeAndroidScope<TScope> scope)
        : base(locator, scope)
    {
    }

    public AndroidDatePicker(string locatorValue, INativeAndroidScope<TScope> scope)
        : base(locatorValue, scope)
    {
    }

    public DateTime? GetDate()
    {
        var text = GetText();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (DateTime.TryParseExact(
                text,
                DateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out var exact))
        {
            return exact.Date;
        }

        return DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var parsed)
            ? parsed.Date
            : null;
    }

    public TScope SetDate(DateTime? date, int? timeoutMs = null)
    {
        if (date is null)
        {
            return ContainingScope;
        }

        return SetText(date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), timeoutMs);
    }

    public bool WaitDate(DateTime? expected, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return true;
        }

        return Poll(() => GetDate()?.Date == expected.Value.Date, timeoutMs ?? DefaultTimeoutMs);
    }

    public TScope AssertDate(DateTime? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null)
        {
            return ContainingScope;
        }

        if (!WaitDate(expected, timeoutMs))
        {
            Fail(message ?? $"Expected date '{expected:yyyy-MM-dd}', actual '{GetDate():yyyy-MM-dd}'.", expected, GetDate());
        }

        return ContainingScope;
    }
}
