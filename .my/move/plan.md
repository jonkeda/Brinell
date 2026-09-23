# Plan: copy Brinell to exact-mobile-apps/test-tooling

## Status (2026-09-23)

- [x] Pre-flight: the org repo is private, I have WRITE access, and there is no
      branch protection on `main`. The org `main` already had one commit
      (`9a262b6 Initial commit`, a README containing only `# mobile-test-framework`).
- [x] gitleaks over the full history found 2 hits, both the placeholder
      `sk-1234567890abcdef` in a "don't do this" docs example. No real secrets.
- [x] `org` remote added. Credentials for `https://github.com/exact-mobile-apps`
      come from `gh auth git-credential`, set in this repo's local git config only.
- [x] `move` created from `main`, then joined with `org/main` using
      `--allow-unrelated-histories`. The README conflict was resolved by keeping
      Brinell's README.
- [x] Cleanup commit: removed `.gitmodules` and `test-output.txt`, ignored
      `test-output.txt`, added a Repository section to the README.
- [x] `move` pushed to `org`.
- [ ] PR `move` → `main` (merge commit). CI rewritten and verified locally, see
      [CI and packages](#ci-and-packages).
- [ ] Licence decision, org secrets, local remote switch, archive personal repo.

Source: `https://github.com/jonkeda/Brinell` (branch `main`, 246 commits, no tags, ~12 MB `.git`).
Target: `https://github.com/exact-mobile-apps/test-tooling`.

## Decisions

1. **History:** keep the full history (mirror push).
2. **`.my/`:** keep it and move it along to the new repo as is (299 tracked files).
3. **SPX submodule:** remove the stale `.gitmodules` entry (it points at
   `jonkeda/SPX`, and no gitlink is tracked).
4. **Source of truth:** the org repo is primary. `jonkeda/Brinell` is archived for
   now. For keeping the two in sync, see [Sync](#sync-org--personal).
5. **Land via a `move` branch:** we can't merge to the org `main` straight away.
   The history goes to a `move` branch in the org repo, and reaches `main` through
   a pull request once it is approved.

## Pre-flight checks (in the current repo)

- [ ] Working tree clean, `main` pushed to origin.
- [ ] Secret scan of history (e.g. `gitleaks detect`). Nothing is expected, since
      workflows only use `secrets.*`.
- [ ] Confirm the target repo exists and that I can push branches to it.
- [ ] Check whether the org `main` already has commits (e.g. an initial README).
      This decides how the PR is made. See [Pull request](#pull-request-move--main).

## Copy to the `move` branch

Working directory: `Brinell/`.

```powershell
git remote add org https://github.com/exact-mobile-apps/test-tooling.git
git fetch org
git switch -c move main
git push -u org move
```

This pushes all 246 commits of `main` as `move`. Nothing is pushed to the org `main`.
There are no tags to carry over.

If the org repo is completely empty, GitHub makes `move` the default branch. Once
`main` exists, set the default back to `main` in the repo settings.

## Clean-up commit on `move`

Make these as normal commits on `move` and push them. They become part of the PR.

- Remove `.gitmodules` (SPX).
- Remove `test-output.txt` and add it to `.gitignore`.
- `README.md`: new repo URL and clone instructions.
- `LICENSE`: currently MIT, "Brinell Contributors". Confirm with the org whether
  it stays MIT/public or becomes internal.
- 16 tracked files mention `Exact.Construction` / `MainConstruction`. That is fine
  for an internal org repo. Review them if the repo goes public.

## CI and packages

The four old workflows (stale since the `src/` → `srcnew/` move and .NET 10) were
replaced on `move` by two:

- **`ci.yml`** runs on PRs into `main`, pushes to `main`, and by hand. Job 1: .NET from
  `global.json`, `dotnet workload restore`, a **Debug** build of `srcnew\Brinell.sln`
  (the UI Automation bridge only compiles in Debug), and the 9 unit-test projects
  (Core, Maui, Wpf, WinForms, Blazor, Automation, Mocking, Uat, Generator). The UI and
  UAT suites launch real apps and stay local. Job 2: packs the 15 libraries in
  Release with `tools/Scripts/pack.ps1` and uploads them as an artifact.
- **`release.yml`**: publishing a GitHub release tagged `vX.Y.Z` packs with that
  version, pushes to the org's GitHub Packages feed, and attaches the `.nupkg` files
  to the release. **No nuget.org push**, because the repo is private.
- `RepositoryUrl`/`PackageProjectUrl` in both `Directory.Build.props` files now
  point at the org repo. They previously pointed at `github.com/Iosk/Brinell`.

Verified locally from a fresh checkout of `move`:

- Debug build: 0 errors (32 min cold, mostly the Android/iOS heads of the sample app).
- Unit tests: 696 passed, 1 skipped, about 1 min.
- `pack.ps1`: 15 `.nupkg` + `.snupkg`, about 1 min, with the org repo URL in the nuspec.

A Release build of the whole solution fails
(`Brinell.Samples.Maui.App` cannot find `Brinell.Maui.AppSupport.pri`), which is why
CI builds Debug and packs project by project.

- [ ] Check that the org Actions policy allows `actions/checkout`, `setup-dotnet`,
      `setup-java` and `upload-artifact`, and Windows runners.
- [ ] First real run is the PR into `main`. `workflow_dispatch` only works once
      `ci.yml` is on the default branch.

## Pull request `move` → `main`

- **Org `main` does not exist yet:** create `main` from the org repo's first
  commit, or let an admin create it, then open the PR `move` → `main`.
  Alternatively, an admin pushes `move` to `main` directly after approval.
- **Org `main` already has commits** (e.g. a README): GitHub cannot open a PR
  between unrelated histories. Join them on `move` first:

  ```powershell
  git fetch org
  git switch move
  git merge org/main --allow-unrelated-histories
  git push org move
  ```

  Then open the PR `move` → `main`.

Merge the PR with a **merge commit**, not squash or rebase. Squash collapses the
246 commits into one and loses the history (decision 1). Check that the org's
branch-protection settings allow merge commits.

## Switch local clone

Working directory: `Brinell/`. Do this after the PR is merged. Until then, keep
working on `move` and push to `org`.

```powershell
git remote rename origin personal
git remote rename org origin
git fetch origin
git switch main
git reset --hard origin/main
git branch -u origin/main main
git branch -d move
```

`reset --hard` is safe here only if all local work was on `move`, which is now merged.

Consumers in the parent folder only reference Brinell by relative path. If the folder
is renamed to `test-tooling/`, update `../AGENTS.md` (layout table) and any relative
`Brinell/` paths.

## Sync (org → personal)

Until the PR is merged, the personal `main` is still the working line. Keep `move`
current with `git merge main` on `move` and `git push org move`.

After the merge, sync is **one-way**, org → personal. Never commit to `jonkeda/Brinell`
directly, so it can always fast-forward and never diverges.

An archived GitHub repo is read-only and **rejects pushes**. "Archived" and
"kept in sync" therefore rule each other out at any given moment. Pick one:

| Option | How | Pros | Cons |
|---|---|---|---|
| A. Manual catch-up (**recommended for now**) | Unarchive → `git push personal origin/main:main` → re-archive | Personal stays archived; no tokens | Manual; only as fresh as the last catch-up |
| B. Dual push from the local clone | Don't archive. `git remote set-url --add --push origin https://github.com/exact-mobile-apps/test-tooling.git` and again with the personal URL. Every `git push` goes to both | Zero effort; no tokens | Only syncs what *I* push; merged org PRs need a `git pull` + push first |
| C. GitHub Action in the org repo | Don't archive. A workflow on push to `main` runs `git push` to `jonkeda/Brinell` with a fine-grained PAT stored as a repo secret | Fully automatic, includes merged PRs | A personal token in an org repo; may clash with org policy |

Recommendation: start with **A** while the personal repo is archived. If regular
syncing turns out to be needed, unarchive the personal repo and switch to **B**.
Only use **C** if the org is fine with it.

## Afterwards

- Add a "moved to exact-mobile-apps/test-tooling" note to the `jonkeda/Brinell`
  README, then archive it once the org repo is confirmed working.
- Add branch protection on `main` in the org repo.
