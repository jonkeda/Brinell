using Brinell.WinForms.Infrastructure;
using Brinell.Samples.WinForms.UITests.Pages;
using Brinell.Samples.WinForms.UITests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Brinell.Samples.WinForms.UITests.Tests;

/// <summary>
/// Tests for container and structural controls (TabControl, GroupBox, DataGridView).
/// Note: These tests use the control framework but may require actual controls
/// to be present in the sample app for full integration testing.
/// </summary>
public class ContainerControlTests : UITestBase
{
    [Fact]
    public void LoginPage_IsDisplayed_ChecksPageVisibility()
    {
        var page = new LoginPage(Context);
        page.IsDisplayed().Should().BeTrue();
    }

    [Fact]
    public void LoginPage_Name_ReturnsCorrectPageName()
    {
        var page = new LoginPage(Context);
        page.Name.Should().Be("LoginPage");
    }

    [Fact]
    public void Form_AllControls_AreAccessible()
    {
        var page = new LoginPage(Context);
        
        // Verify all controls are accessible - these should not throw
        page.EnterUsername("test");
        page.EnterPassword("test");
        page.SetPort(8080);
        page.SetRememberMe(true);
        page.SelectRole("Admin");
        page.SetVolume(50);
        page.SetStartDate(System.DateTime.Today);
    }

    [Fact]
    public void Form_InteractionSequence_ComplexScenario()
    {
        var page = new LoginPage(Context);
        
        // Perform a complex multi-control interaction sequence
        page.EnterUsername("admin");
        page.EnterPassword("password123");
        page.SetRememberMe(true);
        page.SelectRole("Admin");
        page.SetPort(9000);
        page.SetVolume(75);
        page.SetStartDate(new System.DateTime(2026, 01, 15));
        page.SetNotes("Complex interaction test");
        
        // Verify all values were set correctly
        page.GetUsername().Should().Be("admin");
        page.GetPort().Should().Be(9000);
        page.GetVolume().Should().Be(75);
        page.GetStartDate().Should().Be(new System.DateTime(2026, 01, 15).Date);
        page.GetNotes().Should().Contain("Complex interaction");
        page.IsRememberMeChecked().Should().BeTrue();
        page.GetSelectedRole().Should().Be("Admin");
    }

    [Fact]
    public void Form_Clear_ResetsAllControlValues()
    {
        var page = new LoginPage(Context);
        
        // Set all values
        page.EnterUsername("testuser");
        page.EnterPassword("pass");
        page.SetPort(7000);
        page.SetVolume(30);
        page.SetStartDate(new System.DateTime(2025, 12, 25));
        page.SetRememberMe(true);
        
        // Clear all
        page.ClickClear();
        System.Threading.Thread.Sleep(150);
        
        // Verify reset
        page.GetUsername().Should().BeEmpty();
        page.GetPort().Should().Be(8080); // Should reset to default
        page.GetVolume().Should().Be(50); // May not reset depending on implementation
        page.IsRememberMeChecked().Should().BeFalse();
    }

