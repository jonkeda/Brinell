# Core Architecture

**Version:** 3.1 | **Status:** Active

## Four-Layer Architecture

```
┌─────────────────────────────────┐
│         Test Projects           │  testsnew/, samples/
│    (xUnit, Page Objects)        │
├─────────────────────────────────┤
│      Platform Libraries         │  Brinell.Maui, Brinell.Blazor, etc.
│   (Control implementations)     │
├─────────────────────────────────┤
│      Technology Adapters        │  Brinell.Maui.Appium, Brinell.Maui.FlaUI
│  (Driver/Element impls)         │
├─────────────────────────────────┤
│          Brinell.Core           │  Interfaces, Locators, Exceptions
│     (Zero platform deps)        │
└─────────────────────────────────┘
```

## Layer Rules

| Layer | Contains | Dependencies |
|-------|----------|-------------|
| **Core** | Interfaces (`IControlObject<TScope>`, `IDriver<TElement>`, `IElement<TSelf>`), Locators, Exceptions, Abstractions | .NET Standard only |
| **Platform** | Control base classes, platform pages, gestures | Core + platform SDK |
| **Technology** | Driver/Element implementations (Appium, FlaUI, Playwright) | Core + Platform + automation library |
| **Tests** | Test fixtures, page objects, assertions | Everything above + xUnit |

**Key constraint:** Core has zero references to any platform or automation library.

## Project Structure (srcnew/)

| Project | Layer | Purpose |
|---------|-------|---------|
| `Brinell.Core` | Core | Interfaces, locators, exceptions |
| `Brinell.Maui` | Platform | MAUI controls, pages, context |
| `Brinell.Maui.Appium` | Technology | Appium driver for MAUI |
| `Brinell.Maui.FlaUI` | Technology | FlaUI driver for MAUI (desktop) |
| `Brinell.Maui.CommunityToolkit` | Platform ext | CommunityToolkit control support |
| `Brinell.Blazor` | Platform | Blazor controls (scaffolded) |
| `Brinell.Html` | Platform | HTML/web controls (scaffolded) |
| `Brinell.Wpf` | Platform | WPF controls (scaffolded) |
| `Brinell.WinForms` | Platform | WinForms controls (scaffolded) |
| `Brinell.Stride` | Platform | Stride game engine (scaffolded) |
| `Brinell.Automation` | Technology | Stride automation server |
| `Brinell.Mocking` | Testing | API mock/stub support |

## Driver Abstraction

```
IDriver<TElement> ←── IMauiDriver ←── AppiumMauiDriver
                                  ←── FlaUIMauiDriver

IElement<TSelf>   ←── IMauiElement ←── AppiumMauiElement
                                   ←── FlaUIMauiElement
```

Drivers are swappable via `MauiDriverFactory` configured through environment variables:
- `BRINELL_DRIVER=Appium|FlaUI`
- `APPIUM_PLATFORM=windows|android|ios`

## Platform Isolation

Each platform is fully self-contained:
- Own `Controls/` folder with all control implementations
- Own `Context/` for `TestContext` implementation
- Own `Pages/` for page object base classes
- Own `Testing/` for test fixture helpers
- No cross-platform references at the platform layer

## Technology Stack

### Core

- **Language:** C# 13 (latest) with nullable reference types, implicit usings
- **Runtime:** .NET 8.0 (LTS), .NET 9.0 (current), .NET 10.0 (preview) — multi-targeting
- **Compilation:** Treat warnings as errors

### Key Dependencies

| Package | Version | Layer | Purpose |
|---------|---------|-------|---------|
| xunit | 2.9.3 | Testing | Test framework |
| Microsoft.NET.Test.Sdk | 17.14.0 | Testing | Test SDK |
| Moq | 4.20.70 | Testing | Mocking |
| AutoFixture | 4.18.1 | Testing | Test data generation |
| Bogus | 35.5.1 | Testing | Fake data |
| FlaUI.Core / FlaUI.UIA3 | 5.0.0 | Technology | Windows UI Automation |
| Appium.WebDriver | 8.0.1 | Technology | Mobile/cross-platform automation |
| Microsoft.Playwright | 1.50.0 | Technology | Web automation |
| Stride.Engine | 4.3.0.2507 | Technology | 3D game engine |
| WireMock.Net | 1.6.10 | Testing | API mocking |
| Microsoft.EntityFrameworkCore | 10.0.0 | Testing | Database fixtures |
| Serilog | 4.1.0 | Infrastructure | Structured logging |

### Automation Protocols

| Protocol | Library | Platform |
|----------|---------|----------|
| UIA3 (UI Automation 3) | FlaUI | Windows desktop (WPF, WinForms, MAUI) |
| Appium Protocol | Appium.WebDriver | Mobile + cross-platform MAUI |
| CDP (Chrome DevTools Protocol) | Playwright | Web (HTML, Blazor) |
| Named Pipes | Custom | Stride game engine |

### Distribution

- **Method:** NuGet.org package registry
- **Package IDs:** `Brinell.Core`, `Brinell.Maui`, `Brinell.Wpf`, etc.
- **Versioning:** Semantic versioning (currently 0.1.0 pre-release)
- **Pre-release policy:** Breaking changes acceptable before 1.0
