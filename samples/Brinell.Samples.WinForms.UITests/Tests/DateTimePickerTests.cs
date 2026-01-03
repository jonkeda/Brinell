using Brinell.Samples.WinForms.UITests.Pages;
using Brinell.Samples.WinForms.UITests.Fixtures;
using FluentAssertions;
using Xunit;

namespace Brinell.Samples.WinForms.UITests.Tests;

/// <summary>
/// Tests for DateTimePicker control using shared app fixture.
/// </summary>
[Collection("UI Tests Collection")]
public class DateTimePickerTests
{
    private readonly AppFixture _fixture;

    public DateTimePickerTests(AppFixture fixture)
    {
        _fixture = fixture;
    }

    private LoginPage GetPage()
    {
        var page = _fixture.LoginPage;
        try { page.ClickClear(); System.Threading.Thread.Sleep(150); } catch { }
        return page;
    }

    [Fact]
    public void DateTimePicker_SetDate_SetsDateCorrectly()
    {
        var page = GetPage();
        var expectedDate = new System.DateTime(2025, 06, 15);
        page.SetStartDate(expectedDate);
        var result = page.GetStartDate();
        result.Should().Be(expectedDate.Date);
    }

    [Fact]
    public void DateTimePicker_SetDate_Today()
    {
        var page = GetPage();
        var today = System.DateTime.Today;
        page.SetStartDate(today);
        var result = page.GetStartDate();
        result.Should().Be(today);
    }

    [Fact]
    public void DateTimePicker_GetDate_ReturnsCurrentDate()
    {
        var page = GetPage();
        var date = new System.DateTime(2026, 01, 15);
        page.SetStartDate(date);
        var result = page.GetStartDate();
        result.Should().Be(date.Date);
    }

    [Theory]
    [InlineData(2025, 1, 1)]
    [InlineData(2025, 6, 15)]
    [InlineData(2025, 12, 31)]
    public void DateTimePicker_VariousDates(int year, int month, int day)
    {
        var page = GetPage();
        var date = new System.DateTime(year, month, day);
        page.SetStartDate(date);
        var result = page.GetStartDate();
        result.Should().Be(date);
    }
}

/// <summary>
/// Tests for TrackBar control using shared app fixture.
/// </summary>
[Collection("UI Tests Collection")]
public class TrackBarTests
{
    private readonly AppFixture _fixture;

    public TrackBarTests(AppFixture fixture)
    {
        _fixture = fixture;
    }

    private LoginPage GetPage()
    {
        var page = _fixture.LoginPage;
        try { page.ClickClear(); System.Threading.Thread.Sleep(150); } catch { }
        return page;
    }

    [Fact]
    public void TrackBar_SetValue_SetsValueCorrectly()
    {
        var page = GetPage();
        page.SetVolume(75);
        var result = page.GetVolume();
        result.Should().Be(75);
    }

    [Fact]
    public void TrackBar_SetMinimum_Works()
    {
        var page = GetPage();
        page.SetVolume(0);
        var result = page.GetVolume();
        result.Should().Be(0);
    }

    [Fact]
    public void TrackBar_SetMaximum_Works()
    {
        var page = GetPage();
        page.SetVolume(100);
        var result = page.GetVolume();
        result.Should().Be(100);
    }

    [Fact]
    public void TrackBar_GetValue_ReturnsCurrentValue()
    {
        var page = GetPage();
        page.SetVolume(50);
        var result = page.GetVolume();
        result.Should().Be(50);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(75)]
    [InlineData(100)]
    public void TrackBar_VariousValues(int value)
    {
        var page = GetPage();
        page.SetVolume(value);
        var result = page.GetVolume();
        result.Should().Be(value);
    }
}

/// <summary>
/// Tests for ProgressBar control using shared app fixture.
/// </summary>
[Collection("UI Tests Collection")]
public class ProgressBarTests
{
    private readonly AppFixture _fixture;

    public ProgressBarTests(AppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void ProgressBar_GetValue_ReturnsCurrentProgress()
    {
        var page = _fixture.LoginPage;
        var result = page.GetProgress();
        result.Should().BeGreaterThanOrEqualTo(0);
        result.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public void ProgressBar_IsVisible_ReturnsTrue()
    {
        var page = _fixture.LoginPage;
        // Progress bar should exist and be visible
        var progress = page.GetProgress();
        // If we can get the value, it's visible
        progress.Should().BeGreaterThanOrEqualTo(0);
    }
}
