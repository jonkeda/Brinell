# Archive — Presenter config design, working notes

Status: Archive. Superseded by [the design](../00-presenter-workspace-design.md).
Date: 2026-09-22

Three documents written on the way to the current design, kept for the evidence
and the reasoning, not as instructions. Where any of them disagrees with
[00-presenter-workspace-design.md](../00-presenter-workspace-design.md), the
design wins.

| Doc | What it was | Why it was superseded |
| --- | --- | --- |
| [00-config-edit-screen.md](00-config-edit-screen.md) | First design for an editable Config tab | Designed editors for fields that no longer exist in the config |
| [01-assembly-and-app-path-resolution.md](01-assembly-and-app-path-resolution.md) | Study: csproj vs assembly-name resolution, with five experiments | Its measurements survive intact; its `AppPath` recommendation was overtaken |
| [02-project-anchored-workspace.md](02-project-anchored-workspace.md) | Project-anchored workspace, revised once mid-flight | Folded into the design |

**What is still worth reading here:** the measurements in
[01](01-assembly-and-app-path-resolution.md) — MSBuild evaluation timings, the
tree-walk costs, the survey showing every checked-in config that spells out a
path is stale — and the revision note at the end of
[02](02-project-anchored-workspace.md), which records why app discovery was
designed and then deleted.
