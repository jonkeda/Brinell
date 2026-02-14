# Brinell Specifications

**Last Updated:** February 14, 2026

Consolidated specification documents for the Brinell UI test automation framework.
Source of truth is the code in `srcnew/`. These specs capture design intent and decisions.

## Product Overview

Brinell is a cross-platform UI testing framework for .NET applications that provides a unified API for automating WPF, WinForms, MAUI, HTML/Web, Blazor, and Stride 3D game engine applications. It replaces the need to learn separate automation libraries (FlaUI, Selenium, Appium, Playwright) with a single consistent API.

### Target Users

| User | Need |
|------|------|
| **QA / Test Automation Engineers** | Reliable, maintainable UI test code with consistent patterns across projects |
| **.NET Developers** | Familiar .NET patterns, IntelliSense-friendly APIs, CI/CD integration |
| **Test Leads / Architects** | Multi-platform testing standards, scalable test architectures |

### Business Objectives

1. **Reduce test development time** — ready-to-use control abstractions and base classes
2. **Improve test reliability** — built-in waiting, synchronization, and screenshot capture
3. **Enable cross-platform testing** — single framework for all .NET UI platforms
4. **Support enterprise CI/CD** — standard xUnit integration with parallel execution

### Success Metrics

| Metric | Target |
|--------|--------|
| Test coverage | >80% |
| Build success rate | >95% |
| Time to first test | <30 minutes |
| Documentation coverage | 100% public APIs |

## Structure

| Folder | Purpose |
|--------|---------|
| [requirements/](requirements/) | Functional and non-functional requirements |
| [architecture/](architecture/) | Architecture, decisions, patterns, project structure |
| [controls/](controls/) | Interface and class specifications (SPEC-006 series) |
| [design/](design/) | Sample app designs |
| [active/](active/) | Work-in-progress specs and known issues |

## Key Principles

1. **Interface-first:** `Brinell.Core` contains only interfaces and abstractions — zero platform dependencies
2. **Nullable skip pattern:** `null` parameter = skip the operation (no-op)
3. **Fluent `TScope` chaining:** all actions/assertions return the containing page/scope
4. **Is/Wait/Assert triad:** every queryable property has `Get*()` → `Wait*()` → `Assert*()`
5. **No `Thread.Sleep`:** use framework `Wait*`/`Assert*` methods that poll conditions
6. **Platform-native performance:** use native automation libraries directly, never abstract away capabilities
7. **Fail fast with context:** every failure includes control ID, expected vs actual, timeout, and screenshot
8. **Test writer first:** optimize for discoverability and minimal boilerplate

## Active Codebase

- **Source:** `srcnew/` (active) — `src/` is legacy, pending port
- **Tests:** `testsnew/` (active) — `tests/` is legacy
- **Implemented platforms:** MAUI (Appium + FlaUI drivers)
- **Scaffolded:** Blazor, Html, WPF, WinForms, Stride

## Spec Status Summary

| Spec | Description | Status |
|------|-------------|--------|
| SPEC-003 | Interface hierarchy consolidation | ✅ Mostly complete (16/18 tasks) |
| SPEC-004 | MAUI base control hierarchy | ✅ Mostly complete (13/15 tasks) |
| SPEC-005 | MAUI sample app tabs | ✅ Mostly complete (19/21 tasks) |
| SPEC-006 | MAUI UI tests update | ✅ Complete (6/6 tasks) |
| SPEC-015 | Element lookup optimization | ✅ Implemented |
| SPEC-023 | TabbedPage automation | ✅ Complete |
| SPEC-025 | MAUI control UI tests | 🔲 Draft (0/33 tasks) |
| SPEC-026 | UI test control interaction fixes | 🔲 Pending |
| SPEC-029 | FlaUI Windows driver fixes | 🔶 In progress (15/22 tasks) |