    [Fact]
    public void Form_ProgressBar_ReadsValue()
    {
        var page = new LoginPage(Context);
        var progress = page.GetProgress();
        progress.Should().BeGreaterThanOrEqualTo(0);
        progress.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public void Form_VolumeTrackBar_BoundaryValues()
    {
        var page = new LoginPage(Context);
        
        // Test minimum
        page.SetVolume(0);
        page.GetVolume().Should().Be(0);
        
        // Test maximum
        page.SetVolume(100);
        page.GetVolume().Should().Be(100);
        
        // Test mid-range
        page.SetVolume(50);
        page.GetVolume().Should().Be(50);
    }

    [Fact]
    public void Form_DatePicker_VariousDates()
    {
        var page = new LoginPage(Context);
        
        var testDates = new[]
        {
            new System.DateTime(2000, 01, 01),
            new System.DateTime(2025, 06, 15),
            System.DateTime.Today,
            new System.DateTime(2099, 12, 31)
        };

        foreach (var date in testDates)
        {
            page.SetStartDate(date);
            page.GetStartDate().Should().Be(date.Date);
        }
    }

    [Fact]
    public void Form_AllInputTypes_TogetherInSequence()
    {
        var page = new LoginPage(Context);
        
        // Text box
        page.EnterUsername("CompleteTest");
        
        // Password (masked)
        page.EnterPassword("SecurePass");
        
        // Numeric
        page.SetPort(5432);
        
        // CheckBox
        page.SetRememberMe(true);
        
        // ComboBox
        page.SelectRole("User");
        
        // RichTextBox
        page.SetNotes("Multi-line\nTest Notes");
        
        // DateTimePicker
        page.SetStartDate(new System.DateTime(2025, 08, 20));
        
        // TrackBar
        page.SetVolume(60);
        
        // Read back all values
        page.GetUsername().Should().Be("CompleteTest");
        page.GetPort().Should().Be(5432);
        page.IsRememberMeChecked().Should().BeTrue();
        page.GetSelectedRole().Should().Be("User");
        page.GetNotes().Should().Contain("Test Notes");
        page.GetStartDate().Should().Be(new System.DateTime(2025, 08, 20).Date);
        page.GetVolume().Should().Be(60);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("User")]
    [InlineData("Guest")]
    public void Form_RoleSelection_AllOptions(string role)
    {
        var page = new LoginPage(Context);
        page.SelectRole(role);
        page.GetSelectedRole().Should().Be(role);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(75)]
    [InlineData(100)]
    public void Form_VolumeSelection_AllLevels(int volume)
    {
        var page = new LoginPage(Context);
        page.SetVolume(volume);
        page.GetVolume().Should().Be(volume);
    }

    [Fact]
    public void Form_RememberMeToggle_TrueAndFalse()
    {
        var page = new LoginPage(Context);
        
        page.SetRememberMe(true);
        page.IsRememberMeChecked().Should().BeTrue();
        
        page.SetRememberMe(false);
        page.IsRememberMeChecked().Should().BeFalse();
        
        page.SetRememberMe(true);
        page.IsRememberMeChecked().Should().BeTrue();
    }

    [Fact]
    public void Form_PortNumberRange_ValidPorts()
    {
        var page = new LoginPage(Context);
        
        var commonPorts = new[] { 80, 443, 3306, 5432, 8080, 8888, 9000 };
        
        foreach (var port in commonPorts)
        {
            page.SetPort(port);
            page.GetPort().Should().Be(port);
        }
    }

    [Fact]
    public void Form_Notes_MultiLineText()
    {
        var page = new LoginPage(Context);
        
        var multiLineText = "Line 1\r\nLine 2\r\nLine 3";
        page.SetNotes(multiLineText);
        
        var result = page.GetNotes();
        result.Should().Contain("Line 1");
        result.Should().Contain("Line 2");
        result.Should().Contain("Line 3");
    }

    [Fact]
    public void Form_Login_WithValidCredentials()
    {
        var page = new LoginPage(Context);
        
        page.EnterUsername("admin");
        page.EnterPassword("admin123");
        page.SelectRole("Admin");
        page.ClickLogin();
        
        System.Threading.Thread.Sleep(100);
        
        var status = page.GetStatusMessage();
        status.Should().Contain("admin");
        status.Should().Contain("Admin");
    }

    [Fact]
    public void Form_MultipleLoginAttempts()
    {
        var page = new LoginPage(Context);
        
        for (int i = 0; i < 3; i++)
        {
            page.ClickClear();
            page.EnterUsername($"user{i}");
            page.SelectRole("User");
            page.ClickLogin();
            System.Threading.Thread.Sleep(100);
            
            var status = page.GetStatusMessage();
            status.Should().Contain($"user{i}");
        }
    }
}
