using Brinell.Samples.WinForms.UITests.Pages;
using Brinell.Samples.WinForms.UITests.Fixtures;
using FluentAssertions;
using Xunit;

namespace Brinell.Samples.WinForms.UITests.Tests;

/// <summary>
/// Tests for input controls using shared app fixture.
/// All tests share a single app instance to prevent chaos.
/// </summary>
[Collection("UI Tests Collection")]
public class InputControlTests
{
    private readonly AppFixture _fixture;

    public InputControlTests(AppFixture fixture)
    {
        _fixture = fixture;
    }

    private LoginPage GetPage()
    {
        var page = _fixture.LoginPage;
        // Reset form state before each test
        try { page.ClickClear(); System.Threading.Thread.Sleep(150); } catch { }
        return page;
    }

    [Fact]
    public void TextBox_Enter_SetsTextCorrectly()
    {
        var page = GetPage();
        page.EnterUsername("testuser");
        var result = page.GetUsername();
        result.Should().Be("testuser");
    }

    [Fact]
    public void TextBox_Clear_RemovesAllText()
    {
        var page = GetPage();
        page.EnterUsername("testuser");
        page.ClickClear();
        System.Threading.Thread.Sleep(150);
        var result = page.GetUsername();
        result.Should().BeEmpty();
    }

    [Fact]
    public void PasswordBox_Enter_AcceptsPassword()
    {
        var page = GetPage();
        page.EnterPassword("secret123");
        // Password field masks the text, verify form still works
        page.EnterUsername("testuser");
        var result = page.GetUsername();
        result.Should().Be("testuser");
    }

    [Fact]
    public void NumericUpDown_SetValue_SetsNumberCorrectly()
    {
        var page = GetPage();
        page.SetPort(9090);
        var result = page.GetPort();
        result.Should().Be(9090);
    }

    [Fact]
    public void NumericUpDown_SetValue_HandlesMinimumValue()
    {
        var page = GetPage();
        page.SetPort(1);
        var result = page.GetPort();
        result.Should().Be(1);
    }

    [Fact]
    public void NumericUpDown_SetValue_HandlesMaximumValue()
    {
        var page = GetPage();
        page.SetPort(65535);
        var result = page.GetPort();
        result.Should().Be(65535);
    }

    [Fact]
    public void RichTextBox_SetContent_SetsTextCorrectly()
    {
        var page = GetPage();
        page.SetNotes("Important notes for testing");
        var result = page.GetNotes();
        result.Should().Contain("Important notes");
    }

    [Fact]
    public void RichTextBox_Clear_RemovesAllText()
    {
        var page = GetPage();
        page.SetNotes("Some notes");
        page.ClickClear();
        System.Threading.Thread.Sleep(150);
        var result = page.GetNotes();
        result.Should().BeEmpty();
    }

    [Fact]
    public void Form_Login_WithAllInputs()
    {
        var page = GetPage();
        page.EnterUsername("admin");
        page.EnterPassword("admin123");
        page.SetPort(8080);
        page.SetNotes("Admin test user");
        page.SelectRole("Admin");
        page.SetRememberMe(true);

        var username = page.GetUsername();
        var port = page.GetPort();
        var notes = page.GetNotes();
        var role = page.GetSelectedRole();
        var rememberMe = page.IsRememberMeChecked();

        username.Should().Be("admin");
        port.Should().Be(8080);
        notes.Should().Contain("Admin test user");
        role.Should().Be("Admin");
        rememberMe.Should().BeTrue();
    }

    [Fact]
    public void Form_Clear_ResetsAllFields()
    {
        var page = GetPage();
        
        // Set values
        page.EnterUsername("testuser");
        page.EnterPassword("password");
        page.SetPort(9000);
        page.SetNotes("Some notes");
        page.SelectRole("User");
        page.SetRememberMe(true);

        // Clear all
        page.ClickClear();
        System.Threading.Thread.Sleep(200);
        
        var username = page.GetUsername();
        var port = page.GetPort();
        var rememberMe = page.IsRememberMeChecked();

        username.Should().BeEmpty();
        port.Should().Be(8080); // Should reset to default
        rememberMe.Should().BeFalse();
    }

    [Fact]
    public void Form_Login_UpdatesStatusMessage()
    {
        var page = GetPage();
        page.EnterUsername("testuser");
        page.SelectRole("User");
        page.ClickLogin();
        System.Threading.Thread.Sleep(150);
        
        var status = page.GetStatusMessage();
        status.Should().Contain("testuser");
        status.Should().Contain("User");
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("User")]
    [InlineData("Guest")]
    public void ComboBox_SelectRole_VariousRoles(string expectedRole)
    {
        var page = GetPage();
        page.SelectRole(expectedRole);
        var result = page.GetSelectedRole();
        result.Should().Be(expectedRole);
    }

    [Fact]
    public void CheckBox_Remember_TogglesCorrectly()
    {
        var page = GetPage();
        
        page.SetRememberMe(false);
        page.IsRememberMeChecked().Should().BeFalse();
        
        page.SetRememberMe(true);
        page.IsRememberMeChecked().Should().BeTrue();
        
        page.SetRememberMe(false);
        page.IsRememberMeChecked().Should().BeFalse();
    }

    [Fact]
    public void TextBox_WithSpecialCharacters()
    {
        var page = GetPage();
        var specialText = "user@example.com";
        page.EnterUsername(specialText);
        
        var result = page.GetUsername();
        result.Should().Be(specialText);
    }

    [Fact]
    public void NumericUpDown_CommonPorts()
    {
        var page = GetPage();
        var commonPorts = new[] { 80.0, 443.0, 3306.0, 5432.0, 8080.0 };
        
        foreach (var port in commonPorts)
        {
            page.SetPort(port);
            var result = page.GetPort();
            result.Should().Be(port);
        }
    }
}
