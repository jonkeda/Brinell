# Phase 7 — Project Integration & Output

## Goal

Write generated page objects and custom control objects to standalone .NET projects, one per site corpus. Each project references Brinell.Html and compiles independently — no risk of breaking downstream code. Custom controls are written to a `Controls/` subfolder and generated before pages, since page objects reference them.

## Tasks

### 7.1 — Project Scaffolding

Create a new `.csproj` per site corpus (e.g. `ExactOnline.Pages`, `Synergy.Pages`) with a `Brinell.Html` reference and `Controls/` + `Pages/` subfolders.

**Implementation:**

- Template `.csproj` embedded as a resource:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>{RootNamespace}</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Brinell.Html" Version="{BrinellVersion}" />
  </ItemGroup>
</Project>
```

- `ProjectScaffoldService` creates the output directory with `Controls/` and `Pages/` subfolders, writes the `.csproj` from the template, substituting `{RootNamespace}` and `{BrinellVersion}` from site configuration.
- If a local Brinell source checkout is configured, use `<ProjectReference>` instead of `<PackageReference>`.
- Create a `GlobalUsings.cs` with common imports (`Brinell.Html.Abstractions`, `Brinell.Html.Controls`, `Brinell.Core.Locators`).
- Project structure after scaffolding:

```
{SiteName}.Pages/
├── {SiteName}.Pages.csproj
├── GlobalUsings.cs
├── Controls/
└── Pages/
```

---

### 7.2 — Configure Output Project Path & Namespace

Output path and root namespace are per-site settings, configured when creating the site corpus and stored in the SQLite corpus database.

**Implementation:**

- Per-site output settings stored in the site corpus:

```csharp
public class SiteOutputSettings
{
    public string OutputPath { get; set; } = "";
    public string RootNamespace { get; set; } = "";
    public string BrinellVersion { get; set; } = "";
    public bool UseProjectReference { get; set; }
    public string? BrinellSourcePath { get; set; }
}
```

- Set during site corpus creation (site configuration wizard). Can be changed later in site settings.
- Each site corpus produces its own standalone project — output path and namespace are independent per site.
- Namespace is not tied to any consuming solution — purely for the standalone generated project.
- Global defaults for `BrinellVersion`, `UseProjectReference`, and `BrinellSourcePath` in `%APPDATA%/Brinell.Scraper/settings.json`; per-site values override.
- Validate output path is writable on save.

---

### 7.3 — Write Generated `.cs` Files

Code generation for using statements and file output. Custom controls are written first (Task 7.3b), then page objects.

**Implementation:**

- Required `using` statements added to every generated page file:

```csharp
using Brinell.Html.Abstractions;
using Brinell.Html.Controls;
using Brinell.Core.Locators;
using {RootNamespace}.Controls;
```

- The `using {RootNamespace}.Controls;` import is always included so pages can reference custom control types.
- Additional `using` statements added as needed based on control types discovered (e.g. `Brinell.Html.Controls.Tables` if table controls are present).
- File naming convention: `{PageName}.cs` — PascalCase, matching the class name (e.g. `LoginPage.cs`, `TimeEntryPage.cs`).
- Page files are written to the `Pages/` subfolder of the project.
- **Generation order:** controls (Task 7.3b) → pages (this task). Pages reference control types, so controls must exist first.
- `CodeOutputService.WritePageObjectAsync(string projectPath, GeneratedPageObject page)`:
  1. Ensure `Pages/` output directory exists.
  2. Build full file content: usings → namespace → class.
  3. Write via `File.WriteAllTextAsync` with UTF-8 encoding to `Pages/{PageName}.cs`.
  4. Record the generation timestamp for incremental tracking.
  5. Return the written file path for UI feedback.

---

### 7.3b — Write Custom Controls

Write ControlObject files from the control registry to the `Controls/` subfolder. This runs before page output (Task 7.3) because pages reference control types.

**Implementation:**

- The control registry in the SQLite corpus tracks all generated custom controls for the site.
- Each control class extends `ContainerBase<TParent, TScope>` and is written to `Controls/{ControlName}.cs`.
- Required `using` statements for control files:

```csharp
using Brinell.Html.Abstractions;
using Brinell.Html.Controls;
using Brinell.Core.Locators;
```

- `CodeOutputService.WriteControlObjectAsync(string projectPath, GeneratedControlObject control)`:
  1. Ensure `Controls/` output directory exists.
  2. Build full file content: usings → namespace → class.
  3. Write via `File.WriteAllTextAsync` with UTF-8 encoding to `Controls/{ControlName}.cs`.
  4. Return the written file path for UI feedback.
- All controls from the registry are written before any pages, ensuring compilation order is correct.
- File naming convention: `{ControlName}.cs` — PascalCase, matching the class name (e.g. `DatePickerControl.cs`, `AutocompleteControl.cs`).

---

### 7.4 — Roslyn Compile Check

Run `dotnet build` on the standalone project to verify generated code compiles.

**Implementation:**

- `CompileCheckService.BuildAsync(string csprojPath)`:

```csharp
var psi = new ProcessStartInfo
{
    FileName = "dotnet",
    Arguments = $"build \"{csprojPath}\" --no-restore --verbosity quiet",
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false,
    CreateNoWindow = true
};

