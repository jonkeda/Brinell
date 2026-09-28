# AD-018: The Presenter Workspace Is The UAT Project

A Presenter workspace is anchored on **a UAT project and a fixture**, not on built binaries.
Everything else is derived from those two, owned by the fixture, or chosen in the toolbar for
the session.

- **The project finds the assembly.** The Pages assembly's path comes from evaluating the
  project with `dotnet msbuild -getProperty:TargetPath` (spawned, cached per project, TFM and
  configuration), which answers for a project that has never been built. A bare assembly name or
  a searched path still works, and is labelled as found by search.
- **The fixture owns the app.** `AppPath` is an optional override (for Stride, or an app with no
  project); every fixture base resolves its own app. Only a configured path that is wrong is an
  error. The fixture's base class implies the target.
- **Listing never loads.** Fixture candidates, their base chain and their platforms are read with
  `MetadataLoadContext`, which releases the file on dispose. That is why a fixture declares its
  platforms with `[UatPlatforms]`: attribute arguments are readable without loading, property
  values are not.
- **Running loads into a collectible context per session**, disposed when the session ends, so
  the Pages assembly can be rebuilt without restarting Presenter. A Build button depends on this.
- **Presenter writes the config without reformatting it.** `BddConfigWriter` changes only the
  rows it edits; `Apply(md, noOp)` is byte-identical for every config in the repo. Save stays
  enabled while the config is invalid, and a valid save reloads the workspace keeping tree
  expansion and selection.

**Why:** paths rot and project names do not. Both checked-in configs that spelled out an
assembly or app path were stale, while every one using a bare name was right; `AppPath` was
exported to environment variables that only Stride read. Evaluation takes about 0.5 s and needs
no build. Loading an assembly into the default context locks it for the life of the process, so
a Presenter that loaded to list fixtures could build once and never again.

**Ruled out:** a config whose required rows are paths to `.dll` or `.exe` files; `AppPath` as a
required field; hosting MSBuild in-process (`Microsoft.Build` and `MSBuildLocator` in a MAUI
app for ~300 ms); loading the Pages assembly into `AssemblyLoadContext.Default`; a fixture
declaring its platforms through a property; guessing the fixture when the config names none;
a config writer that reformats rows it did not change.

**Broken when:** a Presenter service calls `Assembly.LoadFrom`/`LoadFromAssemblyPath` on the
Pages assembly outside a session context; building after a run fails with the file in use; a
workspace without `AppPath` reports an error; saving an unchanged config produces a diff.

Evidence: [the workspace design at 7162ac7](https://github.com/exact-mobile-apps/test-tooling/blob/7162ac7/.my/present/startup/00-presenter-workspace-design.md),
with the measurements in
[01-assembly-and-app-path-resolution.md](https://github.com/exact-mobile-apps/test-tooling/blob/7162ac7/.my/present/startup/archive/01-assembly-and-app-path-resolution.md)
and [02-project-anchored-workspace.md](https://github.com/exact-mobile-apps/test-tooling/blob/7162ac7/.my/present/startup/archive/02-project-anchored-workspace.md).
