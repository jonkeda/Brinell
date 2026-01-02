# 1. Overview - Code Examples

**Parent:** [Overview](21d1_Overview.md)

---

## 1.1 Platform Enum Definition

```csharp
namespace Oravey.UITestFramework.Core.Abstractions;

/// <summary>
/// Defines the target platform for UI tests.
/// Replaces string-based platform identification and IsWindows/IsMobile properties.
/// </summary>
public enum Platform
{
    /// <summary>Windows desktop WPF application (FlaUI)</summary>
    Windows,
    
    /// <summary>Windows MAUI application (Appium)</summary>
    WindowsMaui,
    
    /// <summary>Android mobile application (Appium)</summary>
    Android,
    
    /// <summary>iOS mobile application (Appium)</summary>
    iOS,
    
    /// <summary>Web browser HTML application (Selenium)</summary>
    Web
}
```

---

## 1.2 Platform Extension Methods

```csharp
namespace Oravey.UITestFramework.Core.Abstractions;

/// <summary>
/// Extension methods for Platform enum replacing IsWindows/IsMobile properties.
/// </summary>
public static class PlatformExtensions
{
    /// <summary>Returns true for Android or iOS platforms.</summary>
    public static bool IsMobile(this Platform platform) =>
        platform is Platform.Android or Platform.iOS;
    
    /// <summary>Returns true for Windows or WindowsMaui platforms.</summary>
    public static bool IsDesktop(this Platform platform) =>
        platform is Platform.Windows or Platform.WindowsMaui;
    
    /// <summary>Returns true for Web platform.</summary>
    public static bool IsWeb(this Platform platform) =>
        platform == Platform.Web;
    
    /// <summary>Returns true for platforms that support touch gestures.</summary>
    public static bool SupportsGestures(this Platform platform) =>
        platform.IsMobile();
    
    /// <summary>Returns true for Windows platform (WPF via FlaUI).</summary>
    public static bool IsWindowsDesktop(this Platform platform) =>
        platform == Platform.Windows;
    
    /// <summary>Returns true for MAUI-based platforms (Appium).</summary>
    public static bool IsMaui(this Platform platform) =>
        platform is Platform.WindowsMaui or Platform.Android or Platform.iOS;
    
    /// <summary>Gets the automation library name for this platform.</summary>
    public static string GetAutomationLibrary(this Platform platform) =>
        platform switch
        {
            Platform.Windows => "FlaUI",
            Platform.WindowsMaui => "Appium",
            Platform.Android => "Appium",
            Platform.iOS => "Appium",
            Platform.Web => "Selenium",
            _ => throw new ArgumentOutOfRangeException(nameof(platform))
        };
}
```

---

## 1.3 Basic Test Structure

```csharp
using FluentAssertions;
using Oravey.Tools.Wpf.UITests.Infrastructure;
using Oravey.Tools.Wpf.UITests.PageObjects;
using Xunit;
using Xunit.Abstractions;

namespace Oravey.Tools.Wpf.UITests.Tests;

[Trait("Category", "UITest")]
[Collection("UITests")]  // Prevents parallel execution
public class NavigationSmokeTests : UITestBase
{
    public NavigationSmokeTests(ITestOutputHelper output) : base(output) { }
    
    [Fact]
    public void Application_Launches_And_Shows_Shell()
    {
        // Arrange
        var shell = new ShellPage(Context);
        
        // Act - Wait for page to be displayed and ready
        shell.WaitForDisplayed();
        shell.WaitForPageReady();  // IsBusy-based
        
        // Assert
        shell.HomeButton.AssertVisible(true);
        shell.SettingsButton.AssertVisible(true);
    }
    
    [Fact]
    public void Navigate_To_Settings_And_Back()
    {
        // Arrange
        var shell = new ShellPage(Context);
        shell.WaitForPageReady();
        
        // Act - Navigate to settings
        var settings = shell.NavigateToSettings();
        settings.WaitForPageReady();
        
        // Assert settings visible
        settings.PageTitle.AssertText("Settings");
        
        // Act - Navigate back
        var homeAgain = settings.NavigateToHome();
        homeAgain.WaitForPageReady();
        
        // Assert home visible
        homeAgain.IsDisplayed().Should().BeTrue();
    }
}
```

---

## 1.4 Project File Example

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <!-- Test Framework -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="coverlet.collector" />
  </ItemGroup>

  <ItemGroup>
    <!-- UI Test Framework -->
    <ProjectReference Include="..\UITestFramework\Oravey.UITestFramework.Core\Oravey.UITestFramework.Core.csproj" />
    <ProjectReference Include="..\UITestFramework\Oravey.UITestFramework.Wpf\Oravey.UITestFramework.Wpf.csproj" />
    <ProjectReference Include="..\UITestFramework\Oravey.UITestFramework.Mocking\Oravey.UITestFramework.Mocking.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- Application Under Test -->
    <ProjectReference Include="..\Tools\Oravey.Tools.Wpf\Oravey.Tools.Wpf.csproj" />
  </ItemGroup>

</Project>
```

---

## 1.5 Directory.Packages.props

```xml
<Project>
  <ItemGroup>
    <!-- UI Automation -->
    <PackageVersion Include="FlaUI.Core" Version="4.0.0" />
    <PackageVersion Include="FlaUI.UIA3" Version="4.0.0" />
    <PackageVersion Include="Appium.WebDriver" Version="8.0.0" />
    <PackageVersion Include="Selenium.WebDriver" Version="4.27.0" />
    <PackageVersion Include="Selenium.WebDriver.ChromeDriver" Version="131.0.6778.10800" />
    
    <!-- API Mocking -->
    <PackageVersion Include="WireMock.Net" Version="1.6.7" />
    
    <!-- Test Framework -->
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageVersion Include="xunit" Version="2.9.2" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageVersion Include="FluentAssertions" Version="6.12.2" />
    <PackageVersion Include="coverlet.collector" Version="6.0.2" />
  </ItemGroup>
</Project>
```

---

*Related: [Architecture Code Examples](21d2_Architecture_CodeExamples.md)*
