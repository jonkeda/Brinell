---
name: decision-record
description: Record an architectural decision for Brinell in `.docs/`. Use when a choice has been made that rules something out - a rule, an invariant, a contract, a "we do it this way and not that way" - so it is written down once, in the durable layer, instead of regrowing as prose scattered through the code. Do not use for a change that merely does something (no decision); use it only when the change forecloses an alternative.
---

# Record a decision

A decision is worth recording when it **rules something out**. A change that merely does
something is not a decision - do not record it. If nothing is now forbidden that was allowed
before, stop here.

## Steps

1. **Confirm it is a decision.** Say in one sentence what is now ruled out. If you cannot,
   it is a change, not a decision - do not add a record.

2. **Find whether an existing decision already covers it.** Read `.docs/README.md` and the
   relevant `.docs/decisions/ad-0xx-*.md`. If one is close, **amend it** rather than adding a
   new record - two homes for one decision is how they diverge. `AD-004` and `AD-009` grew
   this way, correctly.

3. **Write the four fields**, short - the whole record is at most one screen:
   - **Decided:** what the rule is.
   - **Why:** the reason it beats the alternative.
   - **Ruled out:** what is now forbidden.
   - **Broken when:** how you would notice it has been violated (a test, a grep, a symptom).

   Describe the rule, not how the code implements it. The code can be re-read; the decision
   cannot be re-derived.

4. **Add the one-line pointer** to `.docs/README.md` (and to `.docs/invariants.md` if it is an
   invariant). Number a new record `ad-0NN` after the highest existing one.

5. **Point at the evidence, do not copy it.** The plan/analysis/measurement lives in
   `.my/<work>/`. Link it; do not restate it in the decision.

6. **Leave a marker in code only where the site is surprising:** one line,
   `// see AD-0NN`. Never paste the decision into a `<remarks>` block - that is the prose
   this skill exists to prevent.

## Do not

- Do not describe the code. A description regenerates from the source and goes stale.
- Do not duplicate a decision that already has a record - amend instead.
- Do not exceed one screen. If it needs more, it is two decisions or it is describing code.
