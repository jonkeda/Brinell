# Connect in the VS Scraper window fails: Playwright's node.exe is not found

Status: both fixed 2026-10-02, not yet retested in Visual Studio. Packaging: the VSIX keeps the Server folder layout. Disappearing message: `answer()` in ScraperPage.cs no longer clears the problem on every successful call.

## Steps

1. Open the Scraper tool window in the Visual Studio extension (Brinell BDD).
2. Click **Connect**.
3. An error message shows for a moment and then disappears.
4. It says something about `node.exe` not being there.

## Expected

The browser opens and the Scraper starts recording.

## Cause

Playwright starts its driver from `.playwright\node\win32_x64\node.exe`, looking for it beside
`Microsoft.Playwright.dll`. The extension's `Server\` folder has **no subfolders**, so that path
does not exist.

`dotnet publish` of `tools/Brinell.Cli` lays the folder out correctly:

- `obj/server/.playwright/node/win32_x64/node.exe` is there.
- 287 files in total, in subfolders.

The VSIX flattens it:

- The VSIX has `Server/node.exe`, not `Server/.playwright/node/win32_x64/node.exe`.
- It holds only 170 `Server/` entries. Files with the same name in different folders overwrite
  each other: localized `*.resources.dll` satellites, `LICENSE`, `SKILL.md`.
- The installed extension (`…\18.0_…Exp\Extensions\Brinell Contributors\Brinell BDD\0.1.0\Server`)
  has no subfolders either.

The cause is in `tools/Brinell.Bdd.VisualStudio/Brinell.Bdd.VisualStudio.csproj`, target
`PublishLanguageServer`. That target builds the `Content` item inside a target and sets
`VSIXSubPath` / `Link` from `%(RecursiveDir)`. In that position `%(RecursiveDir)` comes out empty,
so every file lands directly in `Server\`.

The localized satellite assemblies are lost the same way. That is harmless today, but it is the
same bug.

## Fix direction

- Keep the folder structure in the VSIX. Either set the metadata after the item exists (a second
  `ItemGroup` that updates `VSIXSubPath`/`Link` from `%(RecursiveDir)`), or compute it from the
  path relative to `$(ServerPublishDir)`.
- Check the result with `unzip -l …vsix | grep .playwright/node/win32_x64/node.exe`, then
  reinstall into the experimental instance.
- The error message disappears too quickly to read. The Scraper window should keep a failed
  Connect's message on screen until the user dismisses it.
- Chromium itself is a separate step (`playwright.ps1 install chromium`). Once node.exe is found,
  `PlaywrightScrapeSource` already reports a missing Chromium clearly.

## Why the message disappeared

Every `ScraperResult` carries `problem`, null on success, and `answer()` in
`tools/Brinell.Cli/Lsp/ScraperPage.cs` passed it to `showProblem`. After a failed call the page
refreshes (`checkApp` → `refresh` → `answer("progress")`), and that successful call hid the error
straight away. Now a problem stays until the same call succeeds, another step opens, or another
problem replaces it.
