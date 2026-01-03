using Brinell.Samples.WinForms.UITests.Pages;
using Brinell.Samples.WinForms.UITests.Fixtures;
using FluentAssertions;
using Xunit;

namespace Brinell.Samples.WinForms.UITests.Tests;

/// <summary>
/// Tests for container and structural controls using shared app fixture.
/// </summary>
[Collection("UI Tests Collection")]
public class ContainerControlTests
{
    private readonly AppFixture _fixture;

    public ContainerControlTests(AppFixture fixture)
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
    public void LoginPage_IsDisplayed_ChecksPageVisibility()
    {
        var page = _fixture.LoginPage;
        page.IsDisplayed().Should().BeTrue();
    }

    [Fact]
    public void LoginPage_Name_ReturnsCorrectPageName()
    {
        var page = _fixture.LoginPage;
        page.Name.Should().Be("LoginPage");
    }

    [Fact]
    public void Form_AllControls_AreAccessible()
    {
        var page = GetPage();
        
        // Verify all controls are accessible
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
        var page = GetPage();
        
        page.EnterUsername("admin");
        page.EnterPassword("password123");
        page.SetRememberMe(true);
        page.SelectRole("Admin");
        page.SetPort(9000);
        page.SetVolume(75);
        page.SetStartDate(new System.DateTime(2026, 01, 15));
        page.SetNotes("Complex interaction test");
        
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
        var page = GetPage();
        
        page.EnterUsername("testuser");
        page.EnterPassword("pass");
        page.SetPort(7000);
        page.SetVolume(30);
        page.SetStartDate(new System.DateTime(2025, 12, 25));
        page.SetRememberMe(true);
        
        page.ClickClear();
        System.Threading.Thread.Sleep(200);
        
        page.GetUsername().Should().BeEmpty();
        page.GetPort().Should().Be(8080);
        page.IsRememberMeChecked().Should().BeFalse();
    }

    [Fact]
    public void Form_ProgressBar_ReadsValue()
    {
        var page = _fixture.LoginPage;
        var progress = page.GetProgress();
        progress.Should().BeGreaterThanOrEqualTo(0);
        progress.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public void Form_VolumeTrackBar_BoundaryValues()
    {
        var page = GetPage();
        
        page.SetVolume(0);
        page.GetVolume().Should().Be(0);
        
        page.SetVolume(100);
        page.GetVolume().Should().Be(100);
        
        page.SetVolume(50);
        page.GetVolume().Should().Be(50);
    }

    [Fact]
    public void Form_DatePicker_VariousDates()
    {
        var page = GetPage();
        
        var testDates = new[]
        {
            new System.DateTime(2025, 06, 15),
            System.DateTime.Today,
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
        var page = GetPage();
        
        page.EnterUsername("CompleteTest");
        page.EnterPassword("SecurePass");
        page.SetPort(5432);
        page.SetRememberMe(true);
        page.SelectRole("User");
        page.SetNotes("Test Notes");
        page.SetStartDate(new System.DateTime(2025, 08, 20));
        page.SetVolume(60);
        
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
        var page = GetPage();
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
        var page = GetPage();
        page.SetVolume(volume);
        page.GetVolume().Should().Be(volume);
    }

    [Fact]
    public void Form_Login_WithValidCredentials()
    {
        var page = GetPage();
        
        page.EnterUsername("admin");
        page.EnterPassword("admin123");
        page.SelectRole("Admin");
        page.ClickLogin();
        System.Threading.Thread.Sleep(150);
        
        var status = page.GetStatusMessage();
        status.Should().Contain("admin");
        status.Should().Contain("Admin");
    }

    [Fact]
    public void Form_MultipleLoginAttempts()
    {
        var page = GetPage();
        
        for (int i = 0; i < 3; i++)
        {
            page.ClickClear();
            System.Threading.Thread.Sleep(100);
            page.EnterUsername($"user{i}");
            page.SelectRole("User");
            page.ClickLogin();
            System.Threading.Thread.Sleep(150);
            
            var status = page.GetStatusMessage();
            status.Should().Contain($"user{i}");
        }
    }
}
