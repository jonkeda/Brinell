# Running MAUI Android UI Tests

This guide covers running Brinell UI tests against the MAUI sample app on Android.

## Prerequisites

1. **Node.js** installed (for Appium)
2. **Appium** with UiAutomator2 driver
3. **Android SDK** with platform-tools
4. **Android Emulator** running or physical device connected
5. **Java JDK** (required by UiAutomator2)

## Quick Start

```powershell
# 1. Start Android emulator (if not running)
emulator -avd Medium_Phone_API_36

# 2. Start Appium server (in separate terminal)
appium --base-path /

# 3. Set platform and run tests
$env:APPIUM_PLATFORM = "android"
cd testsnew/Brinell.Maui.UITests
dotnet test --filter "ButtonControlTests"
```

## Detailed Setup

### 1. Install Appium and Android Driver

```powershell
# Install Appium globally
npm install -g appium

# Install UiAutomator2 driver for Android
appium driver install uiautomator2

# Verify installation
appium driver list --installed
# Should show: uiautomator2
```

### 2. Start Android Emulator

```powershell
# List available emulators
emulator -list-avds

# Start emulator (replace with your AVD name)
emulator -avd Medium_Phone_API_36

# Verify device is connected
adb devices
# Should show: emulator-5554   device
```

**Tip:** The emulator must be fully booted before running tests. Wait for the home screen to appear.

### 3. Build the MAUI App for Android

```powershell
cd samples/Brinell.Samples.Maui.App

# Build the Android APK
dotnet build -f net10.0-android
```

The signed APK will be at:
```
samples/Brinell.Samples.Maui.App/bin/Debug/net10.0-android/com.brinell.samples.maui-Signed.apk
```

**Important:** The project must have `EmbedAssembliesIntoApk=true` in the .csproj for Fast Deployment to work correctly:

```xml
<PropertyGroup Condition="'$(TargetFramework)' == 'net10.0-android'">
    <EmbedAssembliesIntoApk>true</EmbedAssembliesIntoApk>
</PropertyGroup>
```

### 4. Start Appium Server

Start Appium in a **separate terminal window**:

```powershell
# Start Appium with base path (required for newer Appium versions)
appium --base-path /
```

**Verify server is running:**
```powershell
Invoke-RestMethod "http://127.0.0.1:4723/status"
# Should return: ready = True
```

### 5. Set Platform Environment Variable

The test fixture uses the `APPIUM_PLATFORM` environment variable to determine which platform to target:

```powershell
$env:APPIUM_PLATFORM = "android"
```

### 6. Run Tests

```powershell
cd testsnew/Brinell.Maui.UITests

# Run all button tests
dotnet test --filter "ButtonControlTests"

# Run specific test
dotnet test --filter "Button_IsExists_ReturnsTrue"

# Run with verbose output
dotnet test --filter "ButtonControlTests" -v n
```

## Android-Specific Notes

### AutomationId Mapping

On Android, MAUI's `AutomationId` maps to the `resource-id` attribute:

| MAUI Property | Android Attribute | Example Value |
|---------------|-------------------|---------------|
| `AutomationId="IncrementButton"` | `resource-id` | `com.brinell.samples.maui:id/IncrementButton` |
| `Title="Basics"` (on tabs) | `content-desc` | `Basics` |

The Brinell framework automatically handles this mapping - you use `AutomationId` in your page objects and it converts to the correct locator for Android.

### Tab Navigation

MAUI TabbedPage renders tabs with `content-desc` (accessibility description) on Android, not `resource-id`. The `TabViewControl` class handles this with a fallback locator.

### Activity Names

MAUI generates hashed activity names on Android (e.g., `crc643b83d6491f48953d.MainActivity`). The test fixture is configured to wait for any activity (`appWaitActivity: "*"`).

## Troubleshooting

### "Connection refused" Error

```
No connection could be made because the target machine actively refused it. (127.0.0.1:4723)
```

**Solution:** Appium server is not running. Start it:
```powershell
appium --base-path /
```

### "Device not found" Error

```
Could not find a connected Android device
```

**Solutions:**
1. Verify emulator is running: `adb devices`
2. Restart ADB: `adb kill-server && adb start-server`
3. Check emulator is fully booted (home screen visible)

### App Crashes on Launch

If you see `OpenQA.Selenium.InvalidSessionIdException` or the app immediately crashes:

1. Verify `EmbedAssembliesIntoApk=true` is set in the .csproj
2. Rebuild the app: `dotnet build -f net10.0-android`
3. Try installing manually: `adb install -r <path-to-apk>`

### Element Not Found

```
Element not found with locator: AutomationId:MyButton
```

**Debug steps:**
1. Add a diagnostic test to dump the page source
2. Verify the element has `AutomationId` set in XAML
3. Check if element is on the current tab/page
4. Use Appium Inspector to explore the UI hierarchy

### Test Timeout

**Solutions:**
- Increase timeout in test: `[Fact(Timeout = 60000)]`
- Ensure app is on correct tab before testing controls
- Check if UI is blocked by a dialog or loading indicator

## Appium Inspector

Use Appium Inspector to explore the Android UI hierarchy:

1. Download from: https://github.com/appium/appium-inspector/releases
2. Configure connection:
   - Remote Host: `127.0.0.1`
   - Remote Port: `4723`
   - Remote Path: `/`
3. Set capabilities:
   ```json
   {
     "platformName": "Android",
     "appium:automationName": "UiAutomator2",
     "appium:app": "E:\\path\\to\\com.brinell.samples.maui-Signed.apk"
   }
   ```
4. Start Session and explore elements

## Complete Example Session

```powershell
# Terminal 1: Start emulator
emulator -avd Medium_Phone_API_36

# Terminal 2: Start Appium
appium --base-path /

# Terminal 3: Run tests
$env:APPIUM_PLATFORM = "android"
cd e:\repos\Clay\ClaiConstructionMobile\Brinell\testsnew\Brinell.Maui.UITests
dotnet build
dotnet test --filter "ButtonControlTests" -v n
```

## See Also

- [MAUI.md](MAUI.md) - Windows MAUI testing guide
- [01-quick-start.md](../01-quick-start.md) - Framework quick start
- [13-troubleshooting.md](../13-troubleshooting.md) - General troubleshooting
