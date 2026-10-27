# Tiny prompts against the skills: world/580

On one capability, tiny prompts gave briefs, a design and a review as good as the skill-made ones
in substance, and on some points better: they raised issues the originals found only at review or
play. What they lost was the house shape, a small scope and the process steps, which is what
[tiny.md](tiny.md) says a factory must still hold. The skills can lose their templates; a short
list of corrections and the loop's gates remain.

## What was run

The capability was Tomorrow's world/580, water as shapes, built 2026-10-18 and 19. Each run was a
cold agent with a tiny prompt ("Create a brief for…", "Create a design for…", "Create a review
of…") and a checkout from just before the brief, so the built code was not there to copy. The
agents had the project's CLAUDE.md and writing rules, as every factory run does, but not the skills,
the templates, the method or any other capability's documents.

- Briefs: five runs from the plan item. Runs 1 and 3 read the original brief by mistake and are set
  aside; run 2 saw its first lines; runs 4 and 5 are clean.
- Design: one run from the original brief.
- Review: one run from the build commit, the brief and the design, without building or running
  tests.

The documents are in [tiny-runs/world-580/](tiny-runs/world-580/); the originals are in Tomorrow's
`.sdlc/modules/world/580-water-as-shapes/`.

## What the tiny prompts did well

- Every brief saw that Edam lies outside the dev area of the day (the Hoorn area stops at 52.58° N,
  Edam is at 52.51°) and proposed Enkhuizen. The original asked the owner to check Edam; the owner
  played it, probably through the full import, so this is a careful point rather than a miss.
- Every brief, and the review, asked whether a tile's water cover should still paint where a shape
  exists; the plan said shapes replace the tiles, the original brief said "beside".
- Every brief named the size of the big lakes as unknown; the original found it at review ("big
  lakes projected whole per cell").
- The design made the original's core choices (a water field in the cell body, version 4, the table
  emptied, three passes, the multipolygon code shared) and also planned two fixes the original
  found only at review: a narrow canal crossing a cell without its centre, and a way that is also a
  relation's outer ring.
- The review found the original review's blocker (one bad area stops a whole import), two of its
  other fixes, and a few more points of its own.

## What they lost

- **Shape:** each brief chose its own headings ("What you will play", "What is not known yet",
  "Next"); no two match, and none matches the template.
- **Gherkin:** briefs 4 and 5 wrote scenarios as bullets; the method needs Given, When, Then that a
  page object can perform, because they become the acceptance tests.
- **Small scope:** briefs 4 and 5 widened the step (water names on hover, a one-square channel rule,
  a new area, a decision record); the original kept it small and named what comes later.
- **Length:** briefs of 660 to 950 words against 394, a design of 1,860 against 580; the writing
  rules ask for a brief read in two minutes.
- **Process:** the design asked no architect or test designer; the review ran no tests, by the
  experiment's rule, and so missed the two faults the original's tests found; neither wrote the
  owner's "how to play" or the record of gates passed without the owner.

## What it costs

The skills and templates behind these three documents are about 2,570 words (brief 437, design
1,358, review 773), with the 4,685-word method beside them. A tiny prompt is about 30 words. Most of
the skills' words are not the template: the design skill's pre-check, the lessons from what the
architect kept blocking on, is about 700 words on its own. Those are corrections, and they stay.

## The corrections this run shows

1. A brief's scenarios are Gherkin, with steps a page object can perform.
2. Keep the capability small; name what grows later instead of building it.
3. A brief reads in two minutes, a design stays to what the build needs.
4. A review runs the tests its diff touches and says how to play the result.
5. A design is reviewed by the architect before it passes; that is the loop's gate, not the
   document's text.
6. Where a gate or a check reads a document, its section names are fixed; elsewhere the shape may
   vary.

## How far to trust this

One capability, a single design and review run, two clean briefs and one nearly clean. The
comparison is mine, not the owner's. The review could not run tests, so it is weaker than a real
one by design. Next is the owner's read of the runs, then a second capability of another kind (a
page, not an import) before the skills change.

## Open questions

- Shrink Tomorrow's skills now? Proposal: not yet; first a second capability, then try one skill,
  step-brief, as a tiny prompt plus the corrections above, on a real step.
- Fix the headings? Proposal: only those a check reads (for a brief, the scenarios); let the rest
  vary and see whether the owner minds.
- Is the extra insight real or luck? Proposal: count, over the next few steps, the points a tiny run
  raises that the skill run didn't, and the reverse.
