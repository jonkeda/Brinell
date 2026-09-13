# Brinell.Maui.AppSupport

Windows automation handlers that make MAUI layout and content containers addressable
by `AutomationId`.

**This goes in the app under test, not in the test project.**

## Why it is needed

Stock MAUI layouts and content containers map to WinUI panels that have **no
AutomationPeer**. Their `AutomationId` is invisible to UI Automation — FlaUI,
WinAppDriver, and Appium cannot see them at all.

Without these handlers, a Brinell container object targeting a `Grid` (or any layout)
fails to resolve, and the only symptom is an `ElementNotFoundException` that looks
exactly like a mistyped `AutomationId`. The failure mode is quiet and misleading,
which is why this is worth setting up before writing container tests.

## Two supported ways to use it

Both are first-class. The app under test is not always a project you can add a
reference to — third-party or legacy apps often leave copying as the only option, so
these sources are kept dependency-free to make that work.

### 1. Reference the project

```xml
<ProjectReference Include="path/to/Brinell.Maui.AppSupport.csproj" />
```

```csharp
using Brinell.Maui.AppSupport;

builder.ConfigureMauiHandlers(handlers => handlers.AddBrinellAutomationHandlers());
```

`samples/Brinell.Samples.Maui.App` demonstrates this route.

### 2. Copy the sources

Copy `Handlers/` and `BrinellAutomationSupport.cs` into the app, adjust the
namespaces, and call the same extension method.

Nothing here references `Brinell.Core`, `Brinell.Maui`, or anything else in this
repository — only `Microsoft.Maui.*`. **Keep it that way.** A single Brinell
dependency would break the copy route silently.

## What gets registered

| Registration | Covers |
|---|---|
| `Layout` | `Grid`, `VerticalStackLayout`, `HorizontalStackLayout`, `StackLayout`, `FlexLayout`, `AbsoluteLayout` |
| `ContentView` | `ContentView` and custom views deriving from it |
| `Border` | `Border` |

The `Layout` registration is against the **base** type, so one line covers every
layout subclass.

`ScrollView` needs no handler — it is already addressable.

## What is deliberately not registered

**`SwipeView` and `RefreshView`.** They map to the WinUI `SwipeControl` and
`RefreshContainer`, which already supply their own AutomationPeers. Overriding those
peers does not merely fail to help — it **collapses the entire UIA tree**, making
every element in the app unaddressable while the app keeps rendering normally.

This was measured, not assumed. The attempted handlers are kept, unregistered, in
`samples/Brinell.Samples.Maui.App/Platforms/Windows/Handlers/AutomationRemainingHandlers.cs`
so the experiment is not repeated. To scope inside a `SwipeView` or `RefreshView`,
wrap its *content* in a container that is addressable.

**`Frame`.** Deprecated in MAUI, with no handler to hook. Use `Border`.

## Verifying it works

`samples/Brinell.Samples.Maui.App` has an "Automation Probe" tab that reports
addressability for every layout type. The corresponding test:

```
dotnet test testsnew/Brinell.Maui.UITests --filter "FullyQualifiedName~AutomationProbeTests"
```

It prints a table and is the regression test for any change to these handlers. Note
that it carries a **control group** (`AutomationContainer`): if that entry ever
reports "NO", the probe itself is broken and no other reading on the page can be
trusted. That check is what caught the SwipeView/RefreshView tree collapse.

---

# Gestures and semantic verbs: the UI Automation bridge

The handlers above make an element *findable*. The bridge makes it *drivable* — a test can
swipe, tap, focus or refresh a control without a mouse, without the foreground window, and
without the control being addressable at all.

## Why this is separate from the handlers

A gesture cannot be expressed as a UI Automation pattern from XAML. `AutomationPeer.GetPatternCore`
takes a closed enum, so a custom pattern is not reachable through WinUI at any level. The only
supported route is a native provider returned from `WM_GETOBJECT` on a window you own.

So the bridge creates a 1×1 child window that draws nothing, takes no input, and exists solely to
carry the provider. **It does not touch the app's automation tree**: no handler is replaced and no
peer is overridden, which is exactly the mistake recorded above.

## Using it

One attached property per element. An `AutomationId` is required — it is the only join between
the element a test names and the bridge element that acts for it.

```xml
<ContentView xmlns:uia="clr-namespace:Brinell.Maui.AppSupport.Uia;assembly=Brinell.Maui.AppSupport">

    <SwipeView AutomationId="TestSwipeView"
               uia:GestureAutomation.Verbs="SwipeRight,CloseFlyout" />

    <RefreshView AutomationId="TestRefreshView"
                 uia:GestureAutomation.Verbs="SwipeDown" />

</ContentView>
```

