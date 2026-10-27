# Tiny: a proposal

Tiny is how Frainet builds dark factories: loops of tiny skills, held by exact gates, with code
only in the gates. A small factory is two loops and a handful of skills; a huge one is a full SDLC
made of more of the same pieces, never a new kind of piece. It is written for the owner and Claude
first, plainly enough for a team to pick up later. It goes first to Todo, then to Tomorrow.

## Why now

The tiny experiment ([tiny-experiment.md](tiny-experiment.md)) showed that today's models already
know how to write a brief, a design or a review. A 75-word skill, a one-line prompt with three
corrections and a gate, gave briefs as good as Tomorrow's 437-word skill and template, and it kept
them in shape where the bare prompt drifted. Most of a factory's text was teaching the model what it
already knew. What it did not know was ours: our facts, our corrections, our gates and our loops.

## The pieces

A factory is made of six kinds of piece, and only these.

| Piece | What it is | Its form |
| --- | --- | --- |
| Loop | Which steps follow which, and who passes each gate | A few lines in the factory file |
| Tiny skill | One step: a prompt, its corrections, its gate | At most about 100 words |
| Gate | Whether a step is done: pass, pass with follow-ups, or stop | Code where it must be exact, a cold reviewer where it is judgement |
| Facts | What the model cannot know: the product, its decisions, where things live | Files every skill reads, such as CLAUDE.md |
| Corrections | What the model gets wrong when left alone | A line in a skill, added after a run showed it |
| Records | Where each turn of a loop leaves its state | A folder per turn and a status file |

The orchestrator is not a piece: it is the main conversation, turning the loops by reading the
factory file. Code exists only inside gates, such as `frainet check` and `frainet trace`.

## One factory file

Each factory is described in one file, `factory.md`: its loops, the skills in each, the gate after
each skill, who passes it, and where the records go. A person reads it in a few minutes; an agent
turns it into work. Todo's first factory would hold two loops:

```text
capability: brief -> design -> build -> review -> accept (the owner)
task (inside build): test -> code -> green -> commit
```

## From small to huge

A factory grows by adding loops, gates and corrections, in the order the loops notes give. Each loop
added brings its own few skills and its gate, and nothing else changes.

| Factory | Loops |
| --- | --- |
| Tiny | task, capability |
| Product | + module, deployment ring |
| Full SDLC | + architecture, specs ring, management |
| Many teams | + who passes which gate, per team; more facts files per product |

Tomorrow today is a full SDLC built by hand. A team's factory is the same as a full SDLC with names
in the "who passes" lines. Nothing in Tiny is only for big factories, so a small one carries no
weight it does not use.

## Keeping it simple

- A skill over 100 words names, in its corrections, the run that made each extra line necessary.
- No templates: a document's shape is fixed only where a gate reads it.
- Code only in gates, and each gate checks exactly what its skill says and no more.
- A piece is added when a run shows it is needed, and removed when nothing uses it.
- Every loop's state is in files, so any turn can start cold.
- One factory file per factory; if it no longer reads in a few minutes, the factory is too big for one file and splits by product, not by kind of piece.

## What Tiny is not

- Not a runner: no program picks the next step or starts agents; the orchestrator does.
- Not a template library: the model knows the craft.
- Not tied to one model or agent: skills are plain text, with a short stub per agent where one is needed.

## How it gets built

1. The `factory.md` format, written from Todo's two loops, and Todo's tiny skills: brief, design,
   build, review, each with its gate.
2. One Todo capability through it, end to end: R11 of TOD.02, step 5 of [plan.md](plan.md), so
   `frainet trace` is the build's gate.
3. The same capability measured against how Tomorrow's `/step` would have done it: words, runs, rework,
   time waiting for a person.
4. Tomorrow's `step-brief` replaced by its tiny skill on a real step, then the next skill, one at a time.

## Open questions

- Is Tiny a name of its own or Frainet's method? Proposal: Frainet's method; Frainet is the kit and
  its gates, Tiny the way factories are made with them.
- How does the orchestrator know the factory file? Proposal: one tiny skill, "turn the loop", that
  reads `factory.md` and the status file and does the next step; it is the only skill every factory
  has.
- How are the corrections kept honest? Proposal: each names the run that showed it, and the
  management loop drops those a newer model no longer needs.
- Where does the review's cold reader come from? Proposal: a subagent with only the skill's gate and
  the document, as the architect agent works in Tomorrow today.
