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
