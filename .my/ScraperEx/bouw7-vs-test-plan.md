# Test plan: the Scraper in Visual Studio, on Bouw7

Status: **ready**, not run. For: the owner, at a Windows machine with Visual Studio (about 45 minutes).
What each step should do is in [docs/guides/scraper.md](../../docs/guides/scraper.md); where this
plan and the guide disagree, note it as a finding. Earlier, broader plan (Blazor sample, VS Code too):
[manual-test-plan.md](manual-test-plan.md).

Tick a task when it passes. When one fails, write **FAIL** and a note under it. Also take a
screenshot, copy the message under the header and the output from Output > "Bruinet BDD", and
log an issue in [issues/](issues/).

## Before you start

- [x] Bouw7 **acceptance** web app URL known: `<fill in URL>`. Never production. Sign-in goes through `login-acc.bouw7.nl`.
- [x] A Bouw7 acc test account. You sign in yourself in the Scraper's browser; the Scraper never types credentials.
- [x] Chromium installed. From the Bruinet root:
  - [x] `dotnet build tests\Bruinet.Html.UITests`
  - [x] `pwsh tests\Bruinet.Html.UITests\bin\Debug\net10.0\playwright.ps1 install chromium`
- [x] Extension running. Either open `tools\Bruinet.Bdd.VisualStudio\Bruinet.Bdd.VisualStudio.slnx` and press **Ctrl+F5**, or install the `.vsix`. In the instance, open `src\Bruinet.sln`.
- [x] `.playwright\node\win32_x64\node.exe` exists under the extension's `Server\` folder.
- [x] Remember: acc holds realistic contacts and projects. Do not commit `Recordings/` or `.scraper-state.json`.

## Open the Scraper

- [x] **P-1** View > Other Windows > **Scraper**.
  It opens in a document tab, themed like Visual Studio, with steps 1–7 on the left.

## 1 Project

- [ ] **P-2** **New project…**: name `Bouw7.UITests`, platform `html`, Start URL the acc URL.
  - [ ] The project is created and added to the solution.
  - [ ] It is picked in the view, and `scraper.config.md` exists.
  - [ ] **Next:** points to Connect.
- [ ] **P-3** Open `scraper.config.md` and add `login-acc.bouw7.nl` under `Aliases`.
  It saves, and the view still shows the project.

## 2 Connect

- [ ] **C-1** **Check the app** before signing in.
  - [ ] It says a sign-in is needed (or that the host is refused) and offers the matching button.
  - [ ] The message **stays** on screen.
- [ ] **C-2** If the host is refused: **Allow this host**.
  The host is added to `Explore hosts`, and the check runs again.
- [ ] **C-3** **Open the browser to sign in**, sign in to Bouw7 acc, and close the browser.
  - [ ] Chromium opens, with no "Driver not found".
  - [ ] **Check the app** now passes, and step 2 gets ✓.
- [ ] **C-4** Close and reopen the Scraper window, then **Check the app** again.
  You are still signed in: the profile kept the session.

## 3 Collect pages

- [ ] **E-1** In the form, set max pages to 15 and `ClickPolicy` to `safe`.
  - [ ] **Undo changes** puts the old values back.
  - [ ] Set them again and **Save**: only the changed fields are written to the config.
- [ ] **E-2** **Explore** (the first time it is a dry run).
  - [ ] It follows links only and lists what it would click.
  - [ ] Nothing in Bouw7 changes.
- [ ] **E-3** **Explore** for real.
  - [ ] The strip shows progress, and pages are captured.
  - [ ] Anything with danger words (delete, send, *verwijderen*, *versturen*…) is skipped.
  - [ ] It stays on the acc hosts.
- [ ] **E-4** Under More, **Record by hand**: open a contact and a project detail page, **Capture**, then **Stop**.
  - [ ] Each page is captured once, and the count goes up.
  - [ ] **Stop** ends the recording.
- [ ] **E-5** Open a captured file under `Recordings/`.
  - [ ] Masked values (IBAN, e-mail and so on) are replaced.
  - [ ] Nothing you typed is recorded.

## 4 Review

- [ ] **R-1** Open a few pages and their captures, then **Compare** two captures of one page.
  - [ ] Outlines and highlights match the pages.
  - [ ] Skipped elements and blocked requests are listed, each with a reason.

## 5 Controls

- [ ] **K-1** Look at **To make**.
  - [ ] Repeating Bouw7 parts (list rows, menus, form fields) appear as patterns.
  - [ ] Tick two and name them.
- [ ] **K-2** **Make 2 controls with the agent**, then **Review** one and **Accept** it.
  - [ ] The prompt reaches Copilot Chat. If it goes to the clipboard instead, a status line says so.
  - [ ] The made control shows how many of the pattern's occurrences each part was found in.
  - [ ] The accept is still there after you reopen the window.

## 6 Page objects

- [ ] **O-1** Tick the contacts overview and a detail page, then **Draft 2 page objects**.
  - [ ] The drafted classes have a scraped header.
  - [ ] The project builds.
- [ ] **O-2** Rename one class in its row.
  - [ ] The class, the draft and your part are renamed.
  - [ ] The project still builds.

## 7 Verify

- [ ] **V-1** **Verify**.
  Each member is found once in the running app, or there is a clear reason per member.

## Errors

- [ ] **X-1** Cut the network (or set a wrong Start URL), then **Check the app**.
  A readable message that stays until the check passes or another step opens.
- [ ] **X-2** Close the Visual Studio instance in the middle of Explore, then reopen it.
  - [ ] No Chromium or `bruinet` process is left running.
  - [ ] The view opens on the step to do next.

## After

- [ ] Delete the trial recordings if they are not needed: `git status`, then `git clean -fd <project>\Recordings`.
- [ ] Write findings in [issues/](issues/), one file per issue.