using var process = Process.Start(psi)!;
var stdout = await process.StandardOutput.ReadToEndAsync();
var stderr = await process.StandardError.ReadToEndAsync();
await process.WaitForExitAsync();

return new BuildResult(process.ExitCode == 0, stdout, stderr);
```

- Run `dotnet restore` once when project is first scaffolded.
- UI displays build output in a collapsible panel — green checkmark on success, red with error details on failure.
- Parse MSBuild error format (`file(line,col): error CS####: message`) to link errors back to generated code.

---

### 7.5 — Detect Existing Page Objects → Update Mode

Incremental generation — only write new or changed pages. Merge new controls into an existing class without removing existing properties.

**Implementation:**

- **Incremental detection:** compare each snapshot's `CapturedAt` timestamp with the page's last-generation timestamp (stored in the corpus). Only regenerate pages whose snapshot is newer than the last generation.
- Pages that haven't changed since last generation are skipped entirely.
- `PageObjectMergeService.MergeAsync(string existingFilePath, GeneratedPageObject newPage)`:
  1. Read existing `.cs` file.
  2. Parse with Roslyn `CSharpSyntaxTree.ParseText()`.
  3. Find the class declaration via `root.DescendantNodes().OfType<ClassDeclarationSyntax>().First()`.
  4. Extract existing property names into a `HashSet<string>`.
  5. For each property in `newPage` — if the name is NOT in the existing set, add it.
  6. Preserve existing property order; append new properties at the end.
  7. Use Roslyn `SyntaxFactory` to build new property nodes, insert via `ClassDeclarationSyntax.AddMembers()`.
  8. Format with `Formatter.Format()` and write back.
- Never remove existing properties — only additive changes.
- After successful merge/write, update the last-generation timestamp for the page.
- Log added/skipped properties and skipped-unchanged pages for UI feedback.

---

### 7.6 — Generate Companion Test Scaffold (Optional)

Generate an xUnit test class with a basic smoke test.

**Implementation:**

- Template:

```csharp
using Xunit;

namespace {Namespace}.Tests;

public class {PageName}Tests
{
    [Fact]
    public void Should_Create_Page_Object()
    {
        // Arrange & Act
        var page = new {PageName}();

        // Assert
        Assert.NotNull(page);
    }
}
```

- Test file naming convention: `{PageName}Tests.cs` placed in a `Tests/` subfolder of the output project, or a separate `.Tests` project if configured.
- Option to create a separate test `.csproj` referencing the page object project and xUnit.
- Checkbox in generation UI: "Generate test scaffold" (default: checked).

---

## Acceptance Criteria

- [ ] Running the tool for a new site corpus creates a valid `.csproj` with `Controls/` and `Pages/` subfolders that restores and builds with zero errors.
- [ ] Output path and namespace are per-site settings, configured during site corpus creation and persisted in the corpus database.
- [ ] Custom control classes are written to `Controls/` subfolder before any page files are written.
- [ ] Generated page `.cs` files include `using {RootNamespace}.Controls;` and compile without errors.
- [ ] Generated control `.cs` files extend `ContainerBase<TParent, TScope>` and compile without errors.
- [ ] Generation order is enforced: controls first, then pages.
- [ ] Incremental output only writes new/changed pages — unchanged pages (snapshot `CapturedAt` ≤ last-generation timestamp) are skipped.
- [ ] `dotnet build` is invoked after generation; results are displayed in the UI.
- [ ] Re-generating for an existing page merges new properties without removing existing ones.
- [ ] Companion test class is generated when the option is enabled.
- [ ] Generated standalone project has no dependency on any consuming solution.
- [ ] Each site corpus produces its own independent project.

## Dependencies

- Phase 5 (LLM Code Generation) — provides `GeneratedPageObject` model.
- Phase 6 (Code Preview & Editing) — user may edit before writing to disk.
- `Brinell.Html` NuGet package or project reference available.
- .NET 10 SDK installed on the machine.
- Roslyn (`Microsoft.CodeAnalysis.CSharp`) for merge and compile-check logic.

---

## Unit Test Plan

### Testable Components (~28 tests)

| Component | Tests | Strategy |
|-----------|-------|---------|
| `ProjectScaffoldService` | 6 | Creates .csproj from template, substitutes namespace/version, creates Controls/Pages folders, GlobalUsings.cs, project reference vs package reference |
| `CodeOutputService` — pages | 5 | Writes to Pages/ subfolder, correct filename, UTF-8 encoding, using statements, overwrites existing |
| `CodeOutputService` — controls | 5 | Writes to Controls/ subfolder, generation order (controls before pages), correct namespace |
| Using statement builder | 4 | Required usings included, custom control namespace, conditional imports, no duplicates |
| Incremental output | 4 | Unchanged pages skipped, changed pages written, new pages written, timestamp tracking |
| Property merge (re-generation) | 4 | New properties added, existing preserved, removed properties handled, formatting maintained |

### Not Unit-Tested

- `dotnet build` invocation — requires .NET SDK installed; verified by integration test
- MSBuild error parsing — dependent on `dotnet` CLI output format
- File system writes — use temp directories in tests, but actual disk I/O is tested

### Test Infrastructure

- **File system:** Tests use `Path.GetTempPath()` for isolated output directories, cleaned up in `Dispose()`
- **Test data:** Sample `GeneratedPageObject` / `GeneratedControlObject` instances
