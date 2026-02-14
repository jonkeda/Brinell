# goal UnifiedTestAPI
- **id**: G-001
- **title**: Provide a unified API for cross-platform UI testing
- **priority**: high
- **success**: Test writers can use the same control and page object patterns across all supported platforms (MAUI, Blazor, WPF, WinForms, Stride) without platform-specific code in test logic

Write once, test everywhere. Test writers should focus on test logic, not platform differences.

## rationale

Modern .NET applications span multiple platforms and UI frameworks. Teams building products for desktop, mobile, and web shouldn't need to learn different automation tools and patterns for each platform. A unified abstraction reduces cognitive load and enables code reuse.

## achievedBy

- FR-001: Multi-Platform Support
- FR-002: Control Object Pattern
- FR-003: Page Object Pattern
- FR-007: Platform-Specific Automation
