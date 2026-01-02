# 11. Cloud Provider Support

**Parent:** [Documentation Index](21d0_UITestFramework_Index.md)  
**Code Examples:** [21d11_CloudProviderSupport_CodeExamples.md](21d11_CloudProviderSupport_CodeExamples.md)  
**Previous:** [WireMock API Mocking](21d10_WireMockApiMocking.md)

---

## 11.1 Overview

The framework supports running UI tests on cloud testing platforms for scalable, cross-platform execution.

### 11.1.1 Supported Providers

| Provider | Type | Platforms |
|----------|------|-----------|
| **BrowserStack** | `CloudProvider.BrowserStack` | Web, Android, iOS |
| **SauceLabs** | `CloudProvider.SauceLabs` | Web, Android, iOS |
| **Local** | `CloudProvider.None` | All (default) |

---

## 11.2 Cloud Provider Enum

```csharp
public enum CloudProvider
{
    /// <summary>
    /// Run locally (default).
    /// </summary>
    None = 0,
    
    /// <summary>
    /// BrowserStack cloud testing.
    /// </summary>
    BrowserStack = 1,
    
    /// <summary>
    /// SauceLabs cloud testing.
    /// </summary>
    SauceLabs = 2
}
```

---

## 11.3 Configuration

### 11.3.1 CloudProviderConfig Properties

| Property | Type | Description |
|----------|------|-------------|
| `Provider` | `CloudProvider` | Cloud provider selection |
| `Username` | `string` | Account username/access key |
| `AccessKey` | `string` | API access key/secret |
| `HubUrl` | `string?` | Custom hub URL (optional) |
| `Project` | `string?` | Project name for reports |
| `Build` | `string?` | Build identifier |
| `SessionName` | `string?` | Test session name |

### 11.3.2 Platform-Specific Options

| Property | Type | Description |
|----------|------|-------------|
| `DeviceName` | `string?` | Physical device name |
| `PlatformVersion` | `string?` | OS version |
| `BrowserName` | `string?` | Browser for web tests |
| `BrowserVersion` | `string?` | Browser version |
| `AppUrl` | `string?` | Cloud-hosted app URL |
| `RealDevice` | `bool` | Use real device vs emulator |
| `NetworkLogs` | `bool` | Capture network logs |
| `Video` | `bool` | Record video |

---

## 11.4 Provider Hub URLs

### 11.4.1 Default URLs

| Provider | Hub URL |
|----------|---------|
| BrowserStack | `https://hub-cloud.browserstack.com/wd/hub` |
| SauceLabs | `https://ondemand.saucelabs.com/wd/hub` |

### 11.4.2 Regional URLs

**BrowserStack:**
- US: `https://hub-cloud.browserstack.com/wd/hub`
- EU: `https://hub.eu-central-1.browserstack.com/wd/hub`

**SauceLabs:**
- US West: `https://ondemand.us-west-1.saucelabs.com/wd/hub`
- US East: `https://ondemand.us-east-4.saucelabs.com/wd/hub`
- EU: `https://ondemand.eu-central-1.saucelabs.com/wd/hub`

---

## 11.5 Capability Building

### 11.5.1 BrowserStack Capabilities

```csharp
// Mobile (Android)
{
    "bstack:options": {
        "userName": "user",
        "accessKey": "key",
        "projectName": "Oravey",
        "buildName": "Build-123",
        "sessionName": "Login_Test",
        "deviceName": "Google Pixel 6",
        "osVersion": "12.0",
        "realMobile": "true",
        "networkLogs": "true"
    },
    "appium:app": "bs://app-id-here"
}

// Web
{
    "bstack:options": {
        "userName": "user",
        "accessKey": "key",
        "os": "Windows",
        "osVersion": "11",
        "browserName": "Chrome",
        "browserVersion": "latest"
    }
}
```

### 11.5.2 SauceLabs Capabilities

```csharp
// Mobile (iOS)
{
    "sauce:options": {
        "username": "user",
        "accessKey": "key",
        "name": "Login_Test",
        "build": "Build-123",
        "deviceName": "iPhone 14 Pro",
        "platformVersion": "16.0",
        "realDevice": true
    },
    "appium:app": "storage:app-id"
}

// Web
{
    "sauce:options": {
        "username": "user",
        "accessKey": "key",
        "browserName": "chrome",
        "browserVersion": "latest",
        "platformName": "Windows 11"
    }
}
```

