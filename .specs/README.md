# Brinell Specifications

**Last Updated:** February 14, 2026

Consolidated specification documents for the Brinell UI test automation framework.
Source of truth is the code in `srcnew/`. These specs capture design intent and decisions.

## Structure

| Folder | Purpose |
|--------|---------|
| [requirements/](requirements/) | Functional and non-functional requirements |
| [architecture/](architecture/) | Architecture, decisions, patterns |
| [controls/](controls/) | Interface and class specifications (SPEC-006 series) |
| [design/](design/) | Sample app designs |
| [active/](active/) | Work-in-progress specs and known issues |

## Key Principles

1. **Interface-first:** `Brinell.Core` contains only interfaces and abstractions — zero platform dependencies
2. **Nullable skip pattern:** `null` parameter = skip the operation (no-op)
3. **Fluent `TScope` chaining:** all actions/assertions return the containing page/scope
4. **Is/Wait/Assert triad:** every queryable property has `Get*()` → `Wait*()` → `Assert*()`
5. **No `Thread.Sleep`:** use framework `Wait*`/`Assert*` methods that poll conditions

## Active Codebase

- **Source:** `srcnew/` (active) — `src/` is legacy, pending port
- **Tests:** `testsnew/` (active) — `tests/` is legacy
- **Implemented platforms:** MAUI (Appium + FlaUI drivers)
- **Scaffolded:** Blazor, Html, WPF, WinForms, Stride
