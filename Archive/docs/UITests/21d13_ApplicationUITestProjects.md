# 13. Application UITest Projects

**Parent:** [Documentation Index](21d0_UITestFramework_Index.md)  
**Code Examples:** [21d13_ApplicationUITestProjects_CodeExamples.md](21d13_ApplicationUITestProjects_CodeExamples.md)  
**Previous:** [Standardized Logging](21d12_StandardizedLogging.md)  
**Version:** 3.0 (Updated December 2025)

---

## 13.1 Overview

Application UITest projects contain the actual UI tests for specific applications. They reference the framework projects and implement page objects and tests specific to the application.

---

## 13.2 Project Structure

### 13.2.1 Naming Convention

```
{Application}.{Platform}.UITests
```

Examples:
- `Oravey.Tools.Wpf.UITests`
- `Oravey.Tools.Maui.UITests`
- `Oravey.Tools.Web.UITests`

### 13.2.2 Folder Structure

```
Oravey.Tools.Wpf.UITests/
├── Infrastructure/
│   ├── UITestBase.cs           # Base test class
│   ├── TestFixture.cs          # Shared fixture (app launch)
│   └── TestHelpers.cs          # Utility methods
├── PageObjects/
│   ├── ShellPage.cs            # Main shell page
│   ├── SettingsPage.cs         # Settings page
│   ├── Dialogs/
│   │   ├── ConfirmDialog.cs
│   │   └── ErrorDialog.cs
│   └── Regions/
│       ├── HeaderRegion.cs
│       └── SidebarRegion.cs
├── Tests/
│   ├── NavigationTests.cs      # Navigation tests
│   ├── SettingsTests.cs        # Settings page tests
│   └── Integration/
│       └── EndToEndTests.cs
├── TestData/
│   ├── TestUsers.json
│   └── TestSettings.json
└── Oravey.Tools.Wpf.UITests.csproj
```

---

## 13.3 Project References

### 13.3.1 Framework References

| Reference | Purpose |
|-----------|---------|
| `Oravey.UITestFramework.Core` | Interfaces and utilities |
| `Oravey.UITestFramework.Wpf` | WPF platform (context, base classes, controls) |
| `Oravey.UITestFramework.Mocking` | WireMock support (optional) |

**Note (v3):** Platform projects are self-contained. The WPF project includes all base classes (`PageBase`, `ControlBase`, etc.) - they are not in Core.

### 13.3.2 Test References

| Reference | Version | Purpose |
|-----------|---------|---------|
| `xunit` | 2.9.x | Test framework |
| `xunit.runner.visualstudio` | 2.8.x | VS Test integration |
| `FluentAssertions` | 7.0.x | Fluent assertions |
| `Microsoft.NET.Test.Sdk` | 17.11.x | Test SDK |

---

## 13.4 UITestBase Class

### 13.4.1 Responsibilities

- Create/dispose platform-specific test context
- Initialize logger
- Launch/close application
- Provide common test utilities

### 13.4.2 Key Properties

| Property | Type | Description |
|----------|------|-------------|
| `Context` | `FlaUITestContext` | Platform-specific test context |
| `Logger` | `ITestLogger` | CSV logger |
| `TestName` | `string` | Current test name |
| `App` | `Application` | FlaUI application |

**Note (v3):** Use the platform-specific context type (`FlaUITestContext`, `AppiumTestContext`, `SeleniumTestContext`) rather than the generic `ITestContext` interface.

### 13.4.3 Lifecycle

```
Constructor → SetupTest() → Test Method → Dispose()
```

---

## 13.5 Test Fixture

### 13.5.1 Purpose

Share expensive resources (like application launch) across tests in a collection.

### 13.5.2 Collection Definition

```csharp
[CollectionDefinition("UITests")]
public class UITestCollection : ICollectionFixture<UITestFixture>
{
}
```

### 13.5.3 Usage

```csharp
[Collection("UITests")]
public class SettingsTests : UITestBase
{
    // Tests share application instance
}
```

---

## 13.6 Test Data Management

### 13.6.1 Test Data Files

```json
// TestData/TestUsers.json
{
  "validUser": {
    "username": "testuser",
    "password": "Test123!",
    "displayName": "Test User"
  },
  "adminUser": {
    "username": "admin",
    "password": "Admin123!",
    "displayName": "Administrator"
  }
}
```

### 13.6.2 Loading Test Data

```csharp
public static class TestData
{
    public static TestUser ValidUser => 
        LoadTestData<TestUser>("TestUsers.json", "validUser");
    
    public static TestUser AdminUser => 
        LoadTestData<TestUser>("TestUsers.json", "adminUser");
}
```

---

## 13.7 Environment Configuration

### 13.7.1 Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `APP_PATH` | Application executable path | Auto-detect |
| `PLATFORM` | Target platform | `Windows` |
| `CLOUD_PROVIDER` | Cloud provider | `None` |
| `LOG_OUTPUT_PATH` | Log directory | `logs/` |

### 13.7.2 Configuration Priority

1. Environment variables
2. appsettings.json
3. Default values

---

## 13.8 Application Launch

### 13.8.1 Local Launch

```csharp
protected virtual void LaunchApplication()
{
    var appPath = GetApplicationPath();
    App = FlaUI.Core.Application.Launch(appPath);
    MainWindow = App.GetMainWindow(Automation);
}
```

### 13.8.2 Attach to Running

```csharp
protected virtual void AttachToApplication(int processId)
{
    App = FlaUI.Core.Application.Attach(processId);
    MainWindow = App.GetMainWindow(Automation);
}
```

---

## 13.9 Best Practices

### 13.9.1 Project Organization

- ✅ One UITest project per platform
- ✅ Keep page objects in separate folder
- ✅ Group tests by feature
- ✅ Use meaningful test method names
- ✅ Share fixtures for expensive setup

### 13.9.2 Test Independence

- ✅ Each test should be independent
- ✅ Reset application state in setup
- ✅ Don't rely on test execution order
- ✅ Clean up test data in teardown

---

*Next: [Test Categories](21d14_TestCategories.md)*
