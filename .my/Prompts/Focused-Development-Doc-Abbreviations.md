# Focused Development Doc Abbreviations

This document proposes short abbreviations for focused development and planning documents such as roadmaps, phase docs, and step docs. The goal is to make your requests short, consistent, and easy to recognize.

## Recommended Core Set

### ROADMAP - Product or Delivery Roadmap

Use this when you want a time-ordered or milestone-based plan across multiple phases. A ROADMAP request should produce the major goals, sequencing, dependencies, and expected outcomes over time.

Example:

- Create a ROADMAP for the scraper rewrite.

### PHASE - Phase Document

Use this when you want one document dedicated to a single phase of work. A PHASE request should produce the goal of the phase, scope, deliverables, dependencies, risks, and completion criteria.

Example:

- Create a PHASE doc for phase 1.

### STEPDOC - Step Document

Use this when you want a practical breakdown inside a phase. A STEPDOC request should produce a small, focused implementation document for one step, usually with objective, tasks, dependencies, and validation.

Example:

- Create STEPDOCs for phase 1.

### PLAN - Execution Plan

Use this when you want a concrete action plan rather than a broad roadmap. A PLAN request should produce sequenced work items, assumptions, blockers, and checkpoints.

Example:

- Create a PLAN for phase 1 data import.

### MILESTONE - Milestone Document

Use this when you want planning centered on a major delivery checkpoint. A MILESTONE request should produce milestone goals, entry and exit criteria, dependencies, and measures of readiness.

Example:

- Create a MILESTONE doc for the first testable release.

## Strong Alternatives

### EPIC - Epic Definition

Use this when the work is big enough to span multiple stories or steps but smaller than a full roadmap. An EPIC request should produce scope, outcomes, constraints, and the main workstreams.

Example:

- Create an EPIC doc for phase 1 onboarding automation.

### WORKSTREAM - Workstream Document

Use this when you want to split a phase into parallel tracks such as UI, backend, testing, or infrastructure. A WORKSTREAM request should produce goals, ownership, dependencies, and deliverables for that track.

Example:

- Create a WORKSTREAM doc for test automation in phase 1.

### TASKMAP - Task Breakdown Map

Use this when you want a structured decomposition of work into tasks and subtasks. A TASKMAP request should produce a hierarchy of work with ordering and dependencies.

Example:

- Create a TASKMAP for phase 1.

### IMPLEMENTATION - Implementation Document

Use this when you want a development-focused execution doc with technical detail. An IMPLEMENTATION request should produce technical scope, design notes, coding tasks, validation, and rollout considerations.

Example:

- Create an IMPLEMENTATION doc for phase 1 persistence.

### CHECKLIST - Delivery Checklist

Use this when you want a compact actionable list rather than narrative planning. A CHECKLIST request should produce ordered check items with clear completion signals.

Example:

- Create a CHECKLIST for phase 1 release readiness.

## Best Fit For Your Current Patterns

If you currently say:

- Create roadmap
- Create doc for phase 1
- Create step docs for phase 1

Then the cleanest shorthand set is probably:

- ROADMAP
- PHASE
- STEPDOC
- PLAN

That gives you a simple hierarchy:

- ROADMAP = cross-phase direction
- PHASE = one phase definition
- PLAN = execution plan inside a phase
- STEPDOC = small focused doc for one implementation step

## Recommended Default Vocabulary

For clarity and low ambiguity, I would recommend this default set:

- ROADMAP
- PHASE
- PLAN
- STEPDOC
- MILESTONE
- WORKSTREAM
- TASKMAP
- IMPLEMENTATION

## Example Requests

- Create a ROADMAP for the next three phases.
- Create a PHASE doc for phase 1.
- Create a PLAN for phase 1.
- Create STEPDOCs for phase 1.
- Create a WORKSTREAM doc for phase 1 testing.
- Create a TASKMAP for phase 1 import pipeline.
- Create an IMPLEMENTATION doc for phase 1 parser refactor.

## Practical Recommendation

If you want the fewest new abbreviations to remember, start with these four:

- ROADMAP
- PHASE
- PLAN
- STEPDOC

They cover most planning requests without much ambiguity and fit naturally with how you already ask for documents.
