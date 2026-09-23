# Coexistence

For the whole span of the migration - from `plan.md` step 1 (adding the first `ICore*`
interface) to step 10 (deleting the last old one) - **both layers live in the solution**:

- old: `Brinell.Core.Interfaces.IElement<TSelf>`, `IElementScope<TElement>`,
  `IPageObject<TElement>`, `IDriver<TElement>`, `ITestContext<TElement>`,
  `IContainerControl<TElement>`, `IContainerObject<TElement>`, `ControlObjectBase<TScope>`, and
  the old capability interfaces;
- new: `Brinell.Core.Automation.ICoreElement`, `ICoreScope`, `ICorePage`, `ICoreDriver`,
  `ICoreTestContext`, `ICoreContainer`, `ICoreCollection<TItem>`, `ICoreItem<TCollection>`,
  `CoreViewBase<TScope>`, and the `ICore*` capability interfaces.

The new names (`ICore*` instead of reusing `IElement<T>`) are what makes coexistence possible:
nothing collides, nothing is bridged.

## Invariants during migration

1. **The old interfaces are never edited.** Not renamed, not moved, not extended. If a bug is
   found in an old interface's default method, it is fixed by moving the fix into the new
   `ICore*` counterpart, and the old one is left as-is. This is how a stack that has not
   moved keeps working the same way it did.

2. **A stack is either on the old or on the new. Never on both.** MAUI stops implementing
   Core's `IElement<IMauiElement>`, `IDriver<IMauiElement>` etc. before `plan.md` step 4 ships
   (it already did in stale-readiness step 1). When MAUI's step 4 lands, `IMauiElement`
   inherits from `ICoreElement` and stops being anywhere near the old `IElement<T>`. Same for
   every other stack.

3. **The calls layer belongs to the new set only.** MAUI's `Brinell.Maui/Calls/*` is deleted in
   step 4; nothing outside `Brinell.Core.Automation.Calls` is a calls-layer type after that.

4. **UAT recognises both `IPageObject` and `ICorePage`** (?C7) until step 10. Same for
   `IControlObject<>` and `ICoreControlObject<>`. That is the only place with a both-recognition
   path, and it goes with the delete.

5. **`ITestContext` (non-generic) is unchanged.** `ICoreTestContext` extends it. Every stack's
   test context, old or new, is an `ITestContext`. This is what keeps runtime services
   (timeouts, logger, navigation, screenshots, reset) working across the transition.

## Type collisions that do not happen, and why

| Old type | New type | Why they do not collide |
| --- | --- | --- |
| `IElement<TSelf>` | `ICoreElement` | Different names, different namespaces (`Brinell.Core.Interfaces` vs. `Brinell.Core.Automation`) |
| `IElementScope`, `IElementScope<TElement>` | `ICoreScope`, `ICoreScope<TSelf>` | Different names |
| `IPageObject`, `IPageObject<TElement>` | `ICorePage`, `ICorePage<TSelf>` | Different names |
| `IDriver<TElement>` | `ICoreDriver` | Different names |
| `IContainerControl<TElement>` | `ICoreContainer` | Different names |
| `ITestContext<TElement>` | `ICoreTestContext` | Different names; both extend the shared `ITestContext` |
| `PageReadinessSnapshot` | `CoreScopeReadiness` | Different names |
| `PageReadinessState` | `CoreScopeReadinessState` | Different names, and the new one has extra values |
| `BusySignalPolicy` | `CoreBusySignalPolicy` | Different names |
| `PageLoadException` | `CoreScopeNotReadyException` | Different names; both are `BrinellException` subclasses so a broad `catch (BrinellException)` still catches both |

## What a per-stack file looks like mid-migration

A MAUI element file after step 4 is done and before Html has moved:

```csharp
using Brinell.Core.Automation;                 // ICoreElement, ICoreScope, ...
using Brinell.Core.Locators;                   // Locator (unchanged)

namespace Brinell.Maui.Interfaces;

public interface IMauiElement : ICoreElement   // was: IElement<IMauiElement>
{
    MauiPlatform Platform { get; }             // MAUI-only
    ShellChromeLocators ShellChrome { get; }   // MAUI-only
    IMauiElement? TryFindByScrolling(Locator locator);   // MAUI-only
    // Universal members (Click, Text, Visible, ...) are on ICoreElement now
}
```

The same file for WPF, still on the old set (until step 7):

```csharp
using Brinell.Core.Interfaces;                 // IElement<T>, IElementScope<T>, ...

namespace Brinell.Wpf;

public class FlaUIWpfElement : IElement<FlaUIWpfElement>
{
    // Same as today. No change.
}
```

Both compile in the same solution build.

## Build order

1. `Brinell.Core` compiles first, exporting *both* namespaces:
   - `Brinell.Core.Interfaces` (unchanged);
   - `Brinell.Core.Automation` (new).
2. Every per-stack project builds against `Brinell.Core`. A project that has migrated `using`s
   the new namespace; a project that has not `using`s the old one. Both work.
3. If a project has moved partway (some files on old, some on new), both `using` directives
   live in `GlobalUsings.cs` for the transition, and are cleaned up when the project's move is
   finished.

## What breaks the invariants

A single change that would break coexistence and must be caught in review:

- **Editing the old interfaces to inherit from the new ones**, or vice versa. Do not bridge.
  Coexistence works because the two are strangers.
- **Adding a `[Obsolete]` on the old interfaces.** No point; every stack that still uses them
  is a stack that has not migrated yet. The obsolescence is loud; the users are already
  aware.
- **A per-stack interface that inherits *both* the old and the new**
  (`IMauiElement : IElement<IMauiElement>, ICoreElement`). The stack is now on both; the diamond
  makes every method ambiguous. Reject in review; a stack is on one or the other.
- **Extension methods declared on both.** `MauiElementExtensions.FindElement(IMauiElement)`
  and `CoreElementExtensions.FindElement(ICoreElement)` overload to the same call site (because
  `IMauiElement : ICoreElement`). Pick one home; the Core one.

## When it ends

Step 10 of `plan.md`. The deletion order is in `migration.md`, section "Delete". The moment the
last old interface is gone, `Brinell.Core.Interfaces` still exists as a namespace but only for
the untouched types (`ITestContext` non-generic, `IDiagnosticDriver`, `IScreenshotService`, the
generator attributes). At the next commit those move under `Brinell.Core.Automation` or into
their own small namespaces if they were misplaced; that is cleanup, not migration.

## Not in scope

- A per-file `#pragma warning disable` to hide the old interfaces from the new call sites.
  The two sets never appear in the same file after a stack has moved.
- A `Brinell.Core.Compatibility` shim project. If a downstream consumer of the *old* Core
  interfaces needs the *new* ones, they migrate their own code the same way a stack does.
- Publishing a compatibility NuGet package. Brinell is an internal framework; the migration
  span is the source tree's own timeline.
