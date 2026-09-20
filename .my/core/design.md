# Common Core layer: design

**Proposed** design. Not accepted; `plan.md` is a draft. The rules R0-R9 come from
`../stale-readiness/design.md` unchanged. This document is the *shape*, not the *rules*.

Where a shape here is identical to MAUI's, this document says "same as
`../stale-readiness/design.md` section N" and only names the delta.

## 1. Goal

One set of interfaces, one calls layer, one readiness model, one exception set - reused by
every stack. Per-stack interfaces (`IMauiElement`, `IHtmlElement`, ...) become **thin
extensions** that only add what that stack uniquely does.

## 2. Rules

Same as `../stale-readiness/design.md` section 2. R0-R9 apply verbatim.

## 3. The layers

```text
Brinell.Core                (namespace: Brinell.Core.Automation - ?C1)
    Interfaces              ICoreElement · ICoreDriver · ICoreScope · ICorePage
                            ICoreTestContext · ICoreContainer · ICoreCollection<TItem>
                            ICoreItem<TCollection>
    Capabilities            ICoreClickable<TScope> · ICoreEditableText<TScope>
                            ICoreToggle<TScope> · ICoreRange<TScope> · ...     (?C2)
    Control                 ICoreControlObject<TScope> · ICoreElementObject<TScope>
    Exceptions              CoreStaleElementException · CoreElementNotReadyException
                            CoreScopeNotReadyException · CoreAppUnavailableException
    Calls                   ControlCall · Poller · Deadline · AttemptContext
                            Observation · ObservationLog · Confirmer · Confirmation<T>
    Scopes                  CoreScopeReadiness · CoreScopeReadinessState
                            CoreBusySignalPolicy · CoreAppRoot · CoreRootedScopeBase
                            CoreContainerObjectBase · CoreCollectionObjectBase
                            CoreItemObjectBase · CorePageObjectBase
    Controls                CoreViewBase<TScope> · Run* helpers · Confirm
    Helpers                 CoreElementExtensions · CoreScopeExtensions

Brinell.Maui                (namespace: Brinell.Maui.Interfaces, unchanged)
    IMauiElement : ICoreElement           adds Platform, ShellChrome, TryFindByScrolling,
                                          bridge verbs, gesture verbs
    IMauiDriver : ICoreDriver             adds MauiPlatform, ShellChromeLocators, ResetAppState
    IMauiElementScope : ICoreScope        adds ScrollingRoot, AllowsScrollLookup
    IMauiPage : ICorePage                 (empty extension for typing)
    IMauiTestContext : ICoreTestContext   adds MauiPlatform, AppElement, ShellChrome

Brinell.Html                (namespace: Brinell.Html.Interfaces)
    IHtmlElement : ICoreElement           adds Fill, SelectOption, Check/Uncheck, Focus/Blur,
                                          Evaluate, GetDom*, InnerHtml, OuterHtml
    IHtmlDriver : ICoreDriver             adds CurrentUrl, PageTitle, IsIdle, GoBack/GoForward
    IHtmlElementScope : ICoreScope        (empty extension for typing)
    IHtmlPage : ICorePage                 adds Url
    IHtmlTestContext : ICoreTestContext   adds CurrentUrl, PageTitle, frame accessors

Brinell.Wpf / .WinForms / .NativeAndroid / .Stride
    Same pattern: I<Stack>Element : ICoreElement, and so on, adding only what is stack-unique
```

**Rule of thumb.** If a member belongs on every stack (`Visible`, `Enabled`, `Text`, `Click`,
`SendKeys`), it lives on `ICore*`. If it belongs only to one stack (`Platform`, `ShellChrome`,
`Fill`, `SelectOption`), it lives on the per-stack extension. If two stacks want the same
member (`Hover`, `Rect`, `ScrollIntoView`), it lives on `ICore*` - two is enough.

Every public member of a scope or control still follows the MAUI flow
(`../stale-readiness/design.md` section 3): `ControlCall.Run` → `Poller.Until` →
{scope, locate, ready, body}. Nothing changes in the flow; only the namespaces are new.

## 4. `ICore*` interfaces

Signatures modelled on MAUI's `../stale-readiness/design.md` 4.2. Only the names differ.

