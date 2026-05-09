# Ways To Ask For New Docs

This guide shows alternative ways to ask for a document besides short commands like "Create a UAT on phase 08." It is designed to improve first-pass quality with minimal back-and-forth.

## 1) Direct Command Style
Use a short command when you want speed.

Examples:
- Create a UAT for phase 08.
- Create an RCA for incident 2026-05-09 login outage.
- Create a PRD for Phase 09 map export.

Best when:
- Scope is already known.
- You are fine refining in a second pass.

## 2) Command Plus Context
Add one line of context to get a better first output.

Examples:
- Create a UAT for phase 08. Focus on acceptance criteria for data import and validation.
- Create an RCA for the scraper timeout issue. Audience is engineering and support.
- Create a Test Plan for phase 08. Keep it risk-based and include smoke tests.

Best when:
- You know the audience or emphasis.
- You want fewer follow-up edits.

## 3) Outcome-First Request
Start with desired outcome, then ask for the document.

Examples:
- I need sign-off from product owners this week. Create a UAT for phase 08.
- I need to brief leadership on cause and prevention. Create an RCA from this incident summary.
- I need deployment readiness by Friday. Create a deployment plan for phase 08.

Best when:
- You want the structure tuned to business purpose.
- You need executive-ready language.

## 4) Structured Input Request
Provide a compact field set in one message.

Template:
- Doc Type:
- Subject:
- Phase/Version:
- Audience:
- Must Include:
- Tone:
- Deadline:

Example:
- Doc Type: UAT
- Subject: Phase 08 import pipeline
- Phase/Version: 08
- Audience: Product + QA
- Must Include: happy path, invalid files, duplicate detection
- Tone: concise and formal
- Deadline: this sprint

Best when:
- You want predictable quality.
- You want repeatable results across many docs.

## 5) Guided Questioning Mode
Ask for questions first, then generation.

Examples:
- Create a UAT for phase 08 using guided questioning.
- Before writing the RCA, ask me up to 8 focused questions.
- Start in guided mode: gather missing details, then generate the doc.

What guided mode should do:
- Ask only high-value questions.
- Stop when enough detail is collected.
- Produce the final doc in one pass.

Suggested guided question set:
- What is the exact scope?
- Who is the primary audience?
- What decisions should this doc enable?
- What must be in scope versus out of scope?
- Any constraints, risks, or deadlines?
- Preferred format and level of detail?
- Any source notes or links to include?

## 6) Two-Step Drafting Mode
Ask for a skeleton first, then expand.

Examples:
- Draft only the UAT outline for phase 08, then ask me what to expand.
- Create an RCA skeleton first, with placeholders for unknowns.
- Step 1 outline, step 2 full document after my approval.

Best when:
- Requirements are still evolving.
- You want control over structure before full writing.

## 7) Quality-Constrained Request
Specify constraints so the output is immediately usable.

Examples:
- Create a UAT for phase 08 in 1 page max.
- Create an RCA with a strict timeline table and CAPA section.
- Create a PRD with measurable success metrics only.

Useful constraints:
- Length limit
- Reading level
- Required sections
- Must include tables/checklists
- Exclude implementation details

## 8) Review-Ready Request
Ask for a draft plus review checklist.

Examples:
- Create a UAT for phase 08 and include a reviewer checklist.
- Create an RCA and add open questions at the end for team review.
- Create a Test Plan and add an approval sign-off block.

Best when:
- You plan to circulate the draft immediately.
- You want faster stakeholder feedback.

## Quick Prompts You Can Reuse
- Create a UAT for phase 08. If details are missing, switch to guided questioning.
- Create an RCA from this summary. Ask only essential questions first.
- Create a PRD for phase 09. Start with a skeleton, then expand after my review.
- Create a Test Plan for phase 08 with risk-based prioritization and a sign-off checklist.
- Create a Deployment Plan for phase 08, include rollback criteria and verification steps.

## Practical Default
If you are unsure, this default prompt usually works well:

Create a [DOC TYPE] for [SUBJECT/PHASE]. Use guided questioning first, ask up to 7 focused questions, then generate a review-ready draft with clear headings, assumptions, and open questions.
