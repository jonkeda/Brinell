I have a .NET MAUI application running on Windows and I want to make MAUI gestures (tap, swipe, drag, pinch-like interactions, and custom gesture recognizers) accessible to UI Automation clients such as FlaUI without modifying or forking FlaUI itself.

Design and implement a complete architecture where:

1. The MAUI application exposes custom UI Automation providers/patterns.
2. Existing FlaUI installations remain unchanged.
3. Test code can access the custom automation functionality through extension methods or direct UIA access.
4. The solution follows Windows UI Automation best practices.
5. Accessibility implications are considered.
6. The implementation is maintainable across future .NET MAUI releases.
7. integrated into brinell

Provide:

- Architectural overview
- Sequence diagrams
- Component diagrams
- Key design decisions and tradeoffs
- C# implementation examples

Specifically answer:

- How custom UIA patterns are registered
- How custom pattern IDs are created and managed
- How a MAUI VisualElement can expose a custom automation provider
- Whether a MAUI Handler, PlatformView, AutomationPeer equivalent, or WinUI accessibility bridge should be used
- How to expose actions such as:
  - SwipeLeft
  - SwipeRight
  - SwipeUp
  - SwipeDown
  - LongPress
  - DoubleTap
- How clients discover custom patterns
- How FlaUI can retrieve and invoke the custom pattern without source changes
- How to implement a FlaUI extension library wrapping the custom pattern
- Error handling and versioning strategy
- Security considerations
- Unit and integration testing strategy

Generate production-quality C# code for:

- Custom COM UI Automation interfaces
- UI Automation provider implementation
- MAUI integration layer
- Pattern registration
- FlaUI extension methods
- Example usage from automated tests

Assume:
- .NET 10
- .NET MAUI
- Windows 11
- UIA3
- FlaUI.UIA3

Prefer standards-based UI Automation solutions over synthetic mouse input where possible.

If any platform limitations exist in MAUI or WinUI, explain them and propose workarounds.

Before proposing code, analyze whether WinUI 3 and .NET MAUI currently allow custom UI Automation providers to be attached to arbitrary controls. If not, identify the exact extensibility points available in the Windows accessibility stack and redesign the solution accordingly.

and create a plan so we can do it step by step