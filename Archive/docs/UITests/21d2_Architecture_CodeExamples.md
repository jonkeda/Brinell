# 2. Architecture - Code Examples

**Parent:** [Architecture](21d2_Architecture.md)

---

## 2.1 Solution File Structure

```xml
<!-- Oravey.UITestFramework.slnx -->
<Solution>
  <Folder Name="Core">
    <Project Path="Oravey.UITestFramework.Core\Oravey.UITestFramework.Core.csproj" />
  </Folder>
  <Folder Name="Platforms">
    <Project Path="Oravey.UITestFramework.Wpf\Oravey.UITestFramework.Wpf.csproj" />
    <Project Path="Oravey.UITestFramework.Maui\Oravey.UITestFramework.Maui.csproj" />
    <Project Path="Oravey.UITestFramework.Html\Oravey.UITestFramework.Html.csproj" />
  </Folder>
  <Folder Name="Mocking">
    <Project Path="Oravey.UITestFramework.Mocking\Oravey.UITestFramework.Mocking.csproj" />
  </Folder>
</Solution>
```

---

## 2.2 Core Project File

```xml
<!-- Oravey.UITestFramework.Core.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>Oravey.UITestFramework.Core</RootNamespace>
    <Description>Core abstractions and base classes for Oravey UI Testing Framework</Description>
  </PropertyGroup>

  <!-- No external dependencies - pure abstractions -->

</Project>
```

---

## 2.3 WPF Platform Project File

```xml
<!-- Oravey.UITestFramework.Wpf.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>Oravey.UITestFramework.Wpf</RootNamespace>
    <UseWPF>true</UseWPF>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FlaUI.Core" />
    <PackageReference Include="FlaUI.UIA3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Oravey.UITestFramework.Core\Oravey.UITestFramework.Core.csproj" />
  </ItemGroup>

</Project>
```

---

## 2.4 MAUI Platform Project File

```xml
<!-- Oravey.UITestFramework.Maui.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>Oravey.UITestFramework.Maui</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Appium.WebDriver" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Oravey.UITestFramework.Core\Oravey.UITestFramework.Core.csproj" />
  </ItemGroup>

</Project>
```

---

## 2.5 HTML/Selenium Platform Project File

```xml
<!-- Oravey.UITestFramework.Html.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>Oravey.UITestFramework.Html</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Selenium.WebDriver" />
    <PackageReference Include="Selenium.WebDriver.ChromeDriver" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Oravey.UITestFramework.Core\Oravey.UITestFramework.Core.csproj" />
  </ItemGroup>

</Project>
```

---

## 2.6 Mocking Project File

```xml
<!-- Oravey.UITestFramework.Mocking.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>Oravey.UITestFramework.Mocking</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="WireMock.Net" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Oravey.UITestFramework.Core\Oravey.UITestFramework.Core.csproj" />
  </ItemGroup>

</Project>
```

---

## 2.7 Layer Communication Example

```csharp
// Example: How layers communicate during a test

// APPLICATION LAYER - Test Class
public class NavigationTests : UITestBase
{
    [Fact]
    public void Navigate_To_Settings()
    {
        // Uses PageObject (Application Layer)
        var shell = new ShellPage(Context);
        
        // PageObject uses ControlObject (Core Layer)
        // ControlObject uses Driver (Platform Layer)
        var settings = shell.NavigateToSettings();
        
        // Assert uses logging (Core Layer)
        settings.PageTitle.AssertText("Settings");
    }
}

// APPLICATION LAYER - PageObject
public class ShellPage : PageObjectBase
{
    public ButtonControl SettingsButton { get; }
    
    public ShellPage(ITestContext context) : base(context, "Shell")
    {
        // Creates platform-specific control (depends on context type)
        SettingsButton = new ButtonControl(context, this, "SettingsButton");
    }
    
    public SettingsPage NavigateToSettings()
    {
        // Logs action via CSV logger
        Log("Navigating to Settings");
        
        // Click calls CheckClickable first (Core pattern)
        SettingsButton.Click();
        
        // Returns new page and waits for ready
        var settings = new SettingsPage(Context);
        settings.WaitForPageReady();
        return settings;
    }
}

// CORE LAYER - ControlObjectBase (simplified)
public abstract class ControlObjectBase
{
    protected readonly ITestContext Context;
    protected readonly ITestLogger Logger;
    
    public virtual void Click()
    {
        // ALWAYS check before action
        CheckClickable();
        
        var element = GetElement();
        Context.Driver.Click(element);
        
        // Structured CSV logging
        Logger.LogAction(Context.TestName, Page?.PageName, AutomationId, 
            "Click", null, null, "Success", null);
    }
    
    public virtual void CheckClickable()
    {
        if (!WaitClickable())
        {
            throw new AssertionException(
                $"Control '{AutomationId}' is not clickable");
        }
    }
}

// PLATFORM LAYER - FlaUIDriverAdapter
public class FlaUIDriverAdapter : IDriverAdapter
{
    private readonly AutomationBase _automation;
    private readonly Window _mainWindow;
    
    public void Click(IElementAdapter element)
    {
        var flaElement = (FlaUIElementAdapter)element;
        flaElement.Element.Click();
    }
}
```

---

## 2.8 Dependency Injection Pattern

```csharp
// Context factory for different platforms
public static class TestContextFactory
{
    public static ITestContext Create(Platform platform, TestConfiguration config)
    {
        return platform switch
        {
            Platform.Windows => CreateWpfContext(config),
            Platform.WindowsMaui => CreateMauiWindowsContext(config),
            Platform.Android => CreateAndroidContext(config),
            Platform.iOS => CreateiOSContext(config),
            Platform.Web => CreateWebContext(config),
            _ => throw new ArgumentOutOfRangeException(nameof(platform))
        };
    }
    
    private static ITestContext CreateWpfContext(TestConfiguration config)
    {
        var driver = new FlaUIDriverAdapter(config.ApplicationPath);
        return new FlaUITestContext(driver, config);
    }
    
    private static ITestContext CreateWebContext(TestConfiguration config)
    {
        var driver = new SeleniumDriverAdapter(config.BrowserType, config.BaseUrl);
        return new SeleniumTestContext(driver, config);
    }
    
    // ... other platform contexts
}
```

---

*Related: [Core Framework Code Examples](21d3_CoreFramework_CodeExamples.md)*
