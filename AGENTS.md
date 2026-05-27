---
title: Brinell Agent Instructions
description: Local entry point for AI agents working in the Brinell submodule and Exact.Construction UITest integration
---

## Read First

Before changing Brinell framework code or Exact.Construction UITest code, read the relevant GitHub instructions. Do not rely on prior conversation context.

Required instruction files:

* [Brinell Copilot Instructions](.github/copilot-instructions.md)
* [Exact.Construction Copilot Instructions](../.github/copilot-instructions.md)
* [Brinell page and ControlObject rules](../.github/instructions/uitest-brinell-page-controlobject.instructions.md)
* [Brinell synchronization rules](../.github/instructions/uitest-brinell-synchronization.instructions.md)
* [Cascading failure prevention](../.github/instructions/uitest-cascading-failure-prevention.instructions.md)
* [Diagnostics and triage](../.github/instructions/uitest-diagnostics-triage.instructions.md)
* [Runtime auth and mock backend](../.github/instructions/uitest-runtime-auth-mockbackend.instructions.md)
* [UITest scripts](../.github/instructions/uitest-scripts.instructions.md)

Sibling conversion references:

* `E:/repos/Clay/ClaiConstructionMobile/.github/copilot-instructions.md`
* `E:/repos/Clay/ClaiConstructionMobile/.github/instructions/*.instructions.md`
* `E:/repos/Clay/ClayBouwMobile/.github/documentation/convert-flow-overview.md`
* `E:/repos/Clay/ClayBouwMobile/.github/documentation/convert-feature-guide.md`
* `E:/repos/Clay/ClayBouw/.github/agents/conversion/*.yaml`

Reference map:

* [AI assistant reference map](docs/ai-assistant-references.md)

## Brinell UITest Rules

* Build tests through Brinell page objects and ControlObjects.
* Put repeated interaction behavior in Brinell controls, not Exact page-local helpers.
* Keep test methods focused on user intent and assertions, not locator plumbing.
* Use semantic operations such as `SelectItem`, `SetText`, `TryClick`, and `WaitReady`.
* Prefer UI Automation patterns such as Invoke, SelectionItem, ExpandCollapse, Value, and RangeValue before any pointer strategy.
* Do not expose direct mouse movement as a normal public test API.
* Pointer or coordinate input is only for explicit gesture-only surfaces, such as drawing/signature, and must be opt-in.

## Synchronization Rules

* Never use `Thread.Sleep`, arbitrary `Task.Delay`, or longer waits as the first fix.
* Wait for concrete UI state: loaded pages, visible/enabled controls, text, busy sentinel changes, request observation, or navigation completion.
* Use existing Brinell wait/assert APIs and `TestConstants` timeouts.
* When a UI test fails, inspect screenshots, diagnostics, runner output, and the app runtime log before changing code.

## Exact.Construction UITest Rules

* For mock UI tests, use mock auth and WireMock through the fixture/runtime scripts.
* Use sibling conversion docs when comparing Bouw7 native behavior,
  conversion phases, fidelity gates, and test expectations.
* Register root/list pages with `RegisterRootPage(...)`.
* Register detail/edit/modal pages with `RegisterPage(...)`.
* Use `RunWithRecovery(...)` in module tests.
* Keep fixture constructors shell-focused; page ownership belongs in test classes.
* Do not add ad-hoc cleanup/back-navigation loops inside test methods.

## Verification

For Brinell MAUI control changes, run non-UI checks first:

```powershell
dotnet test Brinell\testsnew\Brinell.Maui.Tests\Brinell.Maui.Tests.csproj -v:minimal /nr:false
dotnet build Brinell\srcnew\Brinell.Maui.FlaUI\Brinell.Maui.FlaUI.csproj -f net10.0-windows -v:minimal /nr:false
```

For Exact.Construction UI tests, prefer the repository scripts. For a focused mock run, use the `.cnv2` wrapper from the workspace root:

```powershell
.cnv2\tools\run-focused-ui-test.ps1 TestMethodName
```

Leave `BRINELL_ALLOW_POINTER_INPUT` unset unless the user explicitly allows visible pointer movement for that run.
