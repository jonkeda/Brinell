# Native Android EnsureAuthenticated Speed Research

Date: 2026-06-05

Status: research only. No implementation changes are proposed here as already
complete.

Scope:

- Project: `Exact.Construction.NativeAndroid.UITests`
- Main subject: `ConstructionNativeAndroidFixture.EnsureAuthenticated()`
- Related browser auth helper: `AndroidBrowserCredentialAuthAutomation`
- Goal: reduce repeated native Android UAT runtime without making auth flaky or
  weakening shell verification.

## Summary

`EnsureAuthenticated()` is expensive for two reasons:

1. Tests currently create and dispose a new `ConstructionNativeAndroidFixture`
   in every test method, so each test usually creates a fresh Appium session and
   pays auth startup again.
2. Several auth checks are written as safe sequential waits. That is stable, but
   absent states can cost seconds each: intro detection, EULA detection, CDP
   stale-target scoring, organization settle waits, and post-action browser idle
   loops.

The largest likely win is fixture reuse for the native Android test class or
collection. The second largest win is replacing repeated per-selector waits in
browser state classification with a single DOM snapshot/classifier, especially
after organization selection where the current code can spend about 20 seconds
classifying `Unknown` before returning to native callback wait.

## Current Shape

Each test creates its own fixture:

- `NativeAndroidLoginSmokeTests` uses `using var fixture =
  ConstructionNativeAndroidFixture.CreateFromConfig()` in each test.
- `ContactsBasicUatTests` does the same in all four P0 tests.

The fixture has an `authenticationChecked` fast path, but that flag is instance
local. Because every test method creates a new fixture, the flag does not help
across tests.

Current fixture auth flow:

```text
Skip intro if visible
Wait up to 1s for shell
If this same fixture already checked auth, do two 1s probes for intro/login
Run auth automation
```

Relevant code:

- `ConstructionNativeAndroidFixture.cs:64`
- `ConstructionNativeAndroidFixture.cs:66`
- `ConstructionNativeAndroidFixture.cs:71`
- `ConstructionNativeAndroidFixture.cs:76`
- `ConstructionNativeAndroidFixture.cs:87`

## Observed Timings

Recent successful auth logs under:

```text
MauiMobile/MAUI-Construction/Exact.Construction.NativeAndroid.UITests/bin/Debug/net10.0/artifacts/native-android-auth
```

### Full Username/Password Login

Latest full-login success, approximate offsets from `EnsureAuthenticated
started`:

| Offset | Event |
| ---: | --- |
| 0.0s | EnsureAuthenticated started |
| 2.2s | Native login page detected |
| 5.0s | Native DTAP environment selected |
| 15.5s | CDP endpoint reachable |
| 23.4s | CDP target selected, password form |
| 29.7s | Password submit clicked |
| 36.1s | DOM submit fallback used |
| 47.8s | Organization chooser detected |
| 57.8s | Organization continue absent |
| 71.5s | Browser state became Unknown |
| 92.6s | Browser helper returned to native callback wait |

Interpretation:

- About 10s was Chrome/CDP startup after tapping login.
- About 6s was password submit/fallback delay.
- About 11s was waiting for the organization chooser after password submit.
- About 21s was post-org `Unknown` browser idle classification before native
  callback wait.

### Cached Web Session / Organization Chooser

Recent cached-org successes:

| Offset | Event |
| ---: | --- |
| 0.0s | EnsureAuthenticated started |
| 2.3s-2.4s | Native login page detected |
| 5.1s-5.4s | Native login clicked |
| 18.5s-20.5s | CDP target selected, organization chooser |
| 23.3s-25.4s | Organization clicked |
| 28.0s-30.1s | Browser page closed, callback wait begins |

Interpretation:

- Cached browser login is much faster than full login, but still spends about
  13s-15s after tapping login before selecting the CDP target.
- Per-selector target scoring and stale tabs likely contribute to that delay,
  though some of it may be real page/browser startup.

## Slow Spots

### 1. Per-Test Fixture Creation

