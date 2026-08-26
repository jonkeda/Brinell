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