```csharp
namespace Brinell.Core.Automation;

/// A UI element, shape shared by every stack.
public interface ICoreElement
{
    // identity
    string InstanceKey { get; }
    string? AutomationId { get; }
    string? Name { get; }

    // state
    bool Visible { get; }
    bool Enabled { get; }
    bool Selected { get; }
    string? Text { get; }
    string? TagName { get; }

    // geometry
    System.Drawing.Point Location { get; }
    System.Drawing.Size Size { get; }
    System.Drawing.Rectangle Rect { get; }

    // interaction (universal actions)
    void Click();
    void DoubleClick();
    void RightClick();
    void Hover();
    void LongPress(int durationMs = 1000);
    void SendKeys(string text, TextInputMethod method = TextInputMethod.Keys);
    void Clear();
    void ScrollIntoView();                  // no default timeout: takes CallRemainingMs
    void Swipe(int startX, int startY, int endX, int endY, int durationMs = 500);

    string? GetAttribute(string name);

    // lookup: one attempt each, no timeouts
    ICoreElement? TryFindElement(Locator locator);
    IReadOnlyList<ICoreElement> FindElements(Locator locator);
}

/// The driver. Shape shared by every stack.
public interface ICoreDriver : IDiagnosticDriver, IDisposable
{
    ICoreElement AppRoot { get; }                                   // the root element of the app
    IReadOnlyList<ICoreElement> FindElements(Locator locator);      // one attempt, from AppRoot
    byte[] TakeScreenshot();
    void SaveScreenshot(string path);
    void ResetAppState();
}

/// Anything controls can be declared in.
public interface ICoreScope
{
    ICoreTestContext Context { get; }
    LocatorStrategy DefaultLocatorStrategy { get; }
    ICorePage? Page { get; }                                        // for naming; never for gating

    CoreScopeReadiness ProbeReadiness();                            // one attempt
    bool IsReady() => ProbeReadiness().IsReady;
    bool WaitReady(int? timeoutMs = null);                          // a real wait

    ICoreElement? TryFindElement(Locator locator);                  // one attempt
    IReadOnlyList<ICoreElement> FindElements(Locator locator);
    ICoreElement FindElement(Locator locator)
        => TryFindElement(locator) ?? throw DescribeMiss(locator);
    ElementNotFoundException DescribeMiss(Locator locator) => new(locator);
}

/// A scope that knows itself. Optional: used by the fluent-return helpers only.
public interface ICoreScope<TSelf> : ICoreScope where TSelf : ICoreScope<TSelf>
{
    TSelf Self { get; }
}

/// A page.
public interface ICorePage : ICoreScope
{
    string Name { get; }
    bool IsLoaded();
    bool IsBusy();
    bool WaitBusy(bool? expected, int? timeoutMs = null);
    bool WaitLoaded(bool? expected, int? timeoutMs = null);
    void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null);
    string? GetTitle();
    bool WaitTitle(string? expected, int? timeoutMs = null);
    void AssertTitle(string? expected, string? message = null, int? timeoutMs = null);
    void AssertIdle(string? message = null, int? timeoutMs = null);
    void TakeScreenshot(string? filename = null);
}
public interface ICorePage<TSelf> : ICorePage, ICoreScope<TSelf> where TSelf : ICorePage<TSelf>;

/// A container.
public interface ICoreContainer : ICoreScope { }
public interface ICoreContainer<TSelf> : ICoreContainer, ICoreScope<TSelf>
    where TSelf : ICoreContainer<TSelf>;

/// A collection of rows.
public interface ICoreCollection<TItem> : ICoreContainer
    where TItem : ICoreItem<ICoreCollection<TItem>>
{
    int GetItemCount();
    bool IsEmpty();
    TItem Item(int index);
    TItem? TryItem(int index);
}

/// A row of a collection.
public interface ICoreItem<TCollection> : ICoreContainer
    where TCollection : ICoreCollection<ICoreItem<TCollection>>
{
    int Index { get; }
    ItemKey Key { get; }
}

/// The test context.
public interface ICoreTestContext : ITestContext, ICoreScope    // ITestContext: existing, non-generic, unchanged
{
    ICoreDriver Driver { get; }
    ICoreElement AppRoot { get; }
}
```

The `ItemKey` record, `LocatorStrategy` and `Locator` are the Core ones today; they are not
renamed.

**`ICore*` never assumes the platform's element type.** That is the departure from Core's
current `IElementScope<TElement>` and `ITestContext<TElement>`. Removing the type parameter is
what lets every stack share the same scope. If a stack needs the platform element type back on
its own scope, it types its per-stack extension:

```csharp
public interface IHtmlElementScope : ICoreScope
{
    new IHtmlElement? TryFindElement(Locator locator);       // covariant return
    new IReadOnlyList<IHtmlElement> FindElements(Locator locator);
}
```

Whether the `new` shadow lookups are declared on every per-stack scope, or the stack keeps
`ICoreElement` at that boundary and casts inside its own code, is per-stack. It does not affect
the calls layer, which is typed on `ICoreElement`.

## 5. Capabilities

