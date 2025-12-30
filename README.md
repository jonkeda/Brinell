# Brinell

[![Build](https://github.com/YOUR_USERNAME/Brinell/actions/workflows/build.yml/badge.svg)](https://github.com/YOUR_USERNAME/Brinell/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/Brinell.Core.svg)](https://www.nuget.org/packages/Brinell.Core)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A cross-platform UI testing framework for .NET applications. Brinell provides a unified API for automating WPF, HTML/Web, and MAUI applications with built-in API mocking support.

## Features

- **Unified API**: Consistent interface across WPF, HTML, MAUI, and Stride platforms
- **Page Object Pattern**: Built-in support for maintainable test architecture
- **Rich Control Library**: Pre-built control wrappers for common UI elements
- **Visual Validation**: Screenshot capture and comparison capabilities
- **API Mocking**: WireMock integration for isolated UI testing
- **Stride 3D Engine Support**: UI testing for Stride game engine applications
- **Multi-targeting**: Supports .NET 8.0, .NET 9.0, and .NET 10.0

## Packages

| Package | Description |
|---------|-------------|
| [Brinell.Core](https://www.nuget.org/packages/Brinell.Core) | Core abstractions and interfaces |
| [Brinell.Wpf](https://www.nuget.org/packages/Brinell.Wpf) | WPF automation using FlaUI |
| [Brinell.Html](https://www.nuget.org/packages/Brinell.Html) | Web automation using Selenium |
| [Brinell.Html.Playwright](https://www.nuget.org/packages/Brinell.Html.Playwright) | Web automation using Playwright |
| [Brinell.Maui](https://www.nuget.org/packages/Brinell.Maui) | Mobile automation using Appium |
| [Brinell.Stride](https://www.nuget.org/packages/Brinell.Stride) | Stride 3D game engine UI testing |
| [Brinell.Stride.Automation](https://www.nuget.org/packages/Brinell.Stride.Automation) | In-game automation hooks for Stride |
| [Brinell.Mocking](https://www.nuget.org/packages/Brinell.Mocking) | API mocking using WireMock |

## Installation

```bash
# For WPF applications
dotnet add package Brinell.Wpf

# For web applications (Selenium)
dotnet add package Brinell.Html

# For web applications (Playwright)
dotnet add package Brinell.Html.Playwright

# For MAUI/mobile applications
dotnet add package Brinell.Maui

# For Stride 3D game engine
dotnet add package Brinell.Stride           # Test project
dotnet add package Brinell.Stride.Automation  # Game project

# For API mocking
dotnet add package Brinell.Mocking
```

## Quick Start

### WPF Application Testing

```csharp
using Brinell.Wpf.Testing;
using Brinell.Wpf.Controls;
using Brinell.Wpf.Infrastructure;

public class LoginPageTests : WpfUITestBase
{
    [UITest]
    public void Login_WithValidCredentials_NavigatesToDashboard()
    {
        // Arrange
        var loginPage = new LoginPage(Context);
        
        // Act
        loginPage.EnterUsername("testuser");
        loginPage.EnterPassword("password123");
        loginPage.ClickLogin();
        
        // Assert
        var dashboardPage = new DashboardPage(Context);
        Assert.True(dashboardPage.IsDisplayed);
    }
}
```

### Page Object Pattern

```csharp
using Brinell.Wpf.Controls.Base;
using Brinell.Wpf.Controls;

public class LoginPage : PageBase
{
    public TextBoxControl UsernameTextBox => FindControl<TextBoxControl>("UsernameTextBox");
    public TextBoxControl PasswordTextBox => FindControl<TextBoxControl>("PasswordTextBox");
    public ButtonControl LoginButton => FindControl<ButtonControl>("LoginButton");
    
    public LoginPage(FlaUITestContext context) : base(context) { }
    
    public void EnterUsername(string username) => UsernameTextBox.SetText(username);
    public void EnterPassword(string password) => PasswordTextBox.SetText(password);
    public void ClickLogin() => LoginButton.Click();
}
```

### Web Application Testing

```csharp
using Brinell.Html.Testing;
using Brinell.Html.Controls;

public class WebLoginTests : HtmlUITestBase
{
    [UITest]
    public void Login_WithValidCredentials_ShowsWelcome()
    {
        // Navigate to login page
        Context.NavigateTo("https://example.com/login");
        
        // Find and interact with controls
        var usernameInput = FindControl<TextInputControl>("username");
        var passwordInput = FindControl<TextInputControl>("password");
        var loginButton = FindControl<ButtonControl>("login-btn");
        
        usernameInput.SetText("user@example.com");
        passwordInput.SetText("password123");
        loginButton.Click();
        
        // Assert
        var welcomeLabel = FindControl<LabelControl>("welcome-message");
        Assert.Contains("Welcome", welcomeLabel.Text);
    }
}
```

### API Mocking

```csharp
using Brinell.Mocking;

// Create a mock API server
var mockServer = new MockApiServer();
mockServer.Start(9000);

// Setup endpoint stubs
var stubBuilder = new ApiStubBuilder(mockServer);
stubBuilder
    .ForPath("/api/users")
    .WithMethod("GET")
    .ReturnsJson(new[] { new { Id = 1, Name = "Test User" } });

// Run your UI tests against the mocked backend
// ...

mockServer.Stop();
```

## Architecture

Brinell follows a layered architecture:

```
┌───────────────────────────────────────────────────────────────────────────┐
│                           Your UI Tests                                   │
├───────────────────────────────────────────────────────────────────────────┤
│  Brinell.Wpf  │  Brinell.Html  │  Brinell.Maui  │  Brinell.Stride        │
├───────────────────────────────────────────────────────────────────────────┤
│                           Brinell.Core                                    │
├───────────────────────────────────────────────────────────────────────────┤
│    FlaUI      │   Selenium/    │    Appium      │  Named Pipe + Win32    │
│               │   Playwright   │                │  + Stride.Automation   │
└───────────────────────────────────────────────────────────────────────────┘
```

## Test Attributes

```csharp
[UITest]                    // Marks a method as a UI test
[SmokeTest]                 // Marks a test as part of smoke test suite
[Platform(Platform.Wpf)]    // Specifies target platform
[Priority(1)]               // Sets test priority (1 = highest)
```

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for a list of changes.