Every Contacts P0 test creates a new fixture and calls `EnsureAuthenticated()`.
For a four-test class, the auth path may run four times.

Current pattern:

```csharp
using var fixture = ConstructionNativeAndroidFixture.CreateFromConfig();
fixture.EnsureAuthenticated();
```

Impact:

- Largest repeated cost.
- `authenticationChecked` does not help across tests.
- Appium session creation/disposal also repeats.

Recommended direction:

- Use xUnit `IClassFixture<ConstructionNativeAndroidFixture>` for
  `ContactsBasicUatTests`, or a native Android collection fixture if tests
  across classes should share one driver.
- Put native Android UI tests in a non-parallel collection.
- Add a `ResetToShell()` / `ReturnToShell()` helper so each test starts from a
  predictable authenticated shell without recreating the session.
- Keep one login smoke that validates clean auth startup separately.

Expected impact:

- For Contacts P0, pay auth once per class instead of once per test.
- Recent 4-test Contacts runs took about 6 minutes. Fixture reuse should remove
  several repeated auth/session starts and is the best first optimization.

### 2. Intro Detection Can Cost About Two Seconds When Intro Is Absent

`EnsureAuthenticated()` always calls `IntroductionPage.SkipIfVisible()`.

`NativeIntroductionPage.IsLoaded(timeoutMs)` checks activity first, then tries
the skip button, then the done button. If the current activity is not the intro
activity and neither button exists, `SkipIfVisible(1000)` can do two sequential
1s element waits.

Relevant code:

- `ConstructionNativeAndroidFixture.cs:66`
- `NativeIntroductionPage.cs:21`
- `NativeIntroductionPage.cs:33`

Recommended direction:

- Read current package/activity once at the top of `EnsureAuthenticated()`.
- Only run long intro button detection when the activity is
  `IntroductionActivity`.
- Otherwise use a zero/short probe for skip/done, or skip intro probing
  entirely if current package/activity clearly says login or shell.

Expected impact:

- Save about 1s-2s per auth attempt when the intro screen is absent.

### 3. DTAP Selection Costs About Three Seconds Per Auth

Recent logs show about 2.7s-2.9s between native login page detection and DTAP
environment selected.

Relevant code:

- `NativeLoginPage.SelectDtapEnvironment()`
- `NativeLoginPage.IsEnvironmentSelected()`

Recommended direction:

- Keep correctness, but avoid repeated work:
  - Cache selected DTAP in the fixture after it is verified for the current
    process/session.
  - Use a more direct spinner selected-value locator if available.
  - If fixture reuse is adopted, this cost mostly happens once.

Expected impact:

- Save about 2s-3s per auth after the first selection in a shared fixture.

### 4. CDP Target Discovery Does Expensive Per-Selector Scoring

`WaitForAuthPageAsync()` repeatedly calls `FindBestAuthPageCandidateAsync()`.
Each candidate is scored by `ClassifyBrowserStateAsync()`. Classification uses
selector lists and 250ms waits per selector/frame.

Relevant code:

- `AndroidBrowserCredentialAuthAutomation.cs:134`
- `AndroidBrowserCredentialAuthAutomation.cs:158`
- `AndroidBrowserCredentialAuthAutomation.cs:189`
- `AndroidBrowserCredentialAuthAutomation.cs:588`

This is safe but expensive with multiple stale Chrome tabs or pages that are
not ready. Cached-org logs show about 13s-15s from login click to CDP target
selection.

Recommended direction:

- Before tapping native Login, snapshot current CDP target ids/URLs.
- After tapping Login, prefer:
  1. a new target,
  2. a target whose URL changed,
  3. the visible Chrome URL target,
  4. only then older actionable targets.
- Replace per-selector Playwright waits with one DOM classifier per frame:
  - Gather visible inputs/buttons/options/text once via `EvaluateAsync`.
  - Match selectors/text in memory.
  - Use very short retries around the whole snapshot, not 250ms per selector.
- Optionally ignore or close stale login tabs only when test config allows it.

Expected impact:

- Save several seconds in cached-org mode.
- Save much more when Chrome contains stale login tabs.