**The xmlns depends on how you took this project, and getting it wrong crashes the app at
runtime with no build warning:**

| How you took AppSupport | xmlns |
|---|---|
| Referenced as a project | `clr-namespace:Brinell.Maui.AppSupport.Uia;assembly=Brinell.Maui.AppSupport` |
| Copied the sources in | `using:Brinell.Maui.AppSupport.Uia` |

`using:` without an assembly means *this* assembly. With a project reference it resolves to
nothing, XAML compilation reports no warning, and the page throws `XamlParseException` the first
time it is shown — which reaches a test as every element on the page disappearing at once.

And one line during startup, before the first page loads:

```csharp
builder.UseBrinellGestureBridge();
```

Write it unconditionally. It is a no-op unless this build has a bridge *and* the launcher asked
for one — see [Shipping](#shipping) — so the same line is correct in a release build, where it
does nothing, as under test. Put it in `CreateMauiApp`: elements publish themselves on `Loaded`,
and one that loads before this runs finds the bridge off and stays unpublished.

Nothing else is needed. No registration: the first element that declares a verb creates the
bridge, and an app with no declarations behaves exactly as if these files were absent.

## From a test

```csharp
driver.PerformGesture("TestRefreshView", MauiGesture.SwipeDown);   // throws if refused
driver.TryPerformGesture("TestSwipeView", MauiGesture.SwipeRight); // returns false if refused
driver.SupportsGesture("TestSwipeView", MauiGesture.Pinch);        // asks first
```

Addressed by `AutomationId` rather than by element, because the controls that most need a gesture
are the ones Windows automation cannot see.

## When a gesture is not found

```
$env:BRINELL_UIA_LOG = "bridge.log"      # every publish, registration and verb, with its HRESULT
$env:BRINELL_APP_CRASH_LOG = "crash.log" # unhandled exceptions in the app under test
```

and from a test, `FlaUIMauiDriver.DescribeGestureBridge()` prints the raw tree below the app
window. Between them they separate the three failures that otherwise look identical: no bridge
window (the app was built without these sources), a bridge window whose provider never answered
`WM_GETOBJECT`, or a fragment root with nothing registered on it.

## What it copies

Copying this project into an app means copying `srcnew/Brinell.Uia.Contracts` alongside it and
adjusting the `Compile Include` path in the csproj. Both ends of the bridge — the app and the
test assembly — must compile the same GUIDs, verb numbers and method table, or they do not speak
to each other.

## Shipping

**The bridge is a remotely invocable command channel into application logic.** UI Automation has
no per-caller authentication: any process at the same or higher integrity level on the same
desktop can enumerate the tree, find the fragment root and call the pattern. There is no
permission to check and no caller to identify, so the only control that holds is that a shipping
build has nothing to find.

Two gates, and only the first one controls anything.

| | What it does | How to set it |
|---|---|---|
| **Compile time** | Decides whether the provider is in the build at all | `BRINELL_UIA_BRIDGE` defined — on in `Debug`, off otherwise, `-p:BrinellUiaBridge=true\|false` to override |
| **Run time** | Decides whether an instrumented build turns it on | `BRINELL_UIA_BRIDGE=1` in the app's environment, which the test driver sets on the app it launches |

The second exists so one build can serve development and testing. It protects nobody on its own —
anything that can set an environment variable on the app could have launched a different build of
it — and it is in the tree here because a gate that never says no looks exactly like a gate that
works.

**What a Release build actually contains.** Measured, not asserted:

| | Debug | Release |
|---|---|---|
| `BridgeFragmentRoot`, `BridgeTargetProvider`, `BrinellUiaBridge` | present | **absent** |
| `MauiVerbDispatcher`, `MauiCapabilities`, the verb bindings | present | **absent** |
| `GestureAutomation` and its attached property | present | present |

The declaration survives because shared XAML names it and the app would not compile without it.
It reads a property nothing acts on. Everything behind it is removed from the build by
`Brinell.Maui.AppSupport.csproj` rather than wrapped in `#if` — the same choice the `Handlers`
folder makes, and for the same reason: these are files people read and copy. One file,
`BrinellBridgeHost.cs`, carries the `#if`, because it is the seam `GestureAutomation` calls into.

**Copying the sources in does not copy the gate.** `BRINELL_UIA_BRIDGE` comes from
`Brinell.Uia.Bridge.props`, which the csproj imports. An app that copies these files in and
imports nothing gets no bridge at all until it defines the constant itself — which is the
direction a gate should fail in, and worth knowing before spending an afternoon on an app that
publishes nothing.

`Brinell.Uia.Tests.BridgeGatingTests` measures both gates from outside: it builds the bridge's
host in Release, launches it *asking* for the bridge, and fails if a fragment root appears.
