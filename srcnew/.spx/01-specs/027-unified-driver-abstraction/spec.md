# SPX-027: Unified Driver Abstraction

**Status:** Implemented in srcnew

## Architecture

Two-level abstraction separating platform concepts from technology:

```
IDriver<TElement>  ←  IMauiDriver  ←  AppiumMauiDriver / FlaUIMauiDriver
IElement<TSelf>    ←  IMauiElement  ←  AppiumMauiElement / FlaUIMauiElement
```

## Key Design

| Interface | Layer | Purpose |
|-----------|-------|---------|
| `IDriver<TElement>` | Core | Generic driver: find elements, screenshots, session |
| `IElement<TSelf>` | Core | Generic element: click, text, attributes, children |
| `IMauiDriver` | Platform | MAUI-specific: extends with any MAUI-only capabilities |
| `IMauiElement` | Platform | MAUI-specific: extends with MAUI element behaviors |

## Locator Translation

Each technology adapter translates `Locator` to its native format:
- **Appium:** `AutomationId` → `MobileBy.AccessibilityId`, `XPath` → `By.XPath`
- **FlaUI:** `AutomationId` → `AutomationId`, `XPath` → `XPath`

## Driver Factory

`MauiDriverFactory` creates the appropriate driver based on environment config:
- `BRINELL_DRIVER=Appium|FlaUI` selects the adapter
- `MauiDriverOptions` encapsulates all configuration

## Implementation

All driver/element code lives in `srcnew/`:
- `Brinell.Core/Interfaces/` — `IDriver`, `IElement`
- `Brinell.Maui/Interfaces/` — `IMauiDriver`, `IMauiElement`
- `Brinell.Maui.Appium/` — `AppiumMauiDriver`, `AppiumMauiElement`
- `Brinell.Maui.FlaUI/` — `FlaUIMauiDriver`, `FlaUIMauiElement`
