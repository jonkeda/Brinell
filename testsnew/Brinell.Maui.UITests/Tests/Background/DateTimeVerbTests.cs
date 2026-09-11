using Brinell.Core.Diagnostics;
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
/// So these ask the element directly whether the semantic route exists, and then run the write
/// inside <see cref="PhysicalInputPolicy.Refused"/>. The second is belt and braces given the
/// calendar route is pointerless too; the first is the actual measurement.
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
    /// The control group: both pickers declare the verb that replaces their flyout.
    /// </summary>
    /// <remarks>
    /// If this fails, everything below still passes - through the calendar and clock flyouts,
    /// exactly as before - and step 20 would have changed nothing while appearing to work.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task BothPickers_OfferTheSemanticRoute()
    {
        var datePicker = _fixture.Context.TryFindElement(Locator.ByAutomationId("TestDatePicker"));
        var timePicker = _fixture.Context.TryFindElement(Locator.ByAutomationId("TestTimePicker"));

        Assert.NotNull(datePicker);
        Assert.NotNull(timePicker);

        Assert.True(
            datePicker!.SupportsSetDate,
            "TestDatePicker does not declare SetDate, so DatePicker.SetDateCore will walk the "
            + "calendar flyout instead - which works, and is not what step 20 built.");

        Assert.True(
            timePicker!.SupportsSetTime,
            "TestTimePicker does not declare SetTime.");

        return Task.CompletedTask;
    }

    /// <summary>
    /// A date is set with every real input refused.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task SetDate_NeedsNoPhysicalInput()
    {
        var page = Page;
        var wanted = System.DateTime.Now.Date.AddDays(3);

        using (PhysicalInput.OverridePolicy(PhysicalInputPolicy.Refused))
        {
            page.TestDatePicker.SetDate(wanted);
        }

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

        using (PhysicalInput.OverridePolicy(PhysicalInputPolicy.Refused))
        {
            page.TestTimePicker.SetTime(wanted);
        }

        Assert.Equal(wanted, page.TestTimePicker.GetTime());
        return Task.CompletedTask;
    }
}
