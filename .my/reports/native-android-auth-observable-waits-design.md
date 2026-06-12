# Native Android Auth Observable Waits Design

Date: 2026-06-05

Status: design only. No code changes are made by this document.

Related research:

- `Brinell/.my/reports/native-android-ensure-authenticated-speed-research.md`

## Rule

Wait for something, not for time.

In this auth lane, timeouts are allowed only as upper bounds. The code should
not sleep because "the app probably needs a moment." Every wait should name the
state transition it is waiting for and should return as soon as that transition
is observed.

Good:

```text
wait until shell tab layout is visible
wait until Chrome page closes
wait until current package is the app package
wait until password form is gone
wait until organization chooser is gone
wait until sync screen is gone
```

Bad:

```text
sleep 500ms after browser action
wait 2.5s for organization chooser to settle
count three idle loops before returning
probe absent EULA text for several seconds
wait post-submit for a configured delay
```

Polling intervals are fine. Fixed-duration sleeps are not fine unless they are
the polling interval inside a named state wait.

## Design Goal

Make `EnsureAuthenticated()` fast by making it precise.

The fixture should move through known native/browser states and immediately
advance when a valid next state appears. It should not stack broad waits for
states that are impossible from the current screen.

Success remains unchanged:

```text
NativeAppShellPage.IsLoaded() == true
```

Browser auth is not success. Browser auth is only a route to native callback and
native shell readiness.

## Observable State Model

Auth should be driven from observable states.

| State | Observed By | Example Signal |
| --- | --- | --- |
| `NativeIntro` | Appium/native | current activity contains `IntroductionActivity`, or skip/done visible |
| `NativeLogin` | Appium/native | login button visible, login activity/screen visible |
| `NativeShell` | Appium/native | tab layout visible and app package foreground |
| `NativeSync` | Appium/native | sync activity or sync text visible |
| `NativeEula` | Appium/native | EULA dialog/root/accept button visible |
| `NativePermission` | Appium/native | Android permission dialog root visible |
| `ChromeForeground` | Appium/native | current package is `com.android.chrome` |
| `ChromeSavedPasswordPrompt` | Appium/native | `Use saved password?` sheet visible |
| `BrowserAuthTarget` | CDP | current/new Chrome target with actionable auth DOM |
| `BrowserUsername` | CDP | visible username input |
| `BrowserPassword` | CDP | visible password input |
| `BrowserOrganization` | CDP/native fallback | organization prompt/options visible |
| `BrowserPostSubmit` | CDP | consent/continue/allow action visible |
| `BrowserHandoff` | CDP + native | page closed, app foreground, sync/shell/eula visible |

## Transition Wait Primitive

Introduce one reusable auth wait helper:

```csharp
WaitForAny(
    string transitionName,
    TimeSpan timeout,
    params StateProbe[] probes)
```

Where each probe has:

```csharp
public sealed record StateProbe(
    string Name,
    Func<StateProbeResult> Probe);
```

And result is:

```csharp
public sealed record StateProbeResult(
    bool Matched,
    object? Value = null);
```

Rules:

- Every wait has a transition name, for logging and diagnostics.
- Each probe performs a zero-timeout or short-timeout check.
- The loop sleeps only for the polling interval between probe batches.
- The wait returns the first matching probe.
- Timeout failure names the transition and the probes tried.

Example:

```text
WaitForAny(
  "after password submit",
  timeout,
  BrowserOrganization,
  BrowserPostSubmit,
  ChromeSavedPasswordPrompt,
  BrowserHandoff,
  BrowserPasswordError)
```

## Fixture-Level Design

`ConstructionNativeAndroidFixture.EnsureAuthenticated()` should begin with a
cheap native snapshot.

```text
snapshot = ReadNativeAuthSnapshot()

if snapshot.NativeShell:
    authenticationChecked = true
    return

if snapshot.NativeIntro:
    skip intro
    wait for NativeLogin or NativeShell

if snapshot.NativeLogin:
    run configured auth automation
    wait through native gates
    return

if authenticationChecked and snapshot.AppPackageForeground:
    wait briefly for NativeShell or fail with current snapshot

run configured auth automation
```

The snapshot should avoid element searches unless the activity/package indicates
they are plausible. For example, do not spend time searching skip/done buttons
when the current activity is clearly `MainActivity`.

## Browser Auth Design

The browser helper should be a state machine where each action waits for a named
next state.

```text
Open native login
  -> wait for BrowserAuthTarget or NativeShell

BrowserAuthTarget
  -> classify once with DOM snapshot

BrowserUsername
  -> fill username
  -> submit username
  -> wait for BrowserPassword, BrowserOrganization, BrowserPostSubmit,
     ChromeSavedPasswordPrompt, BrowserHandoff

BrowserPassword
  -> fill password
  -> submit password
  -> wait for BrowserOrganization, BrowserPostSubmit,
     ChromeSavedPasswordPrompt, BrowserHandoff, BrowserPasswordError

BrowserOrganization
  -> select organization
  -> wait for BrowserHandoff, BrowserPostSubmit,
     BrowserOrganizationGone, NativeSync, NativeShell

BrowserPostSubmit
  -> click exact action
  -> wait for BrowserHandoff, NativeSync, NativeShell

BrowserHandoff
  -> return to fixture native gate wait
```

There should be no generic "post action sleep." The next-state wait is the
settling mechanism.

## DOM Snapshot Classifier

Current browser classification can be slow because it tests selectors one by
one. Replace it with one frame-level snapshot:

