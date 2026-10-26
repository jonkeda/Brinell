# Run MAUI iOS Tests

Working directory: Bruinet root. Host: macOS with Xcode and a booted iOS simulator.

## Prerequisites

- Xcode with an iOS simulator runtime; boot it with `xcrun simctl boot <udid>`.
- Node, Appium 3 and the XCUITest driver (`appium driver install xcuitest`).
- Appium server: `appium --address 127.0.0.1 --port 4723 --relaxed-security`.

## Build App

```bash
dotnet build samples/Bruinet.Samples.Maui.App/Bruinet.Samples.Maui.App.csproj -f net10.0-ios -c Debug \
  -p:RuntimeIdentifier=iossimulator-arm64 -p:EnableWindowsTargeting=true -v:minimal /nr:false
```

Build the Shell app the same way: the Shell fixture installs its own app.

```bash
dotnet build samples/Bruinet.Samples.Maui.ShellApp/Bruinet.Samples.Maui.ShellApp.csproj -f net10.0-ios -c Debug   -p:RuntimeIdentifier=iossimulator-arm64 -p:EnableWindowsTargeting=true -v:minimal /nr:false
```

- `EnableWindowsTargeting=true` lets restore pass for the project's Windows TFM on macOS.
- Add `-p:ValidateXcodeVersion=false` when the installed Xcode is newer than the iOS workload expects.
- On an Intel Mac use `iossimulator-x64`. The fixture picks the folder from the host architecture.

## Environment

```bash
export APPIUM_PLATFORM=ios
export APPIUM_DEVICE_NAME="iPhone 17 Pro Max"
export APPIUM_PLATFORM_VERSION=26.5
export APPIUM_UDID=<simulator udid>          # xcrun simctl list devices booted
# optional: APPIUM_SERVER_URI, APPIUM_APP_PATH (overrides the fixture's default .app path)
```

## Run

Use the mobile head (plain net10.0, Appium driver). Start with one test:

```bash
dotnet test testsnew/Bruinet.Maui.UITests.Mobile \
  --filter "FullyQualifiedName~ButtonTests.Button_IsExists_ReturnsTrue"
```

The first session builds WebDriverAgent and takes longer.

`MauiDriverOptions.LaunchSettings` reach an iOS app as environment variables, through the
XCUITest `processArguments: { env: {...} }` capability. The test process's own environment does
not reach the app.

## Tree Differences

- A view with `SemanticProperties.Description` becomes one accessibility element on iOS.
  XCUITest hides its children and reports the description as the element's label. A
  ControlObject that reads a child of such a view must fall back to the element's `Name`,
  which Bruinet reads from the iOS `label`.
- Zero-size elements are missing from the tree. An empty `Label` measures 0 high on iOS.
