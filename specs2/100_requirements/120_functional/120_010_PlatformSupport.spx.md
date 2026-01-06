# functional PlatformSupport
- **id**: FR-010
- **title**: Supported Platforms and Technologies
- **priority**: high
- **status**: draft
- **category**: Platform and Technology

The framework must support UI test automation across multiple platforms and technologies through independent platform implementations.

## capabilities

### SupportedPlatforms
- **id**: FR-010.1
- **title**: Platform enumeration

The framework must support the following platforms:

| Platform | Description | Automation Approach |
|----------|-------------|---------------------|
| Windows Desktop (WPF) | Windows Presentation Foundation applications | UI Automation API |
| Windows Desktop (WinForms) | Windows Forms applications | UI Automation API |
| Windows MAUI | .NET MAUI on Windows | Mobile automation protocol |
| Android | Android mobile applications | Mobile automation protocol |
| iOS | iOS mobile applications | Mobile automation protocol |
| Web | Browser-based applications | Browser automation protocol |
| Stride | Stride game engine UI | Custom protocol (named pipes) |

### PlatformIdentification
- **id**: FR-010.2
- **title**: Type-safe platform identification

The framework must provide type-safe platform identification:
- Each platform has a unique identifier
- Platform can be queried at runtime
- Platform-specific code paths can be selected based on identifier

### IndependentImplementations
- **id**: FR-010.3
- **title**: Independent platform implementations

Each platform must have a complete, independent implementation:
- No cross-platform dependencies between implementations
- Each implementation uses native automation capabilities directly
- Platform implementations are packaged separately
- A test project references only the platforms it needs

### PlatformCapabilities
- **id**: FR-010.4
- **title**: Platform-specific capabilities

Each platform may expose capabilities specific to that platform:

**Mobile Platforms (Android, iOS, MAUI):**
- Touch gestures (tap, swipe, pinch, long press)
- Device orientation control
- Soft keyboard interaction
- App lifecycle (background, foreground)
- Deep link navigation

**Web Platform:**
- Multiple browser support (Chrome, Firefox, Edge, Safari)
- Tab and window management
- Cookie manipulation
- JavaScript execution
- Network interception (where supported)
- Frame/iframe navigation

**Desktop Platforms (WPF, WinForms):**
- Window management
- Keyboard shortcuts
- Context menus
- System dialogs

**Game Platform (Stride):**
- 3D UI element interaction
- Game loop synchronization

### AutomationProtocols
- **id**: FR-010.5
- **title**: Automation protocol abstraction

The framework must abstract automation protocols:
- Tests target platforms, not specific automation tools
- Multiple automation tools may implement the same platform
- Tool selection is a configuration concern, not a test concern

Example: Web platform may use Selenium or Playwright; test code remains unchanged.

---

## constraints

- Platform implementations must not have runtime dependencies on other platforms
- Tests must be able to run against a single platform without loading other platform code
- Platform detection must work without instantiating automation drivers
