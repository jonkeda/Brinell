# Comments on the manual test plan for Visual Studio

Answers to two questions on [visual-studio-test-plan.md](visual-studio-test-plan.md).
Date: 2026-09-28.

## 1. Do the UI test runs need the desktop, mouse and keyboard?

**No. The plan is wrong there.** I wrote it out of habit from other UI test tools, not from how
Bruinell works. The runs in the plan (sections 5 and 6) are the Todo **MAUI** suite on Windows,
through `Bruinell.Maui.FlaUI`, and that driver was built so a person can keep working while it
runs:

| What the plan feared | What happens |
| --- | --- |
| The run moves the mouse or types | It never does. Every action is a UI Automation pattern or a gesture bridge verb, and nothing else is possible: there is no code for pointer, keyboard, clipboard or taking the foreground, and no switch to turn it on ([AD-005](../../../.docs/decisions/ad-005-physical-input-is-opt-in.md), [AD-008](../../../.docs/decisions/ad-008-gestures-and-semantic-actions-go-through-ui-automation.md)). |
| The app needs to be in front | It is kept behind. `QuietWindow` records the window you were using when the app starts, and a watchdog puts your window back if the app takes the foreground (`srcnew/Bruinell.Maui.FlaUI/Windowing/QuietWindow.cs`; the `Stage=Background` tests check it). |
| A failure screenshot needs the app visible (R-7) | It doesn't. `WindowCapture` asks the window to render itself (`PrintWindow`), so it captures the app's own content while it is behind other windows. |

So you can keep typing in Visual Studio, or anywhere else, while a run goes. What is left:

- **Don't click inside the Todo app's window.** Your input wouldn't break the driver, but it
  would change the app's state under the scenario, and the scenario would then fail for a reason
  of your making.
- **The app's window does appear** (on the taskbar, behind your window). That is expected, not a
  sign that it wants focus.
- **This holds for MAUI only.** The WPF and WinForms suites still fall back to real input unless
  `BRUINELL_BACKGROUND_MODE` is set (AD-005). The web suite runs its browser through Playwright,
  without your mouse or keyboard. The plan runs neither of them.
- **When the debugger stops** (section 6), Visual Studio comes to the front, as usual. That is the
  debugger, not the test run.

**Changes to make in the plan:**
- Section 1: remove "Close every other Visual Studio window..." and "they need the desktop to
  themselves: don't use the mouse or keyboard while a run goes". Replace them with: "You can keep
  working while a run goes; only don't click inside the Todo app's window."
- Section 5, intro: the same.
- R-1: "the Todo app opens" becomes "the Todo app starts (behind your window)".
- Add a check to section 5: during R-1, keep typing in the editor. The run passes, your window
  keeps the focus, and the app never comes to the front. It tests the claim above in Visual
  Studio's setting, where the test host is started by the extension.

## 2. Does Visual Studio 2026 Insiders matter for the setup?

**Not for the setup itself; your machine is ready.** What I checked on this machine:

| | Found | Needed |
| --- | --- | --- |
| Visual Studio | Professional 2026 Insiders, 18.11 (channel `VisualStudio.18.Preview`), the only install | The VSIX installs on 17.10 up to, but not including, 19.0 (`source.extension.vsixmanifest`). 18.11 is in range, and Professional is covered by the `Community` target. |
| Workload | Visual Studio extension development, installed; the VSSDK build targets are there | The same |
| .NET SDK | 10.0.401 (and older ones) | The repository's `global.json` asks for 10.0.100 with `latestFeature`, so it takes 10.0.401. The `rc.1` SDK on the machine is not picked (`samples/global.json` refuses prereleases). |
| F5 | `StartProgram` is `$(DevEnvDir)devenv.exe /rootsuffix Exp` | That is the Visual Studio that builds it, so F5 starts an Insiders experimental instance. Its settings live in `%LOCALAPPDATA%\Microsoft\VisualStudio\18.0_5e1d64edExp`, created on the first F5. |

The VSIX was built with this Insiders install during the work, so the build side is known to work.
What is not known is how the extension behaves inside it; that is what the plan checks.

Where Insiders can make a difference:

- **It changes under you.** Insiders updates often. If a check fails, note the exact version
  (Help > About) in the results, and try it again after the next update before treating it as
  the extension's fault. A failure there may also be a failure in the next release, so it is
  still worth reporting.
- **Theme names (P-11).** The plan names Dark, Light and Blue. Use whichever themes your
  **Tools > Theme** lists; what matters is a dark one, a light one, and switching without a
  reload.
- **Menu places.** Some items the plan names by their menu path (Document Outline in E-1, the
  Test Explorer and Error List paths) may have moved in 2026. If one isn't where the plan says,
  use **Feature Search** (Ctrl+Q), and note where it was.
- **Copilot (section 12).** "17.14 or later" is met. The MCP settings in 2026 may look different
  from the guide's; note what you had to do.
- **The results line** in section 14 asks for the Visual Studio version: write
  "2026 Insiders 18.11.x" in full, since this is the first run on any Visual Studio.

Nothing in the setup steps (S-1 to S-4) needs to change for Insiders.