The generator writes public members from Core methods, and the capability interfaces
(`IClickableControlObject<TScope>`, `IEditableTextControlObject<TScope>`,
`IToggleControlObject<TScope>`, `IRangeControlObject<TScope>`, `IScrollableControlObject<TScope>`,
`IExpandableControlObject<TScope>`, `IFocusableControlObject<TScope>`,
`IPressableControl<TScope>`, `IProgressControlObject<TScope>`, `IRefreshableControlObject<TScope>`,
`ISelectorControlObject<TScope>`, `ISwipeableControlObject<TScope>`, `ITabControlObject<TScope>`,
`ITextControlObject<TScope>`, `IDateControlObject<TScope>`, `ITimeControlObject<TScope>`) mark
which contract a control fulfils.

Per **?C2**, they are renamed with the `ICore` prefix:

| Existing | New |
| --- | --- |
| `IClickableControlObject<TScope>` | `ICoreClickable<TScope>` |
| `IEditableTextControlObject<TScope>` | `ICoreEditableText<TScope>` |
| `IToggleControlObject<TScope>` | `ICoreToggle<TScope>` |
| `IRangeControlObject<TScope>` | `ICoreRange<TScope>` |
| `IScrollableControlObject<TScope>` | `ICoreScrollable<TScope>` |
| `IExpandableControlObject<TScope>` | `ICoreExpandable<TScope>` |
| `IFocusableControlObject<TScope>` | `ICoreFocusable<TScope>` |
| `IPressableControl<TScope>` | `ICorePressable<TScope>` |
| `IProgressControlObject<TScope>` | `ICoreProgress<TScope>` |
| `IRefreshableControlObject<TScope>` | `ICoreRefreshable<TScope>` |
| `ISelectorControlObject<TScope>` | `ICoreSelector<TScope>` |
| `ISwipeableControlObject<TScope>` | `ICoreSwipeable<TScope>` |
| `ITabControlObject<TScope>` | `ICoreTab<TScope>` |
| `ITextControlObject<TScope>` | `ICoreText<TScope>` |
| `IDateControlObject<TScope>` | `ICoreDate<TScope>` |
| `ITimeControlObject<TScope>` | `ICoreTime<TScope>` |
| `IControlObject<TScope>` | `ICoreControlObject<TScope>` |
| `IElementObject<TScope>` | `ICoreElementObject<TScope>` |

The members within each interface do not change - only the type name. The generator's
convention-based discovery keeps working (?C2).

## 6. Exceptions

Under `Brinell.Core.Automation.Exceptions`:

```csharp
/// The element a handle referred to is no longer in the UI tree.
public class CoreStaleElementException(Locator? locator, Exception? platformError = null) : BrinellException;

/// Found, but not ready for what the call wants.
public class CoreElementNotReadyException(Locator locator, CoreNotReadyReason reason, string? detail = null) : BrinellException;
public enum CoreNotReadyReason { NotVisible, Disabled, Other }

/// A scope in the chain did not become ready.
public class CoreScopeNotReadyException(CoreScopeReadiness readiness, int budgetMs) : BrinellException;

/// The app under test is gone. Never retried.
public class CoreAppUnavailableException(string detail, Exception? platformError = null) : BrinellException;
```

**`BrinellException`, `ElementNotFoundException`, `AssertionException`, `WaitTimeoutException`
stay in `Brinell.Core.Exceptions`.** They are shared by every stack today and did not need
renaming.

**`PageLoadException`** is deprecated in step 4 (?C8): every stack's `PageObjectBase` throws
`CoreScopeNotReadyException` after it moves, and `PageLoadException` is deleted in step 10 when
no code raises it.

## 7. Scope readiness

`CoreScopeReadiness` and `CoreScopeReadinessState` replace `PageReadinessSnapshot` and
`PageReadinessState`. Same shape as MAUI's `../stale-readiness/design.md` 7.1:

```csharp
namespace Brinell.Core.Automation;

public enum CoreScopeReadinessState
{
    Ready,
    MissingRoot, StaleRoot, NotLoaded, Busy, ContentNotReady, ItemChanged,
    MissingBusySignal, InvalidBusySignal
}

public readonly record struct CoreScopeReadiness(
    string ScopeName, CoreScopeReadinessState State, string? Detail = null, bool RootReacquired = false)
{
    public bool IsReady => State == CoreScopeReadinessState.Ready;
}

public enum CoreBusySignalPolicy { Disabled, Required }
```

The chain from MAUI's 7.2 applies unchanged: `CoreAppRoot` → `CorePageObjectBase` →
`CoreContainerObjectBase` → `CoreCollectionObjectBase` → `CoreItemObjectBase`. Popups and
dialogs set `AsksParent => false`.

## 8. Calls

Copied from MAUI's 6.1-6.5, retyped on `ICoreTestContext`. The types live under
`Brinell.Core.Automation.Calls`:

