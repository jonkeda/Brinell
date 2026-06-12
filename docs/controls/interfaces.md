# Core Interfaces

Core interfaces define the contracts that page objects, controls, contexts, and
drivers use across platforms.

## Primary Concepts

| Concept | Responsibility |
| --- | --- |
| Control object | Represents one UI element or logical control |
| Page object | Represents one page, view, screen, or dialog |
| Locator | Describes how to find a UI element |
| Test context | Owns driver/session services for a test |
| Driver | Platform-specific element discovery and action backend |
| Artifact service | Screenshots, manifests, summaries, and diagnostics |

## Interface Rules

- Add shared capability contracts to `Brinell.Core`.
- Keep platform element types out of core interfaces.
- Prefer small capability interfaces over one large control API.
- Keep nullable and timeout behavior explicit.
- Add tests for new contracts and default behavior.

## Naming

- Interfaces use `I...`.
- Capability interfaces describe what a control can do.
- Platform implementations keep the platform name in the project, not in the
  core interface name.

## Implementation Checklist

When adding a new control capability:

1. Add or update the core interface.
2. Implement the capability in the relevant platform controls.
3. Add unit tests for shared behavior.
4. Add UI tests only if live driver behavior needs coverage.
5. Update [Control Object Index](index.md).