```text
visible inputs
visible buttons
visible links
visible role=option/button elements
body text markers
current URL
document ready state
```

Then classify from the snapshot:

```text
if visible organization prompt/options:
    BrowserOrganization
else if visible password input:
    BrowserPassword
else if visible username input:
    BrowserUsername
else if visible entry action:
    BrowserAuthEntry
else if visible post-submit action:
    BrowserPostSubmit
else if app package foreground or page closed:
    BrowserHandoff
else:
    Unknown
```

The snapshot must redact input values. It should report only selector, type,
visibility, and text labels needed for diagnostics.

## Native Gate Design

After browser handoff, wait for native states as a race, not as a sequence of
absent-state waits.

Current sequence:

```text
wait sync finished
maybe accept EULA
maybe handle permission
wait shell
```

Proposed loop:

```text
WaitForAny("native post-auth gate",
  NativeShell,
  NativeSync,
  NativeEula,
  NativePermission)

if NativeShell:
    return

if NativeSync:
    wait until NativeSync gone or NativeShell/Eula/Permission visible

if NativeEula:
    accept EULA
    continue gate loop

if NativePermission:
    allow/deny permission
    continue gate loop
```

This avoids spending time proving absent EULA or absent permission before
checking whether the shell is already ready.

## Organization Selection Design

Organization selection should not wait for a fixed settle window.

After clicking an organization, wait for any valid next signal:

```text
BrowserOrganizationGone
BrowserPostSubmit
BrowserHandoff
ChromeSavedPasswordPrompt
NativeSync
NativeEula
NativeShell
BrowserOrganizationStillVisibleWithSameOptions
```

Retry once only when the chooser is still visible with the same options and no
handoff/native state appeared.

Optional continue should be handled as a state:

```text
if BrowserOrganizationContinueVisible:
    click continue
    wait for BrowserHandoff or native gate
```

Do not spend 500ms probing continue after every organization selection if the
page is already closed or native app is already foreground.

## Saved Password Prompt Design

The Chrome saved-password sheet is native UI, not browser DOM.

After any username/password submit, include `ChromeSavedPasswordPrompt` in the
next-state race.

Rules:

- If prompt username matches configured username, tap `Sign in`.
- If prompt username does not match, dismiss it.
- After tapping/dismissing, wait for browser/native next states.
- Do not sleep one second after tapping the prompt; wait for the sheet to be
  gone, browser state to change, or native app foreground/shell.

## Timing Diagnostics

Every transition wait should log:

```text
transition name
start timestamp
matched probe
elapsed milliseconds
redacted URL if browser-related
current native package/activity if native-related
```

Example:

```text
auth.wait after-password-submit matched BrowserOrganization in 11732ms
auth.wait after-organization-click matched BrowserHandoff.PageClosed in 438ms
auth.wait native-post-auth-gate matched NativeShell in 2140ms
```

Diagnostics should go to the Brinell artifact layout:

```text
TestResults/<run-id>/suites/Exact.Construction.NativeAndroid.UITests/logs
```

The existing `bin/Debug/net10.0/artifacts/native-android-auth` logs can remain
temporarily, but Brinell `TestResults` should become the primary output.

## Implementation Plan

1. Add an `AuthStateSnapshot` for native package/activity/shell/login/intro
   probes.
2. Add a small `WaitForAny` transition helper for auth, with logging.
3. Convert fixture `EnsureAuthenticated()` to use native snapshots before
   page-object waits.
4. Convert post-login gates to a native state race.
5. Add a browser DOM snapshot classifier.
6. Convert browser username/password/org/post-submit actions to named
   next-state waits.
7. Remove fixed sleeps:
   - `WaitAfterBrowserActionAsync`
   - saved-password prompt `Thread.Sleep`
   - organization native fallback `Thread.Sleep`
   - `WaitForOrganizationChooserToSettleAsync`
   - generic idle-count wait after browser actions
8. Add fixture reuse separately for Contacts class speed. Reuse is still the
   largest suite-level runtime win, but observable waits make the auth logic
   correct and fast even when reuse is not possible.

## Testing Strategy

Run these cases after each phase:

```powershell
.\Install\scripts\run-tests.ps1 -Suite NativeAndroidUi -Filter "FullyQualifiedName~NativeAndroidLoginSmokeTests.Login_AuthenticationCompletes_AppShellIsVisible" -SkipAppBuild -NoStartAppium -DeviceSerial emulator-5554
.\Install\scripts\run-tests.ps1 -Suite NativeAndroidUi -Filter "FullyQualifiedName~ContactsBasicUatTests" -SkipAppBuild -NoStartAppium -DeviceSerial emulator-5554
```

Required scenarios:

- already authenticated app shell
- cached web session that starts at organization chooser
- full username/password login
- Chrome saved-password prompt visible
- stale Chrome login tabs present
- no EULA visible
- EULA visible
- notification permission visible

## Acceptance Criteria

- No auth step waits for a fixed duration except as a polling interval inside a
  named transition wait.
- Every timeout failure says which transition was expected and which probes were
  tried.
- Auth success still requires native shell visibility.
- Contacts P0 still passes twice in a row.
- Logs contain no credential or token values.
- Measured auth time improves for cached-org and full-login paths without
  hiding failures.

## Decision

Use observable-state waits as the auth design rule.

The implementation should not be "sleep less." It should be "wait for the exact
next states." That is both faster and safer: when the app moves quickly, the
test moves immediately; when the app is slow, the test waits for the actual
state it needs instead of guessing how long is enough.