```csharp
namespace Brinell.Core.Automation.Calls;

internal enum ObservationKind { Done, ScopeNotReady, Missing, NotReady, Stale, ItemChanged, Mismatch, Pending, Failed }
internal readonly record struct Observation(...);
internal sealed class ObservationLog { ... }
internal readonly struct Deadline { ... }
internal sealed class Poller(int pollingIntervalMs) { ... }
internal sealed class AttemptContext(Deadline deadline, int animationMs) { ... }
internal sealed class ControlCall(ICoreTestContext context, string scopeName, string controlId) { ... }
public readonly record struct Confirmation<T>(ConfirmationResult Result, T? LastValue, Exception? LastError);
public enum ConfirmationResult { Confirmed, NotConfirmed, Replaced }
```

The type names themselves keep their MAUI-side names because the namespace disambiguates. A
per-stack call site imports `using Brinell.Core.Automation.Calls;` and uses `ControlCall`,
`Poller` etc. as normal. This is the one place `Core` is not prefixed - the types are internal
implementation, not the public shape.

**Sharing is the point.** MAUI's `Brinell.Maui/Calls/*` is deleted in step 4 and its callers
`using` this namespace instead. Html and the other stacks never grow their own copies.

## 9. Controls: `CoreViewBase<TScope>`

Same shape as MAUI's `../stale-readiness/design.md` section 8, retyped on `ICoreScope<TScope>`
and `ICoreElement`:

```csharp
public abstract partial class CoreViewBase<TScope> : ICoreControlObject<TScope>
    where TScope : ICoreScope<TScope>
{
    protected CoreViewBase(ICoreScope<TScope> scope, Locator locator);
    protected Locator Locator { get; }
    protected ICoreScope<TScope> CoreScope { get; }

    protected virtual ICoreElement? TryFindElement() => CoreScope.TryFindElement(Locator);
    protected virtual ElementNotFoundException NotFound() => CoreScope.DescribeMiss(Locator);
    protected ICoreElement FindElement();

    protected virtual bool RequiresVisibilityForAction => true;
    protected virtual void EnsureReadyForActionCore(ICoreElement e);       // throws CoreElementNotReadyException

    // Run* helpers: same names and signatures as today (R8)
    protected T? RunGetWithElement<T>(...);
    protected bool RunWaitWithElement<T>(...);
    protected TScope RunAssertWithElement<T>(...);
    protected TScope RunDoWithElement(...);
    protected TScope RunSetWithElement<T>(...);

    protected Confirmation<T> Confirm<T>(Func<T?> read, Func<T?, bool> done, int? timeoutMs);
}
```

Per-stack view bases become empty extensions when their behaviour is identical:

```csharp
public abstract class MauiViewBase<TScope> : CoreViewBase<TScope>
    where TScope : IMauiScope<TScope>
{
    // only MAUI-specific overrides, e.g. bridge-verb helpers
}
```

If nothing MAUI-specific remains, the stack drops its base class and inherits `CoreViewBase`
directly. Same for Html, WPF, WinForms.

## 10. What changes for whom

| Audience | Change |
| --- | --- |
| **Generated code** | None (R8). |
| **Control authors** | Base class name changes (`ViewBase<TScope>` -> `CoreViewBase<TScope>`, or a per-stack alias). Everything else is a namespace change. `ICoreElement` instead of `IMauiElement` in Core methods when the stack chooses not to type on its own element. |
| **Container authors** | `ProbeContentReadiness` returns `CoreScopeReadiness`. Same override points as MAUI. |
| **Page authors** | Inherit `CorePageObjectBase<TSelf>` or a per-stack subclass. `IsLoaded()` still no timeout. |
| **Test authors** | Type names change: `IMauiTestContext` still works via inheritance, but `ICoreTestContext` is the shared type. Assertion exceptions are `CoreScopeNotReadyException`, `CoreElementNotReadyException`, `CoreStaleElementException`, `CoreAppUnavailableException` in the caught set. |
| **Driver authors** | Implement `ICoreElement` + `ICoreDriver` on the platform types, plus the per-stack extensions. |
| **Skills** | Every `.github/skills/<stack>-*` skill's references switch from `I<Stack>Element` to `ICoreElement` as the element type, and from `I<Stack>Scope` to `ICoreScope<TScope>` as the scope type. Where a per-stack member is what matters, the skill points at the per-stack extension. |
| **Users of the old Core interfaces** | Unaffected until step 10. `IElement<T>`, `IElementScope<T>`, `IPageObject<T>`, `ITestContext<T>`, `IDriver<T>` stay on disk while stacks migrate. |

## 11. Open decisions

The nine `?C1`-`?C9` in `plan.md` section 3. Step 0 decides `?C1` (namespace vs. project) and
`?C2` (capability prefixes). The others each pin a small design choice; the default given is
the working assumption.
