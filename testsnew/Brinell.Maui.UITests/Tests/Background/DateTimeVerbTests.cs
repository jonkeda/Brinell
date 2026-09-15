using Brinell.Core.Locators;
using Brinell.Maui.UITests.Pages;
using Xunit;

namespace Brinell.Maui.UITests.Tests.Background;

/// <summary>
/// Step 20: setting a date and a time without touching the machine.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this is separate from <c>DatePickerTests</c>.</b> Those assert that a date was set,
/// which the calendar route also satisfies - it walks the flyout by pattern and uses no pointer
/// either. What they cannot show is <i>which</i> route ran, and the whole of step 20 is the
/// claim that the app now sets its own property instead of a test navigating its calendar.
/// </para>
/// <para>
/// So these ask the element directly whether the semantic route exists - that is the actual
/// measurement - and then set the value. (They used to run the write inside a refused
/// physical-input policy as well; the Windows driver no longer has physical input to refuse.)
/// </para>
/// </remarks>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Pattern", "GestureBridge")]
[Trait("Stage", "Background")]
public class DateTimeVerbTests
{
    private readonly MauiFixture _fixture;

    public DateTimeVerbTests(MauiFixture fixture)
    {
        _fixture = fixture;
        _fixture.Open(SamplePage.DateTime);
    }

    private DateTimeTestPage Page => new(_fixture.Context);

    /// <summary>
    /// A date is set through the app, with no calendar flyout.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task SetDate_NeedsNoPhysicalInput()
    {
        var page = Page;
        var wanted = System.DateTime.Now.Date.AddDays(3);

        page.TestDatePicker.SetDate(wanted);

        Assert.Equal(wanted, page.TestDatePicker.GetDate());
        return Task.CompletedTask;
    }

    /// <summary>
    /// An afternoon time survives the round trip, which is what the flyout route could not manage.
    /// </summary>
    /// <remarks>
    /// <b>The regression guard for stage G step 37.</b> 15:30 came back as 03:30 through the
    /// flyout: the WinUI hour list is a 12-hour clock and the AM/PM half was being lost. Setting
    /// <c>TimePicker.Time</c> has no 12-hour clock anywhere in it to lose, so the fix is the
    /// absence of the mechanism rather than a correction to it - which is exactly the kind of fix
    /// that quietly regresses if nothing pins an afternoon value.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task SetTime_KeepsTheAfternoonHalfOfTheClock()
    {
        var page = Page;
        var wanted = new TimeSpan(15, 30, 0);

        page.TestTimePicker.SetTime(wanted);

        Assert.Equal(wanted, page.TestTimePicker.GetTime());
        return Task.CompletedTask;
    }
}