### 5. Browser Unknown Idle Loop Can Burn About 20 Seconds

After organization selection in the full-login run, the log showed:

```text
71.5s Browser auth state: Unknown
92.6s No additional browser auth actions detected; continuing native callback wait
```

The loop only needs three idle classifications, but each `Unknown`
classification is expensive because it scans organization, username, password,
entry, and post-submit selectors.

Relevant code:

- `AndroidBrowserCredentialAuthAutomation.cs:458`
- `AndroidBrowserCredentialAuthAutomation.cs:572`
- `AndroidBrowserCredentialAuthAutomation.cs:580`
- `AndroidBrowserCredentialAuthAutomation.cs:588`

Recommended direction:

- After browser action completed, treat these as immediate handoff signals:
  - page closed,
  - current Android package is the app package,
  - native sync/EULA/shell is visible,
  - URL navigated away from known auth pages and no actionable auth controls are
    visible in a fast DOM snapshot.
- Reduce `idleCount` from 3 to 1 after an organization click when the URL is
  no longer actionable or the native app is foreground.
- Make post-action classification use the fast DOM snapshot above.

Expected impact:

- Save up to about 20 seconds in the full-login path observed in the latest
  successful smoke.

### 6. Organization Selection Waits For A Full Settle Window

After organization click, `WaitForOrganizationChooserToSettleAsync()` waits up
to 2.5s while the chooser remains visible. The flow then probes optional
continue selectors.

Relevant code:

- `AndroidBrowserCredentialAuthAutomation.cs:960`
- `AndroidBrowserCredentialAuthAutomation.cs:1160`
- `AndroidBrowserCredentialAuthAutomation.cs:1191`

Recommended direction:

- Race several outcomes instead of waiting only for chooser invisibility:
  - chooser gone,
  - page closed,
  - app package foreground,
  - sync/shell visible,
  - continue button visible.
- After organization selection, do not run a full post-submit selector sweep if
  the browser is already closing or native foreground is visible.

Expected impact:

- Save about 2s-5s in cached-org and full-login paths.

### 7. Password Submit Falls Back After A Noticeable Delay

In the full-login run:

```text
29.7s Clicked password submit
36.1s Submitted password form using DOM fallback
```

Relevant code:

- `AndroidBrowserCredentialAuthAutomation.cs:648`
- `AndroidBrowserCredentialAuthAutomation.cs:663`

Recommended direction:

- Use a non-navigation-waiting click when possible.
- Consider using DOM `requestSubmit()` first for this known login form, then
  click fallback only if submit did not fire.
- Check for Chrome saved-password sheet during the submit wait at a shorter
  interval.

Expected impact:

- Save several seconds in full-login runs.

### 8. EULA And Permission Gates Are Sequential Absent-State Searches

`WaitThroughPostLoginGates()` always calls sync wait, optional EULA detection,
notification permission detection, then a shell wait.

Relevant code:

- `ConstructionNativeAuthAutomationBase.cs:23`
- `ConstructionNativeAuthAutomationBase.cs:28`
- `ConstructionNativeAuthAutomationBase.cs:32`
- `ConstructionNativeAuthAutomationBase.cs:35`
- `ConstructionNativeAuthAutomationBase.cs:37`

`NativeEulaDialog.AcceptIfVisible()` defaults to 2s and `IsVisible()` tries
three text searches sequentially. If no EULA is present, this can cost up to
about 6 seconds.

Relevant code:

- `NativeEulaDialog.cs:13`
- `NativeEulaDialog.cs:19`

Recommended direction:

- Check shell first in `WaitThroughPostLoginGates()`; if shell is already
  visible, return before sync/EULA/permission probes.
- Cache "EULA already accepted" per fixture/session after the first absence or
  successful accept.
- Replace text-only EULA search with a root/resource-id search if the native
  app exposes one.
- Use a short absent-state EULA probe, for example 250ms, then only escalate to
  2s if an EULA-like root/dialog is detected.
- Cache notification permission handled/absent per fixture/session.

Expected impact:

- Save up to about 6 seconds after auth when no EULA is visible.
- Save smaller repeated permission-dialog checks.

## Recommended Implementation Order

### Phase 1: Measure Before Cutting

Add timing instrumentation around:

- fixture create/Appium session creation,
- `EnsureAuthenticated()` top-level states,
- intro probe,
- shell fast path,
- login page detection,
- DTAP selection,
- CDP endpoint wait,
- CDP target selection,
- browser state transitions,
- organization handling,
- post-login sync/EULA/permission/shell gates.

Write these to Brinell `TestResults/<run-id>/suites/.../logs` instead of only
`bin/Debug/net10.0/artifacts`.

This gives a before/after baseline and prevents accidental "faster because it
skipped work" fixes.

### Phase 2: Share Fixture For Contacts P0

Change Contacts P0 tests to use a shared fixture:

```csharp
public sealed class ContactsBasicUatTests :
    IClassFixture<ConstructionNativeAndroidFixture>
```

Then:

- make `ConstructionNativeAndroidFixture` constructor public or create a
  factory fixture wrapper compatible with xUnit,
- disable parallel execution for native Android UI tests,
- add `ReturnToShell()` or `EnsureShellReady()` before each Contacts test,
- keep the login smoke independent so clean auth still has coverage.

This is the highest leverage change because it removes repeated auth from
multi-test UAT classes.

### Phase 3: Add Fast App-Side State Classification

Introduce a cheap native state snapshot:

```text
package
activity
has shell tab layout
has login button
has intro activity/buttons
```

Use it in `EnsureAuthenticated()` before calling page-object waits. This avoids
sequential waits for states that are impossible from the current activity.

### Phase 4: Fast Browser Classifier And Handoff Detection

Replace selector-by-selector classification with a DOM snapshot classifier.
After organization selection or password submit, race browser and native
handoff signals instead of waiting through repeated `Unknown` classifications.

### Phase 5: Post-Login Gate Short-Circuits

Make `WaitThroughPostLoginGates()` shell-first and cache absent EULA/permission
states per fixture. This should be small and low risk once measurement exists.

## Risks

- Fixture reuse can create test order coupling. Mitigate with a single native
  Android collection, no parallelism, and explicit navigation back to shell.
- Aggressive browser handoff detection can return too early. Mitigate by still
  requiring native shell/sync/EULA gates before declaring auth success.
- Closing stale Chrome tabs can surprise local debugging. Prefer ignoring stale
  tabs first, and make tab cleanup opt-in.
- Reducing EULA/permission waits can miss a slow first-run dialog. Mitigate by
  using a short absent probe only after shell is already visible, or escalating
  when a dialog root appears.

## Success Criteria

Use the same headless emulator and Appium setup as the current green lane.

Baseline commands:

```powershell
.\Install\scripts\run-tests.ps1 -Suite NativeAndroidUi -Filter "FullyQualifiedName~NativeAndroidLoginSmokeTests.Login_AuthenticationCompletes_AppShellIsVisible" -SkipAppBuild -NoStartAppium -DeviceSerial emulator-5554
.\Install\scripts\run-tests.ps1 -Suite NativeAndroidUi -Filter "FullyQualifiedName~ContactsBasicUatTests" -SkipAppBuild -NoStartAppium -DeviceSerial emulator-5554
```

Target outcomes:

- Clean full-login smoke still passes.
- Cached organization chooser smoke still passes.
- Contacts P0 still passes twice in a row.
- Contacts P0 class runtime drops materially by avoiding repeated auth/session
  setup.
- Auth logs show no credential or token values.

## Decision

Do not start by shaving individual 250ms waits. Start by removing repeated auth
work:

1. Add timing instrumentation.
2. Share a fixture/session for Contacts P0.
3. Add fast app-side state classification.
4. Replace browser per-selector polling with one DOM snapshot classifier.
5. Short-circuit browser handoff and post-login gates when native shell is
   already visible.

This keeps the design principle intact: browser automation may finish browser
steps quickly, but native app shell readiness remains the only auth success
signal.
