---
description: Show available GSD commands and usage guide
---

Display the complete GSD command reference below. Output ONLY this content — no project analysis, git status, or commentary.

# GSD Command Reference

## Core Workflow
| Command | Description |
|---------|-------------|
| `/gsd-project-new` | Initialize a new GSD project with planning structure |
| `/gsd-progress` | Check progress, show context, route to next action |
| `/gsd-work-pause` | Save complete context for resuming later |
| `/gsd-work-resume` | Restore context and resume from previous session |

## Phase Planning
| Command | Description |
|---------|-------------|
| `/gsd-phase-research {N}` | Deep research before planning a phase |
| `/gsd-phase-discuss {N}` | Interactive Q&A to capture decisions for a phase |
| `/gsd-phase-plan {N}` | Create execution plans for a phase |
| `/gsd-phase-execute {N}` | Execute plans for a phase |
| `/gsd-phase-verify {N}` | Verify completed phase work |

## Quick Mode
| Command | Description |
|---------|-------------|
| `/gsd-quick "description"` | Plan and execute a quick task in one session |

## Roadmap Management
| Command | Description |
|---------|-------------|
| `/gsd-phase-add "description"` | Append a new phase to the roadmap |
| `/gsd-phase-remove {N}` | Remove a future phase and renumber |
| `/gsd-phase-insert {N} "description"` | Insert urgent work as decimal phase |

## Milestone Management
| Command | Description |
|---------|-------------|
| `/gsd-milestone-new` | Start a new milestone |
| `/gsd-milestone-complete` | Complete current milestone and archive |

## Configuration
| Command | Description |
|---------|-------------|
| `/gsd-settings` | Configure workflow toggles and model profile |
| `/gsd-profile-set {profile}` | Switch model profile (quality/balanced/budget) |

## Utilities
| Command | Description |
|---------|-------------|
| `/gsd-todo-add "description"` | Capture task/idea for later |
| `/gsd-todo-check` | List and manage pending todos |
| `/gsd-health` | Check project health and consistency |
| `/gsd-codebase-map` | Generate codebase analysis docs |
| `/gsd-update` | Check for GSD updates |
