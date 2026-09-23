# Android App to .NET MAUI: Recommended Shorthand Sequence

This document describes which shorthand doc types I would use, and in which order, to plan and execute a migration from a native Android app to .NET MAUI.

## Recommended Order

1. `GAP`
- Compare current Android app capabilities vs target MAUI capabilities.
- Output: migration gap list (UI, platform APIs, libraries, build/release, test tooling).

2. `BRD`
- Confirm business goals, constraints, timeline, and stakeholders.
- Output: business outcomes, budget/time boundaries, and approval criteria.

3. `PRD`
- Define what must be preserved, improved, deferred, or removed in the MAUI version.
- Output: feature scope, personas, user stories, success metrics.

4. `NFR`
- Lock quality targets early (startup time, memory, offline behavior, security, telemetry).
- Output: measurable non-functional thresholds for parity and acceptance.

5. `TRD`
- Set technical constraints and migration standards before design.
- Output: target frameworks, package policy, DI/state/navigation approach, platform interop rules.

6. `HLD`
- Map Android architecture to MAUI architecture at component level.
- Output: major modules, boundaries, data flow, integration points.

7. `ADR` (repeat as needed)
- Record key decisions with trade-offs.
- Typical ADR topics: MVVM toolkit choice, navigation pattern, local storage, auth, background jobs, push notifications.

8. `LLD`
- Break HLD into implementable module-level designs.
- Output: view/viewmodel responsibilities, service contracts, validation rules, error handling.

9. `API SPEC` (if backend contracts are impacted)
- Formalize endpoint/schema adjustments needed by MAUI clients.
- Output: request/response contracts, auth behavior, versioning notes.

10. `PLAN`
- Convert architecture/design into an execution sequence.
- Output: sprint or phase plan with dependencies and checkpoints.

11. `PHASE` (one per major stage)
- Organize execution in controlled chunks.
- Common phases: Foundation, Feature Parity, Stabilization, Release Readiness.

12. `WORKSTREAM`
- Split parallel tracks to reduce blocking.
- Typical workstreams: UI migration, platform services, data/offline, QA automation, release/ops.

13. `TASKMAP`
- Decompose each workstream into concrete task trees.
- Output: prioritized tasks, dependency graph, critical path.

14. `IMPLEMENTATION`
- Define coding-level execution details for each major feature or module.
- Output: technical task specs, code-level notes, validation and rollout guidance.

15. `TESTPLAN`
- Define strategy for regression, parity, integration, and device coverage.
- Output: environments, test types, entry/exit criteria, risk-based priorities.

16. `RTM`
- Ensure every requirement is tied to tests and implementation artifacts.
- Output: traceability matrix from BRD/PRD/NFR to code and tests.

17. `TC`
- Produce executable test cases for critical flows and edge cases.
- Output: preconditions, steps, expected outcomes, pass/fail criteria.

18. `UAT`
- Validate business acceptance with real users/stakeholders.
- Output: acceptance evidence and sign-off status.

19. `DEP`
- Plan production rollout in detail.
- Output: deployment sequence, ownership, verification checkpoints.

20. `ROLLBACK`
- Prepare safe reversal path before release.
- Output: rollback triggers, steps, data integrity checks, communications.

21. `RUNBOOK`
- Prepare support and operations procedures.
- Output: diagnostics, common failures, recovery and escalation paths.

22. `RELEASE-NOTES`
- Publish clear release communication for users and stakeholders.
- Output: feature/fix summary, known issues, upgrade actions.

23. `RETRO` or `AAR`
- Capture lessons learned and improvements for the next migration/release cycle.
- Output: what worked, what did not, concrete follow-up actions.

## Fast-Track Minimal Sequence

If you need a lean version for a smaller app:

1. `GAP`
2. `PRD`
3. `NFR`
4. `TRD`
5. `HLD`
6. `ADR`
7. `PLAN`
8. `IMPLEMENTATION`
9. `TESTPLAN`
10. `UAT`
11. `DEP`
12. `ROLLBACK`
13. `RELEASE-NOTES`

## Practical Rule of Thumb

- Use full sequence for medium/large apps, regulated domains, or multi-team delivery.
- Use fast-track sequence for small apps with limited platform complexity.
- Keep `ADR` ongoing during implementation whenever a material technical choice changes.
