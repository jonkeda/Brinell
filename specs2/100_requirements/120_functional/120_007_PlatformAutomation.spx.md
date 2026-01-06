# functional PlatformAutomation
- **id**: FR-007
- **title**: Native automation library integration
- **priority**: high
- **status**: approved
- **tags**: platform, integration, drivers

The framework must integrate with native automation libraries for each platform.

## capabilities

### WpfPlatform
- **id**: FR-007.1
- **title**: WPF automation via FlaUI

The framework must use FlaUI for WPF automation. The framework must access UI Automation 3 (UIA3) directly. The framework must support all standard WPF controls.

### MauiPlatform
- **id**: FR-007.2
- **title**: MAUI automation via Appium

The framework must use Appium WebDriver for MAUI automation. The framework must support Windows, Android, and iOS targets. The framework must support platform-specific gestures (mobile).

### MobileGestures
- **id**: FR-007.2.1
- **title**: Mobile gesture support

Mobile platform implementations (MAUI for Android/iOS) must support:
- Tap — Single tap/touch
- DoubleTap — Two taps in quick succession
- LongPress — Extended press with configurable duration
- Swipe — Directional swipe gesture (Left, Right, Up, Down)

Click should alias to Tap for mobile. Desktop platforms may implement gestures as no-ops.

### WebPlatform
- **id**: FR-007.3
- **title**: Web automation via Selenium/Playwright

The framework must support Selenium WebDriver for traditional web automation. The framework must support Playwright for modern web applications and Blazor. The framework must support Chrome, Firefox, Edge, and Safari browsers. Each web driver must have its own platform implementation project.

### WinFormsPlatform
- **id**: FR-007.4
- **title**: WinForms automation via FlaUI

The framework must use FlaUI for WinForms automation. The framework must access UI Automation 3 (UIA3) directly. The framework must support all standard WinForms controls.

### StridePlatform
- **id**: FR-007.5
- **title**: Stride Engine automation via named pipes

The framework must support Stride Engine 3D game UI testing. The framework must use named pipes for test-to-game communication. The framework must support Stride UIElement hierarchy.

### DirectDriverAccess
- **id**: FR-007.6
- **title**: No adapter abstraction layers

Platform implementations must access automation drivers directly. The framework must not introduce adapter abstraction layers. Platform implementations must expose native driver capabilities.
