using Brinell.WinForms.Infrastructure;
using Brinell.Samples.WinForms.UITests.Pages;
using Brinell.Samples.WinForms.UITests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Brinell.Samples.WinForms.UITests.Tests;

public class DateTimePickerTests : UITestBase
{
    [Fact]
    public void DateTimePicker_SetDate_SetsCorrectly()
    {
        var page = new LoginPage(Context);
        var testDate = new DateTime(2025, 12, 25);
        page.SetStartDate(testDate);
        var result = page.GetStartDate();
        result.Should().Be(testDate.Date);
    }

    [Fact]
    public void DateTimePicker_SetDate_HandlesCurrentDate()
    {
        var page = new LoginPage(Context);
        var today = DateTime.Today;
        page.SetStartDate(today);
        var result = page.GetStartDate();
        result.Should().Be(today.Date);
    }

    [Fact]
    public void DateTimePicker_GetDate_ReturnsSetDate()
    {
        var page = new LoginPage(Context);
        var testDate = new DateTime(2024, 06, 15);
        page.SetStartDate(testDate);
        var result = page.GetStartDate();
        result.Should().Be(testDate.Date);
    }

    [Fact]
    public void DateTimePicker_SetDate_HandlesPastDate()
    {
        var page = new LoginPage(Context);
        var pastDate = new DateTime(2000, 01, 01);
        page.SetStartDate(pastDate);
        var result = page.GetStartDate();
        result.Should().Be(pastDate.Date);
    }

    [Fact]
    public void DateTimePicker_SetDate_HandlesFutureDate()
    {
        var page = new LoginPage(Context);
        var futureDate = new DateTime(2099, 12, 31);
        page.SetStartDate(futureDate);
        var result = page.GetStartDate();
        result.Should().Be(futureDate.Date);
    }

    [Theory]
    [InlineData(2024)]
    [InlineData(2025)]
    [InlineData(2026)]
    public void DateTimePicker_MultipleYears(int year)
    {
        var page = new LoginPage(Context);
        var testDate = new DateTime(year, 06, 15);
        page.SetStartDate(testDate);
        var result = page.GetStartDate();
        result.Year.Should().Be(year);
    }
}

public class TrackBarTests : UITestBase
{
    [Fact]
    public void TrackBar_SetValue_SetsCorrectly()
    {
        var page = new LoginPage(Context);
        page.SetVolume(75);
        var result = page.GetVolume();
        result.Should().Be(75);
    }

    [Fact]
    public void TrackBar_SetValue_HandlesMinimum()
    {
        var page = new LoginPage(Context);
        page.SetVolume(0);
        var result = page.GetVolume();
        result.Should().Be(0);
    }

    [Fact]
    public void TrackBar_SetValue_HandlesMaximum()
    {
        var page = new LoginPage(Context);
        page.SetVolume(100);
        var result = page.GetVolume();
        result.Should().Be(100);
    }

    [Fact]
    public void TrackBar_SetValue_HandlesMidpoint()
    {
        var page = new LoginPage(Context);
        page.SetVolume(50);
        var result = page.GetVolume();
        result.Should().Be(50);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(75)]
    [InlineData(100)]
    public void TrackBar_MultipleValues(int value)
    {
        var page = new LoginPage(Context);
        page.SetVolume(value);
        var result = page.GetVolume();
        result.Should().Be(value);
    }

    [Fact]
    public void TrackBar_SequentialChanges()
    {
        var page = new LoginPage(Context);
        page.SetVolume(25);
        page.SetVolume(50);
        page.SetVolume(75);
        var result = page.GetVolume();
        result.Should().Be(75);
    }
}

public class ProgressBarTests : UITestBase
{
    [Fact]
    public void ProgressBar_GetValue_ReturnsCurrentProgress()
    {
        var page = new LoginPage(Context);
        var progress = page.GetProgress();
        progress.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void ProgressBar_GetValue_WithinRange()
    {
        var page = new LoginPage(Context);
        var progress = page.GetProgress();
        progress.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public void ProgressBar_MultipleReads_Consistent()
    {
        var page = new LoginPage(Context);
        var progress1 = page.GetProgress();
        System.Threading.Thread.Sleep(100);
        var progress2 = page.GetProgress();
        // Progress should be same if no async operation is running
        progress1.Should().Be(progress2);
    }

    [Fact]
    public void Form_CombinedInteraction_AllControls()
    {
        var page = new LoginPage(Context);
        
        // Set all new controls
        page.SetStartDate(new DateTime(2025, 03, 15));
        page.SetVolume(65);
        
        // Get values
        var startDate = page.GetStartDate();
        var volume = page.GetVolume();
        var progress = page.GetProgress();
        
        startDate.Should().Be(new DateTime(2025, 03, 15).Date);
        volume.Should().Be(65);
        progress.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void Form_Volume_And_Date_Independent()
    {
        var page = new LoginPage(Context);
        
        // Change volume
        page.SetVolume(40);
        var volume1 = page.GetVolume();
        
        // Change date
        page.SetStartDate(DateTime.Today.AddDays(-7));
        
        // Volume should remain unchanged
        var volume2 = page.GetVolume();
        volume1.Should().Be(volume2);
    }

    [Fact]
    public void Form_Date_And_Volume_Together()
    {
        var page = new LoginPage(Context);
        var testDate = new DateTime(2025, 12, 25);
        const int testVolume = 80;
        
        page.SetStartDate(testDate);
        page.SetVolume(testVolume);
        
        var resultDate = page.GetStartDate();
        var resultVolume = page.GetVolume();
        
        resultDate.Should().Be(testDate.Date);
        resultVolume.Should().Be(testVolume);
    }

    [Fact]
    public void Form_Multiple_Control_Interactions()
    {
        var page = new LoginPage(Context);
        
        // Set username and port as before
        page.EnterUsername("volumetest");
        page.SetPort(8888);
        
        // Set new controls
        page.SetVolume(33);
        page.SetStartDate(new DateTime(2024, 09, 10));
        
        // Verify all
        page.GetUsername().Should().Be("volumetest");
        page.GetPort().Should().Be(8888);
        page.GetVolume().Should().Be(33);
        page.GetStartDate().Should().Be(new DateTime(2024, 09, 10).Date);
    }
}
