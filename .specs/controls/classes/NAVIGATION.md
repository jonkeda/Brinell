# Navigation Controls

**Source of truth:** `srcnew/Brinell.Maui/Controls/Navigation/`

## Controls

| Control | Interfaces | MAUI Control |
|---------|-----------|-------------|
| `MauiTabBarControl` | `IClickableControlObject` | Shell TabBar |
| `MauiFlyoutItemControl` | `IClickableControlObject` | Shell FlyoutItem |
| `MauiNavigationPageControl` | `IControlObject` | NavigationPage |

## Navigation Models

MAUI supports multiple navigation patterns:
- **Shell TabBar** — Bottom tab navigation (uses XPath due to AutomationId issues)
- **TabbedPage** — Tabbed navigation (AutomationId timing issue — see active/SPEC-023)
- **FlyoutPage** — Hamburger menu navigation
- **Shell routing** — Programmatic `GoToAsync`

The sample app currently uses TabbedPage for tab navigation.
