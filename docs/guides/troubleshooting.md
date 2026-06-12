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

## Pointer Input Problems

Routine actions should not require pointer input. For explicit gesture-only
surfaces, set:

```powershell
$env:BRINELL_ALLOW_POINTER_INPUT = "true"
```

Leave it unset for normal runs.
