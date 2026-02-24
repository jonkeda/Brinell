using Brinell.WinForms.UITests.Fixtures;
using Brinell.WinForms.UITests.Pages;

namespace Brinell.WinForms.UITests.Tests;

/// <summary>
/// Tests for container interactions and complex multi-control scenarios.
/// </summary>
[Collection("WinForms UITests")]
public class ContainerControlTests
{
    private readonly WinFormsSampleFixture _fixture;

    public ContainerControlTests(WinFormsSampleFixture fixture) => _fixture = fixture;

    private LoginPage GetPage()
    {
        var page = new LoginPage(_fixture.Context);
        page.ClickClear();
        page.WaitForFormCleared(timeoutMs: 2000);
        return page;
    }

    [Fact]
    public void LoginPage_IsDisplayed_ChecksPageVisibility()
    {
        var page = new LoginPage(_fixture.Context);
        Assert.True(page.IsLoaded());
    }

    [Fact]
    public void Form_AllControls_AreAccessible()
    {
        var page = GetPage();
        page.EnterUsername("test");
        page.EnterPassword("test");
        page.SetPort(8080);
        page.SetRememberMe(true);
        page.SelectRole("Admin");
        page.SetVolume(50);
        page.SetStartDate(DateTime.Today);
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
        page.SetStartDate(new DateTime(2026, 01, 15));
        page.SetNotes("Complex interaction test");

        Assert.Equal("admin", page.GetUsername());
        Assert.Equal(9000, page.GetPort());
        Assert.Equal(75, page.GetVolume());
        Assert.Contains("Complex interaction", page.GetNotes());
        Assert.True(page.IsRememberMeChecked());
        Assert.Equal("Admin", page.GetSelectedRole());
    }

    [Fact]
    public void Form_Clear_ResetsAllControlValues()
    {
        var page = GetPage();
        page.EnterUsername("testuser");
        page.EnterPassword("pass");
        page.SetPort(7000);
        page.SetVolume(30);
        page.SetRememberMe(true);

        page.ClickClear();
        page.WaitForFormCleared();

        Assert.Equal(string.Empty, page.GetUsername());
        Assert.Equal(8080, page.GetPort()); // Default
        Assert.False(page.IsRememberMeChecked());
    }

    [Fact]
    public void Form_ProgressBar_ReadsValue()
    {
        var page = new LoginPage(_fixture.Context);
        var progress = page.GetProgress();
        Assert.NotNull(progress);
        Assert.InRange(progress.Value, 0, 100);
    }

    [Fact]
    public void Form_VolumeTrackBar_BoundaryValues()
    {
        var page = GetPage();

        page.SetVolume(0);
        Assert.Equal(0, page.GetVolume());

        page.SetVolume(100);
        Assert.Equal(100, page.GetVolume());

        page.SetVolume(50);
        Assert.Equal(50, page.GetVolume());
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
        page.SetStartDate(new DateTime(2025, 08, 20));
        page.SetVolume(60);

        Assert.Equal("CompleteTest", page.GetUsername());
        Assert.Equal(5432, page.GetPort());
        Assert.True(page.IsRememberMeChecked());
        Assert.Equal("User", page.GetSelectedRole());
        Assert.Contains("Test Notes", page.GetNotes());
        Assert.Equal(60, page.GetVolume());
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("User")]
    [InlineData("Guest")]
    public void Form_RoleSelection_AllOptions(string role)
    {
        var page = GetPage();
        page.SelectRole(role);
        Assert.Equal(role, page.GetSelectedRole());
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
        Assert.Equal(volume, page.GetVolume());
    }

    [Fact]
    public void Form_Login_WithValidCredentials()
    {
        var page = GetPage();
        page.EnterUsername("admin");
        page.EnterPassword("admin123");
        page.SelectRole("Admin");
        page.ClickLogin();
        page.WaitForLoginComplete();

        var status = page.GetStatusMessage();
        Assert.Contains("admin", status);
        Assert.Contains("Admin", status);
    }

    [Fact]
    public void Form_MultipleLoginAttempts()
    {
        var page = GetPage();

        for (int i = 0; i < 3; i++)
        {
            page.ClickClear();
            page.WaitForFormCleared();
            page.EnterUsername($"user{i}");
            page.SelectRole("User");
            page.ClickLogin();
            page.WaitForLoginComplete();

            Assert.Contains($"user{i}", page.GetStatusMessage());
        }
    }
}
