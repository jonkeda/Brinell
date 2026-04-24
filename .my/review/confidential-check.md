# Confidential / Exact-Specific Content Audit

Review checklist for references to Exact Online, Synergy, Oravey, Iosk, or other confidential/internal identifiers that should not ship in the open-source Brinell sources.

> **Scope:** `srcnew/`, `testsnew/`, `samples/` — the publishable source code.
> **Out of scope:** `.my/`, `.copilot-tracking/`, `docs/` planning docs (private, never published).

---

## Summary

| Area | Status | Notes |
|------|--------|-------|
| `srcnew/` source code | ✅ Clean | No Exact/Synergy/confidential refs in any `.cs` file |
| `srcnew/` project files | ⚠️ Review | `Directory.Build.props` has `github.com/Iosk/Brinell` URLs |
| `testsnew/` test code | ⚠️ Review | Hardcoded local path with `Iosk\Oravey` in one test file |
| `samples/` | ⚠️ Review | `TestRunner.ps1` has hardcoded local path with `Iosk\Oravey` |
| Root scripts | ⚠️ Review | 4 `.ps1` scripts have hardcoded `Iosk\Oravey` paths |
| `docs/` | ⚠️ Info | Multiple Oravey/Exact refs — acceptable if docs stay private |

---

## Findings

### 1. `srcnew/Directory.Build.props` — Iosk GitHub URLs

**File:** `srcnew/Directory.Build.props`, lines 20-21

```xml
<PackageProjectUrl>https://github.com/Iosk/Brinell</PackageProjectUrl>
<RepositoryUrl>https://github.com/Iosk/Brinell</RepositoryUrl>
```

- [ ] **Action:** Decide whether `Iosk` is the intended public GitHub org name. If not, replace with the correct org/user.

---

### 2. `testsnew/Brinell.Maui.UITests` — Hardcoded local path

**File:** `testsnew/Brinell.Maui.UITests/Tests/Container/IndexedContainerTests.cs`, line 64

```csharp
System.IO.File.WriteAllText(@"E:\repos\Private\Iosk\Oravey\Brinell\TestResults\pagesource.xml", pageSource);
```

- [ ] **Action:** Remove or replace with a relative/temp path. This leaks the local dev folder structure (`Iosk\Oravey`).

---

### 3. `samples/TestRunner.ps1` — Hardcoded local path

**File:** `samples/TestRunner.ps1`, line 4

```powershell
$appPath = "E:\repos\Private\Iosk\Oravey\Brinell\samples\Brinell.Samples.Stride.App\bin\Debug\net10.0-windows\Brinell.Samples.Stride.App.exe"
```

- [ ] **Action:** Use a relative path or `$PSScriptRoot`-based path.

---

### 4. Root `.ps1` scripts — Hardcoded local paths

**Files:** `test-key-diag.ps1`, `test-key-verify.ps1`, `test-pipe.ps1`, `test-key.ps1`

All contain:
```powershell
$gamePath = "e:\repos\Private\Iosk\Oravey\Brinell\samples\..."
```

- [ ] **Action:** Use relative paths or remove if these are dev-only throw-away scripts.

---

### 5. `docs/` — Oravey namespace references (informational)

Multiple docs reference the old `Oravey.UITestFramework` namespace:

| File | Content |
|------|---------|
| `docs/README.md` | "Documentation for Oravey UI Test Framework v3.0" |
| `docs/getting-started/quick-start.md` | `Oravey.UITestFramework.Wpf` project refs |
| `docs/getting-started/framework-overview.md` | `Oravey.UITestFramework.*` namespace diagram |
| `docs/guides/test-writing-guide.md` | `using Oravey.UITestFramework.Wpf;` |
| `docs/run/WPF.md` | `%TEMP%/OraveyUITests/` |
| `docs/run/Playwright.md` | `E:\repos\Private\Iosk\Oravey\Brinell` paths |
| `CHANGELOG.md` | "Migrated from Oravey.UITestFramework namespace" |

- [ ] **Action:** If docs are published, update all `Oravey` → `Brinell` references. If private-only, no action needed.

---

### 6. Exact Online / Synergy references

**No references found in `srcnew/`, `testsnew/`, or `samples/`** — these only appear in `.my/Scraper/` planning docs (private).

- [x] **Clean** — no Exact Online or Synergy content in publishable code.

---

### 7. Credentials / secrets scan

**No hardcoded credentials, API keys, tokens, or passwords found** in any source file.

- [x] **Clean** — `docs/requirements/non-functional.md` and `docs/guides/best-practices.md` explicitly warn against this, and the codebase follows that guidance.

---

## Not checked (manual review recommended)

- [ ] Binary files, images, or embedded resources that might contain logos or branding
- [ ] `.gitignore`'d files (build output, `bin/`, `obj/`, user secrets)
- [ ] NuGet package metadata beyond `Directory.Build.props`
- [ ] Git history (old commits may contain since-removed confidential content)
