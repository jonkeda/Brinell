# Common Core layer

Start here.

| File | What it is | Authoritative for |
| --- | --- | --- |
| [plan.md](plan.md) | Steps, order, status, verification. L1 (the leaf layer) is done; L2 is next | **what to do next, and what is done** |
| [design.md](design.md) | The new interfaces, the calls layer, the exceptions, using **`ICore*`** names throughout | **what to build** |
| [naming.md](naming.md) | The full rename table (existing Core name -> new `ICore*` name; per-stack aliases) | **what things are called** |
| [migration.md](migration.md) | Per-stack playbook: MAUI, Html, WPF, WinForms, NativeAndroid, Stride | **how each stack moves** |
| [coexistence.md](coexistence.md) | How the existing `Bruinet.Core.Interfaces.IElement<T>` and the new `ICoreElement` live in the same solution while stacks migrate | **the migration invariants** |

## Origin

This document supersedes the "move down" draft in
[`../stale-readiness/move-down.md`](https://github.com/exact-mobile-apps/test-tooling/blob/7162ac7/.my/stale-readiness/move-down.md) with one change: the
common layer uses **new names** (`ICoreElement`, `ICoreDriver`, `ICoreScope`, ...) instead of
reusing the existing `IElement<T>`, `IDriver<T>`, `IElementScope<T>` and friends.

The rest of the move-down draft still holds: MAUI passed its steps 0-9 on its own interfaces
(`IMauiElement`, `IMauiDriver`, `IMauiElementScope`, `IMauiPage`, `IMauiTestContext`); Html has a
planned slice in [`../html/`](https://github.com/exact-mobile-apps/test-tooling/blob/7162ac7/.my/html/) that will land on its own `IHtml*` interfaces; the other
stacks (NativeAndroid, WPF, WinForms, Stride) still use Core's old interfaces.

**The new names let the two layers coexist.** The old `Bruinet.Core.Interfaces.IElement<T>`
stays on disk while `Bruinet.Core.Automation.ICoreElement` grows next to it. A stack moves when
its own project switches from the old set to the new set. The old set is deleted when the last
stack has moved.

## Goal

One shape for every stack:

- one element (`ICoreElement`);
- one driver (`ICoreDriver`);
- one scope (`ICoreScope`, with `ICorePage`, `ICoreContainer`, `ICoreCollection`, `ICoreItem`
  extending it);
- one test context (`ICoreTestContext`);
- one calls layer (`Bruinet.Core.Automation.Calls/*`);
- one exception set (`CoreStaleElementException`, `CoreElementNotReadyException`,
  `CoreScopeNotReadyException`, `CoreAppUnavailableException`);
- one readiness model (`CoreScopeReadiness`).

Every per-stack interface becomes a **thin extension** that only adds the members that stack
alone needs (`IMauiElement.Platform`, `IMauiElement.ShellChrome`, `IHtmlElement.Fill`, ...). No
stack redefines what an element or a scope *is*.

## Scope

- **In:** `srcnew/Bruinet.Core` (new namespace `Bruinet.Core.Automation`), the per-stack
  interface projects that switch over, and the docs.
- **Out:** behaviour changes. The new layer is the shape MAUI proved (rules R0-R9 in
  `../stale-readiness/design.md`). Anything that changes what a call *does* belongs in a
  separate project, tracked next to this one.
- **Out:** deleting the old `Bruinet.Core.Interfaces.IElement<T>` and friends. That happens
  with the **last** stack, in a follow-up whose only job is the delete.

## Not decided yet

Every question tagged **?C1**-**?C9** in `plan.md` and `design.md`. This is a **draft plan**,
not an accepted design.
