# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial release of Brinell UI testing framework
- `Brinell.Core` - Core abstractions and interfaces
  - `IDriverAdapter` and `IElementAdapter` for platform abstraction
  - `ITestContext` with platform identification
  - Control interfaces: `IButton`, `ICheckBox`, `ITextBox`, etc.
  - Test attributes: `[UITest]`, `[SmokeTest]`, `[Platform]`, `[Priority]`
  - Exception types for common UI test failures
  - Screenshot service abstractions
  - CSV test logging support
  
- `Brinell.Wpf` - WPF application automation
  - FlaUI-based driver and element adapters
  - Control implementations: Button, CheckBox, ComboBox, ListBox, etc.
  - Page Object base class with control discovery
  - Visual validation support
  
- `Brinell.Html` - Web application automation
  - Selenium WebDriver-based implementation
  - HTML control wrappers
  - Support for Chrome, Firefox, Edge browsers
  
- `Brinell.Maui` - Mobile/MAUI automation
  - Appium-based driver implementation
  - MAUI control wrappers
  - Android and iOS support
  
- `Brinell.Mocking` - API mocking
  - WireMock.Net integration
  - Fluent API stub builder
  - Request/response recording

### Changed
- Migrated from Oravey.UITestFramework namespace

### Fixed
- N/A (initial release)

## [0.1.0] - TBD

- Initial public release
