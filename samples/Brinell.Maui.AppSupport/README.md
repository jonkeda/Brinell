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

Nothing else is needed. No builder call, no registration: the first element that declares a verb
creates the bridge, and an app with no declarations behaves exactly as if these files were absent.

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

These sources have no place in a shipping build. UI Automation has no per-caller authentication,
so the only real control is absence.
