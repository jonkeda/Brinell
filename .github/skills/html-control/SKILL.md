---
name: html-control
description: Create or extend a Brinell Html ControlObject - a simple control, a container, a component with named parts, or a collection with rows - for an HTML or Blazor element, in srcnew/Brinell.Html or srcnew/Brinell.Blazor. Use when asked to add, write or extend an Html or Blazor control object, container, collection or row, or when an Html UI test needs a control member that does not exist yet.
---

# Write an Html ControlObject

Html controls are hand-written (no generator): `ControlBase<TScope>` in
`srcnew/Brinell.Html/Controls`. The rules are MAUI's (`.docs/contracts/call-model.md`,
`.docs/contracts/control-object.md`). Only the base classes and the platform probe differ.

## Choose the kind

| Kind | Base | Example |
| --- | --- | --- |
| Simple control | `ControlBase<TScope>` or a family base (`ClickableControlBase`, `FocusableControlBase`, `ToggleControlBase`, `RangeControlBase`, `SelectorControlBase`) | `TextInputControl<TScope>` |
| Container or component | `ContainerBase<TParent, TSelf>`; parts are controls declared on it | `TodoStatus<TParent>` in the Todo web tests |
| Frame | `FrameScope<TParent>` | `FramesPage.Inner` |
| Collection with rows | `CollectionObjectBase<TParent, TSelf, TItem>` plus `ItemObjectBase<TSelf>` | `RecyclingListPage.TodoRows` |

## Probe the element

Read it in the browser's DevTools, or with a short Playwright script: the tag, the ARIA role,
the `data-testid`, and the attributes the control reads (`disabled`, `aria-checked`, `value`,
`data-item-key`). Blazor inputs bind on `input`: set them with `Fill`, not `SendKeys`.

## Write the members

- **Every public member is one call.** Reads go through `RunGetWithElement`/`RunWithElement(func)`.
  Waits go through `RunWaitWithElement`, or `RunWaitWithOptionalElement` when "absent" is an
  answer. Assertions go through `RunAssertWithElement` or `RunAssert(element => ...)`, which
  wait (R7). `Try*` members answer about now.
- **Actions act once.** Use `RunDoWithElement` or `RunSetWithElement`. When the action changes
  state, confirm it inside the action with
  `ActAndConfirm("Fill", () => e.Fill(v), () => e.InputValue, x => x == v, $"'{v}'", timeoutMs)`.
  Never loop over the action.
- **Readiness before acting** goes in `EnsureReadyForActionCore` (enabled, for example), and
  throws `ElementNotReadyException`.
- **Every public member takes `int? timeoutMs = null`.** A wait below the call (a scroll) takes
  `CallRemainingMs`.
- **Containers:** override `ProbeContentReadiness(root)` for a content check (a spinner gone),
  and set `AsksParent => false` for a scope shown over its parent.
- **No catch-alls, no sleeps.** A failure is an answer or a signal (`StaleElementException`,
  `ElementNotReadyException`, `AppUnavailableException`).

## Prove it

Add a unit test in `testsnew/Brinell.Html.Tests` against a mocked `IHtmlElement`. A mocked
element must show the effect of an action (a `Callback` that sets the value), or a confirming
member fails, and that is the point. Then add a UI test on the Blazor sample with the
`html-ui-test` skill.
