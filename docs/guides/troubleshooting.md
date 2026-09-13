# Troubleshooting

Start with diagnostics. Do not add sleeps or longer timeouts as the first fix.

## Build Fails

Run:

```powershell
dotnet build srcnew\Brinell.sln -v:minimal /nr:false
```

If the top-level `Brinell.sln` fails during restore, check whether the failure
comes from a tool project or package vulnerability warning treated as an error.

## Test Cannot Find Element

Check:

- automation ID or locator is correct;
- page object waits for the right loaded state;
- control is visible/enabled before action;
- app navigation completed;
- driver page source or automation tree contains the element.

## Flaky UI Test

Before changing code:

1. Inspect screenshots.
2. Inspect runner output.
3. Inspect framework logs and app logs.
4. Check whether the failure is a stale element, wrong page, blocked UI thread,
   missing readiness signal, or real app bug.

Fix by waiting for a real state change, not by sleeping.

## Appium Problems

Check:

- `APPIUM_SERVER_URI`;
- Appium server is running;
- platform driver is installed;
- app path or package is valid;
- emulator/device is connected;
- platform value matches `windows`, `android`, or `ios`.

## Playwright Problems

Install browsers from the test project's output if needed:

```powershell
pwsh bin\Debug\net10.0\playwright.ps1 install
```

## Physical Input Problems

MAUI runs on Windows refuse real mouse, keyboard, clipboard and foreground use by
default, so a run does not take the machine. A test that falls back to real input
fails with `PhysicalInputRefusedException`, naming the call site and the semantic
route that should replace it.

- **Fix the route, not the policy.** The exception says which verb or pattern to use.
  If the control has no semantic route, see
  [AD-008](../architecture/decisions.md#ad-008-gestures-and-semantic-actions-go-through-ui-automation).
- **A test that is about real input** should say so with `[PhysicalInputFact]`, which
  skips it while input is refused.
- **To allow real input for a run** - to watch it, or to exercise the fallback path:

```powershell
$env:BRINELL_BACKGROUND_MODE = "0"
```

- **To list every physical-input use without refusing any:**

```powershell
$env:BRINELL_BACKGROUND_MODE = "audit"
$env:BRINELL_PHYSICAL_INPUT_LOG = "physical-input.log"
```

`BRINELL_ALLOW_POINTER_INPUT` no longer does anything, and has not for some time.

## Gesture Bridge Problems

"No element published on the app's bridge answers ..." has several causes that read
identically from a test:

- **The app was built without the bridge.** It is compiled in only in Debug, or with
  `-p:BrinellUiaBridge=true`. A Release build contains no bridge at all, by design.
- **The app never turned it on.** Every app under test must call
  `builder.UseBrinellGestureBridge()` in `MauiProgram`, before its first page loads.
- **The element never declared the verb**, or has no `AutomationId`.
- **The page had not published yet.** Pages publish on `Loaded`, after they appear in
  the tree; the driver waits through that, so a persistent failure is one of the above.

Set `BRINELL_UIA_LOG` to a file and rerun: the app records whether the bridge is on,
what it published, and every call it answered. `FlaUIMauiDriver.DescribeGestureBridge()`
prints the raw tree under the window.

**If every test after some point fails in milliseconds,** check the driver's root
element before the app: UI Automation can retire a cached element for a live window.
The driver now re-attaches (`FlaUIMauiDriver.RootReattachments` counts it); a count
that keeps growing means something is invalidating it again.