---

## 11.6 Environment Configuration

### 11.6.1 Environment Variables

| Variable | Description |
|----------|-------------|
| `CLOUD_PROVIDER` | `None`, `BrowserStack`, `SauceLabs` |
| `CLOUD_USERNAME` | Provider username |
| `CLOUD_ACCESS_KEY` | Provider access key |
| `CLOUD_PROJECT` | Project name |
| `CLOUD_BUILD` | Build identifier |
| `CLOUD_DEVICE` | Target device name |
| `CLOUD_PLATFORM_VERSION` | OS/platform version |
| `CLOUD_BROWSER` | Browser name |
| `CLOUD_BROWSER_VERSION` | Browser version |
| `CLOUD_APP_URL` | Cloud-hosted app URL |

### 11.6.2 Loading from Environment

```csharp
var config = CloudProviderConfig.FromEnvironment();
// Reads all CLOUD_* variables
```

---

## 11.7 Test Context Factory Integration

### 11.7.1 Factory Method

```csharp
public static ITestContext CreateFromCloud(
    CloudProviderConfig cloudConfig,
    Platform platform,
    string testName)
{
    return cloudConfig.Provider switch
    {
        CloudProvider.BrowserStack => CreateBrowserStackContext(cloudConfig, platform, testName),
        CloudProvider.SauceLabs => CreateSauceLabsContext(cloudConfig, platform, testName),
        _ => CreateLocalContext(platform, testName)
    };
}
```

### 11.7.2 Usage in Tests

```csharp
public class CloudUITestBase : IDisposable
{
    protected ITestContext Context { get; }
    
    public CloudUITestBase()
    {
        var cloudConfig = CloudProviderConfig.FromEnvironment();
        var platform = Platform.FromEnvironment();
        
        Context = TestContextFactory.CreateFromCloud(
            cloudConfig,
            platform,
            GetType().Name);
    }
}
```

---

## 11.8 CI/CD Integration

### 11.8.1 GitHub Actions

```yaml
env:
  CLOUD_PROVIDER: BrowserStack
  CLOUD_USERNAME: ${{ secrets.BROWSERSTACK_USERNAME }}
  CLOUD_ACCESS_KEY: ${{ secrets.BROWSERSTACK_ACCESS_KEY }}
  CLOUD_PROJECT: Oravey
  CLOUD_BUILD: ${{ github.run_number }}
```

### 11.8.2 Azure DevOps

```yaml
variables:
  - name: CLOUD_PROVIDER
    value: SauceLabs
  - name: CLOUD_USERNAME
    value: $(SauceLabsUser)
  - name: CLOUD_ACCESS_KEY
    value: $(SauceLabsKey)
```

---

## 11.9 Test Execution Reporting

### 11.9.1 Session Update

After test completion, update session status:

```csharp
public void UpdateSessionStatus(bool passed, string? reason = null)
{
    // BrowserStack API
    // PUT /automate/sessions/{sessionId}.json
    // Body: { "status": "passed", "reason": "..." }
    
    // SauceLabs API
    // PUT /rest/v1/{user}/jobs/{jobId}
    // Body: { "passed": true, "custom-data": {...} }
}
```

### 11.9.2 Log CSV Format

```
Timestamp;TestName;PageName;ControlId;Action;Value;ExpectedValue;Result;Message
2024-01-15T10:30:45;Cloud_Login_Test;LoginPage;UsernameInput;EnterText;admin;;Passed;BrowserStack
```

---

## 11.10 Best Practices

### 11.10.1 DO

- ✅ Use environment variables for credentials
- ✅ Set meaningful project/build/session names
- ✅ Enable video recording for debugging
- ✅ Update session status after test
- ✅ Use real devices for final verification
- ✅ Handle longer timeouts for cloud execution

### 11.10.2 DON'T

- ❌ Hardcode credentials in code
- ❌ Run all tests on cloud (expensive)
- ❌ Skip local testing
- ❌ Use cloud for development/debugging
- ❌ Forget to clean up sessions

---

*Next: [Standardized Logging](21d12_StandardizedLogging.md)*
