# Plan — bring `.my/present` in line with the UAT changes

Status: In progress — WP1–WP4 delivered, WP5 open
Date: 2026-09-21
Area: `.my/present/*.md`
Related:

- [Rethinking UAT](../../uat/rethinking-uat.md)
- [Attribute-driven UAT catalog](../../uat/attribute-catalog/README.md)
- [The Gherkin language, formalised](../../uat/attribute-catalog/01-grammar.md)
- [Implementation review (delivered)](../../uat/attribute-catalog/05-implementation-review.md)
- [AD-011 — UAT vocabulary is attribute-declared](../../../.docs/decisions/ad-011-uat-vocabulary-is-attribute-declared.md)
- [UAT phrases and flows](../../../docs/guides/uat-phrases-and-flows.md)

---

## 0. Why this plan exists

`.my/present` is the **presentation/design set** for the Markdown-driven UAT
runner, written 2026-07-07 as a forward-looking proposal. Since then the thing
it proposes has been *built*, and then *reworked*:

- `.my/uat/attribute-catalog/` moved the step vocabulary onto `[UatStep]`
  attributes and deleted all three hand-written phrase tables — shipped and
  reviewed as **delivered** (05-implementation-review.md), recorded as **AD-011**.
- `.my/uat/rethinking-uat.md` settled the *authoring pipeline* question
  (Journeys → Gherkin → run directly; `.cs` only as an escape hatch).
- `.my/uat/complex/` opened a topic `present/` has no doc for at all: multi-user,
  service-call, database and clock/network checks via custom phrases.
- `.my/uat/todos/` added a journey-traced scenario set and a Todo sample.

The result: `present/` still reads as a *proposal for something that does not
exist yet*, and several of its concrete API details are now wrong. This plan
says exactly what to change, doc by doc.

**Scope:** documentation in `.my/present` only. No source changes. Anything that
would change code is listed in §8 as out of scope.

---

## 1. Ground truth to write against

Every edit below must be checked against these, not against memory:

| Question | Authoritative source |
| --- | --- |
| What phrases exist | `docs/guides/uat-phrases-and-flows.md` §Built-In Phrases, verified against `[UatStep]` in `srcnew/Brinell.Core/Interfaces/*.cs` |
| Where phrases are declared | `srcnew/Brinell.Core/Testing/UatStepAttribute.cs`, `srcnew/Brinell.Uat/UatCatalogBuilder.cs` |
| What the runtime still registers centrally | `UatReflectionRuntime.CreateCommandCatalog()` — **3 page verbs only** |
| Document grammar | `.my/uat/attribute-catalog/01-grammar.md` §1 |
| Public API names | `srcnew/Brinell.Uat/UatDiscovery.cs`, `UatBinder.cs`, `UatCommandCatalog.cs` |
| Which tech targets are real | `testsnew/Brinell.*.Uat.Tests/`, `samples/` |

---

## 2. The four changes that drive every edit

**C1 — The vocabulary is attribute-declared, not centrally registered.**
`[UatStep(keyword, phrase, CommandId = "Control.X")]` sits on the `Brinell.Core`
control interfaces (`IClickableControlObject`, `IEditableTextControlObject`,
`ITextControlObject`, `IToggleControlObject`, `ISelectorControlObject`,
`IElementObject`). `UatCatalogBuilder.RegisterControlVerbs` discovers them into
both the runtime catalog and the spec catalog, with a per-projection prefix
(`Builtin.` / `Spec.`). The Presenter's former third table is gone.

**C2 — The real phrase set is 18, and some `present/` phrases never shipped.**
3 engine-owned page verbs + 15 discovered control verbs. Phrases invented in
`03 runner code binding.md` that do **not** exist: `the application is running`,
`I enter {formName}`, `I select {value} in {control}` (shipped as `from`),
`I should see {value} in {control}`, `I should see these {items}`.
Phrase missing from the `07` and `08` lists: `{control} should not be visible`.

**C3 — The plans are largely executed.** All six tech UAT projects exist
(`Brinell.Maui/Wpf/WinForms/Blazor/Html/Stride/Presenter.Uat.Tests`), eight real
`uat.config.md` files exist, the Presenter app exists. `04` and `06` read as
future work for things that are done.

**C4 — Two new topics have no `present/` doc.** The authoring pipeline
(journeys → Gherkin, LLM as drafter, `.cs` as escape hatch) and UAT for complex
things (multi-user, service validation, DB checks).

---

## 3. Drift inventory

| Doc | Verdict | Drift |
| --- | --- | --- |
| `00 idea overview.md` | **Reframe** | Open questions are all answered; "reflection may be enough" predates C1. |
| `01 uat runner ui design.md` | **Light touch** | UI sketch still valid; superseded in detail by `10`, needs a pointer. |
| *(missing `02`)* | **Add pointer** | `04` Phase 2 cites `02 uat markdown grammar.md`, which does not exist. Grammar now lives in `.my/uat/attribute-catalog/01-grammar.md`. |
| `03 runner code binding.md` | **Heaviest rewrite** | C1 + C2 + wrong API/type names throughout. |
| `04 development roadmap.md` | **Status pass** | C3; Phase 5/7 contradict C1; broken `02` link (line 61). |
| `05 simple page and tests example.md` | **Correct the code** | `UatDiscovery.BuildCatalog(config)` does not exist; invented control types. |
| `05b default naming page example.md` | **Correct the code** | Same API drift; inference table needs checking against `UatNameInference`. |
| `06 mvp task checklist.md` | **Close out** | All boxes ticked; Slice 5 still describes the pre-C1 attribute set. |
| `07 first uat project targets.md` | **Status + phrase list** | MAUI target delivered; command surface incomplete (C2). |
| `08 uat diagnostics and config hardening.md` | **Status + catalog report** | Closest to reality; catalog-report example predates C1; one phrase missing. |
| `09 brinell presenter development plan.md` | **Status pass** | Presenter shipped; `Brinell.Presenter.Uat.Tests` exists. |
| `10 presenter tabbed tree redesign.md` | **Verify only** | Already written as *current* design; check against `srcnew/Brinell.Presenter`. |
| `11 uat for brinell dotnet techs.md` | **Status pass** | Phases 4–8 have shipped projects; phrase list missing `should not be visible`. |

---

## 4. Cross-cutting rules for the whole set

Apply these to every file touched, so the set stops drifting again:

1. **Status header.** Add the `.my/uat` header block to each doc:
   `Status:` / `Date:` / `Area:` / `Related:`. Status is one of
   *Delivered* · *Partially delivered* · *Design, superseded by …* · *Proposal*.
   This is the single highest-value change — a reader currently cannot tell
   which of these twelve docs describes reality.
2. **One phrase table, everywhere else links to it.** `docs/guides/uat-phrases-and-flows.md`
   is the source. Replace every inline phrase list in `present/` with a short
   excerpt plus a link, or delete the list. Do **not** create a second full copy.
3. **Real type names only.** No `ButtonControl` / `TextInputControl` /
   `TapAsync` / `EnterTextAsync`. Use the `Brinell.Core` interfaces and the real
   method names (`Click`, `Enter`, `SetText`, `Clear`, `Check`, `Uncheck`,
   `SelectByText`, `AssertText`, `AssertTextContains`, `AssertVisible`,
   `AssertEnabled`, `AssertChecked`, `AssertSelectedText`).
4. **Keep the history.** Where a design decision was changed rather than
   implemented, keep the original paragraph and add a short
   `> **Superseded (2026-09):** …` note. These are presentation docs; the
   before/after is the story worth telling. Do not silently rewrite history.
5. **Every code block must be checkable.** If a snippet cannot be traced to a
   real file, either fix it or mark it `// illustrative — not the shipped API`.

---

## 5. Work packages

Ordered so the highest-drift, most-cited doc is fixed first and the rest can
reference it.

### WP1 — `03 runner code binding.md` (rewrite the binding model) — ✅ delivered 2026-09-21

The document that C1 invalidates most. Section by section:

- **§First Command Set (line 42)** — replace the 12-row invented table with the
  real 18, split into *3 engine-owned page verbs* and *15 discovered control
  verbs*, or excerpt + link per rule 4.2. Drop the five phrases from C2.
- **§UAT Attributes (line 160)** — add `[UatStep]` as the primary attribute and
  show its real shape (`UatEffectiveStepKeyword`, `HasLiteral`, `Literal`,
  `CommandId` suffix). Keep `[UatName]`, `[UatPhrase]`, `[UatIgnore]`.
  Note `[UatAction]` still exists (`UatDiscovery.cs:93`) but is a capability
  marker, not a phrase source — say so explicitly.
- **§ControlObject Method Attributes (line 191)** — rewrite: attributes live on
  the **Core interfaces**, not on concrete controls. Show
  `IClickableControlObject.Click` with its real `[UatStep]` line.
- **§Assembly Discovery (line 265)** — step 8 ("Add built-in generic commands
  for known capabilities") becomes the `UatCatalogBuilder` discovery pass;
  mention dedup on `(keyword, phrase)`.
- **§Diagnostics / §Minimal Resolved Example** — update `ButtonControl.TapAsync`
  to the real binding, and command ids to the `Builtin.Control.*` form.
- **Add a new short section: "Two projections, one source"** — runtime vs spec
  catalog, why they cannot drift, pointing at AD-011.

**Delivered — notes for whoever picks up WP2–WP5.**

All seven bullets above were done. Five further sections in the same doc turned
out to contradict the shipped API and were corrected under rules 4.3/4.5, beyond
the bullet list written above:

- **§Execution Context** — the sketched `UatExecutionContext` (with `Scenario`,
  `App`, `Pages`, `Controls`, `Diagnostics` registries) is not what shipped. The
  real one holds `CurrentPageName`, `Items` and a `Diagnostics` string list.
- **§Step Invocation** — the real `UatStepInvocation` carries `UatStep` +
  `UatCommandPattern` and projects `MatchedPattern`/`CommandId`; it has no
  `ResolvedPage`/`ResolvedControl`, because resolution happens in the handler.
- **§Validation Rules** — replaced the prose list with the real `UATB001`–`UATB004`
  and `UATD001`–`UATD003` codes.
- **§`uat.config.md`** — the shipped file has `Reporting`, `Settings` and
  `Skip Rules` sections and uses `.dll` paths, not logical assembly names.
- **§Page And Control Names** — the suffix list was missing `Entry`, `Switch`,
  `Display`, `Message` and `Control`; corrected against `UatNameInference`.

Two corrections to this plan's own text, worth carrying into the other WPs:

1. The Core control interfaces are **generic** — `IClickableControlObject<TScope>`,
   `IElementObject<TScope>` and so on. §2/C1 above writes them non-generic.
2. Page objects derive from `PageObjectBase<TSelf>` and expose controls as
   `Button<TPage>` / `Entry<TPage>` constructed with an AutomationId. Rule 4.3
   says "no invented types" but does not give the real shape; use this one.

Verification run: §7.1 and §7.2 greps are clean for this file (the only hits are
inside `> **Superseded**` notes, which both rules permit); all seven relative
links resolve; the phrase counts in the doc match `[UatStep]` in `Brinell.Core`
(1+3+3+2+2+4 = 15) plus the 3 page verbs = 18.

**Not done:** the §8.1 prerequisite. `docs/guides/uat-phrases-and-flows.md` was
*not* edited, so the guide this doc now points at as authoritative still opens by
saying `CreateCommandCatalog()` registers the built-in phrases. Its phrase table
is correct, and `03` links to that table specifically, so nothing in `03` is
wrong — but the prerequisite is still outstanding and should be done before WP2
sends four more docs to the same guide.

### WP2 — Status pass across `04`, `06`, `07`, `09`, `11` — ✅ delivered 2026-09-21

One pass, mechanical, per rule 4.1:

- `04` — add a delivered/not-delivered column or marker per phase. Fix the
  `02 uat markdown grammar.md` link at line 61 → `.my/uat/attribute-catalog/01-grammar.md`.
  Rewrite Phase 5 (attributes) and Phase 7 (built-in commands) for C1.
  Check the Phase 13–15 items (reports, CI, catalog browser) against what
  actually exists before marking them.
- `06` — mark the checklist **closed**; add a closing line pointing at `08` and
  the attribute-catalog folder as what came next. Fix Slice 5's attribute list.
- `07` — mark the MAUI target delivered, naming the real scenarios
  (`main-page-greeting.uat.md`, `main-page-validation.uat.md`,
  `user-form-basic-input.uat.md`). Complete the command surface (C2). Note WPF
  and HTML/Blazor, listed there as "second/third target", now have projects.
- `09` — mark shipped; point at `srcnew/Brinell.Presenter` and
  `testsnew/Brinell.Presenter.Uat.Tests`; note the Presenter now consumes the
  shared projection (`UatSpecCommandCatalog.CreateDefault()`,
  `UatWorkspaceService.cs:29`) instead of its own table.
- `11` — add the missing phrase, and mark the per-tech phases against the
  `testsnew/Brinell.*.Uat.Tests` projects that now exist. Leave the phases that
  are genuinely still open as open.

**Delivered.** All five docs carry status headers and per-phase/per-slice status.
Three things are worth carrying forward:

1. **The §8.1 prerequisite was done first**, not deferred.
   `docs/guides/uat-phrases-and-flows.md` §Built-In Phrases now explains the
   3-registered / 15-discovered split, and its §Source Files lists
   `UatStepAttribute.cs`, the Core interfaces and `UatCatalogBuilder.cs`. The
   guide is now safe for the remaining WPs to cite. **This is the one edit so far
   outside `.my/present`.**
2. **`11` needed far less work than §3 assumed.** It already had an
   "Implemented in the first pass" section and an accurate per-tech coverage
   list — it was written *after* the rollout, not before it. Only the missing
   phrase, the status header, and the two open phases needed touching.
3. **The dangling `02` link is fixed in place**, by pointing Phase 2 of `04` at
   `.my/uat/attribute-catalog/01-grammar.md` with a note on why `02` never
   existed. WP4's "add a stub `02`" item is therefore now optional — it buys
   numbering continuity, not a working link.

**Two status findings that are real work, not documentation:**

- **`11` Phase 9 (cross-tech scenario parity) did not land**, and the doc now says
  so with the evidence: only MAUI and STRIDE share a scenario name, and the four
  baseline names the phase proposed exist in MAUI alone. The vocabulary is
  provably shared, but no two techs run the same scenario, so adapter differences
  do not surface as a comparison.
- **`11` Phase 2's README/template for new tech UAT projects was never written.**
  The pattern is consistent across all seven projects but discoverable only by
  reading one.

**Marked partial, with the reason, rather than delivered:** `04` Phase 13
(per-scenario JSON and artifacts ship; no Markdown report, no export action),
Phase 14 (headless CI runs as xUnit, but no CLI entry point exists — the shape
changed rather than the work being pending) and Phase 15 (the Presenter's
Command Catalog / Discovery tabs and Validate button cover the acceptance checks;
named dry-run and parse-only modes do not exist and are probably obsolete).

Verification run: §7.2 grep clean across all five; §7.3 all 12 distinct relative
links resolve, and no dangling `02` link remains; §7.4 all five carry a status
header.

### WP3 — Fix the example code in `05` and `05b` — ✅ delivered 2026-09-21

- Replace `UatDiscovery.BuildCatalog(config)` with the real
  `UatDiscovery.Discover(...)` + catalog construction, or mark the snippet
  illustrative (rule 4.5).
- Replace invented control types with Core interfaces (rule 4.3).
- `05` §Custom ControlObject: the `[UatAction]`/`[UatPhrase]` pairing is the
  pre-C1 model — show `[UatStep]` for a reusable verb and keep `[UatPhrase]`
  for the genuinely custom `I should see {control} is masked`.
- `05b`: verify the inferred-name table against `UatNameInference` and the
  suffix list in `.my/uat/attribute-catalog/01-grammar.md` §3 before republishing it.

**Delivered.** Both docs carry status headers; every snippet is now traceable to
shipped code, and `testsnew/Brinell.Maui.Uat.Tests` is cited as the live version.

**The dead-API list was longer than §3 recorded.** Beyond `BuildCatalog`, the
test snippets in both docs used `UatConfig.Load`, `ThrowIfFailed()`,
`UatScenarioRunner`, `result.Passed`, `FormatErrors()`, `FormatFailures()`, and
`result.Invocations` with `ResolvedPage` / `ResolvedControl` — none of which
exist. Both test sections were rewritten onto the real base classes:
`UatSpecFormatTestBase` (parse/bind/config assertions) and
`UatScenarioTestBase<TFixture>` (`RunUatFileAsync`, and
`RunExpectedFailureUatFileAsync` for the diagnostics case), each as a `[Theory]`
over `GetScenarioFiles()` rather than a test per file.

**One finding that is conceptual, not cosmetic.** A *binding* test cannot assert
resolved page or control names, which is what the old snippets tried to do. The
binder captures `{control}` as a **string**; the lookup to a page member happens
later, inside the handler, against `CurrentPageName`. So a bind proves the phrase
matched, and only a run proves the name resolved. `05b` now asserts inferred
names directly against `UatNameInference.FromIdentifier` instead, which is
cheaper and more precise.

**Corrections to rule 4.3's model of custom verbs**, which §5 got wrong:

1. **A custom *control* verb uses `[UatStep]`, not `[UatPhrase]`.** `[UatPhrase]`
   is discovered on root types, page types and `[UatPhraseClass]` types only —
   never on controls. WP3's instruction above ("show `[UatStep]` for a reusable
   verb and keep `[UatPhrase]` for the custom masked assertion") was therefore
   backwards for the masked assertion; it is now `[UatStep]`.
2. **A control that derives from a Brinell control inherits the built-in phrases**
   and must not re-declare them — `05`'s `[UatAction]`+`[UatPhrase]` pair on
   `EnterPassword` would have been a duplicate phrase (`UATD003`).
3. **`[UatPhrase]` has a keyword overload**, `[UatPhrase(UatEffectiveStepKeyword.When, "…")]`,
   and every real usage in the repo passes it. The one-arg form is valid but
   registers the phrase for all keywords.

**Two small fixes carried back into `03`** (WP1's file), since WP3 verified the
attribute source: `[UatIgnore]`'s real targets are `Class | Property | Method`,
and the three one-arg `[UatPhrase]` examples now use the keyword overload. The
page-object `AssertOpenAsync` example was dropped — `I should be on the {page}
page` is an engine verb, so no page needs its own open-assertion phrase.

**`05b`'s inferred names were correct and are now explained.** `StripSuffix`
strips exactly one suffix, first match in list order wins, and only when
something would remain — which is why `StatusMessageLabel` lands on
`Status Message` rather than `Status`, though `Message` is itself a known suffix.

Verification run: §7.2 grep clean across both (remaining hits are inside
`> **Superseded**` notes); all six distinct relative links resolve; both carry a
status header; every API introduced was checked against source
(`GetScenarioFiles` defaults to `"Scenarios"`,
`RunExpectedFailureUatFileAsync(filePath, params string[])`,
`UatRuntimeValidationOptions(Target:, Fixture:)`).

### WP4 — `08` and the grammar gap — ✅ delivered 2026-09-21

- `08` — mark the hardening slice delivered; `uat.config.md` is real in eight
  places. Update the §Command Catalog Report example to show that ids now come
  from `[UatStep]` with a projection prefix. Add `{control} should not be visible`.
- Add **`02 uat markdown grammar.md`** as a thin doc that states the grammar is
  now formalised and links to `.my/uat/attribute-catalog/01-grammar.md`
  (§1 document grammar, §2 phrase mini-language, §3 lexicon). Do not duplicate
  the EBNF. This closes the dangling reference and restores the numbering.

**Delivered.** `02` exists as a pointer (no EBNF copied), and `04` Phase 2's note
now links to it. The numbering runs 00–11 with no gap.

**`08` needed the least correction of any doc in the set** — worth recording,
because it is the one that was written *after* something shipped rather than
before. Both report formats in it match the shipped output line for line:
`DescribeDiscovery()` prints `- {Page}: {controls}` sorted as the example shows,
and `FormatCatalog` prints `- {Keyword}: {Phrase} -> {CommandId}`. The
expected-failure file exists at exactly the proposed path. Only three things
needed adding: the missing phrase, where command ids now come from, and closing
the Done Definition.

**The one substantive change** is in §Command Catalog Report. The report's *shape*
is unchanged, but `Builtin.Control.Tap` is no longer a string typed into a table —
it is assembled at discovery from the `[UatStep]` suffix plus the projection
prefix. So the report a user reads is generated from the same attributes that
implement the behaviour and cannot describe a phrase the engine lacks. `08` now
says this.

**Two precision fixes made while writing `02`**, after checking the source:
parser codes run `UAT000`–`UAT022` (not from `UAT001`), and `03`
§Validation Rules tabulates only the `UATB*`/`UATD*` codes — the parser codes
live in `UatMarkdownParser.cs`, so `02` points there instead of overclaiming.

`02` also records three things a reader needs before opening the full grammar:
`And`/`But` inherit the previous effective keyword (which is why `[UatStep]` takes
a `UatEffectiveStepKeyword` and an `And` phrase cannot be declared), step lines
must be **bare** — no fence, no list marker — and parse errors are line-numbered
and coded.

Verification run: all five distinct relative links in `02` and `08` resolve; both
carry status headers; no dangling `02` reference remains anywhere in the set.

### WP5 — Two new docs for the two new topics (C4)

- **`12 authoring pipeline.md`** — condense `rethinking-uat.md` for this
  audience: journeys → Gherkin → run directly; the interpret-vs-transpile table;
  LLM as drafting accelerator; `.cs` as reviewed escape hatch, never in CI;
  traceability (`TOD.xx.y` → scenario → green/red). Point at `.my/uat/todos/`
  as the worked example.
- **`13 uat for complex scenarios.md`** — condense `.my/uat/complex/`: the
  "drive through the UI, verify out of band" principle, and the four backings
  (multi-user via `MockApiServer`, service-call assertions, database checks,
  clock/network). This is the answer to the obvious "but can it test *real*
  systems?" question the current set cannot answer.

---

## 6. Suggested order and rough size

| Step | Work package | Size |
| --- | --- | --- |
| 1 | WP1 — `03` rewrite | Large — the substantive one |
| 2 | WP4 — `08` + new `02` | Small |
| 3 | WP2 — status pass over five docs | Medium, mechanical |
| 4 | WP3 — example code in `05`/`05b` | Medium |
| 5 | WP5 — two new docs | Medium |
| 6 | Final consistency sweep (§7) | Small |

WP1 and WP4 can be done independently; WP2 and WP3 are easier once WP1 has
fixed the vocabulary once and the rest can link to it.

---

## 7. Verification

Documentation-only, so the checks are greps and link walks, not builds:

1. **No invented phrases remain.** Every phrase string in `.my/present/*.md`
   appears in `docs/guides/uat-phrases-and-flows.md` §Built-In Phrases, or is
   explicitly labelled as a custom `[UatPhrase]` example.
2. **No invented types remain.** `grep -n "ButtonControl\|TextInputControl\|TapAsync\|EnterTextAsync\|BuildCatalog" .my/present/*.md`
   returns nothing outside an explicitly illustrative block.
3. **No dangling internal links.** Walk every relative link, including the
   `02 uat markdown grammar.md` reference.
4. **Every doc has a status header** (rule 4.1).
5. **Phrase count agrees**: the count stated in `present/` equals the number of
   `[UatStep]` attributes in `srcnew/Brinell.Core` plus the 3 page verbs in
   `UatReflectionRuntime.CreateCommandCatalog()`.

---

## 8. Out of scope (but worth recording)

These came up while surveying and are **not** part of this doc plan:

1. **`docs/guides/uat-phrases-and-flows.md` is itself mildly stale** — its
   §Built-In Phrases still says "`UatReflectionRuntime.CreateCommandCatalog()`
   registers the built-in runtime phrases", which is true of only 3 of the 18
   since C1, and §Source Files does not list `UatCatalogBuilder.cs` or
   `Brinell.Core/Testing/UatStepAttribute.cs`. Since rule 4.2 makes that guide
   the single source the `present/` set links to, **fixing it is a prerequisite
   for WP1** — small edit, different repo area, worth doing first.
2. **The open follow-up from the implementation review** — no test pins the
   expected built-in vocabulary, so deleting a `[UatStep]` shrinks both
   projections silently and stays green
   ([05-implementation-review.md](../../uat/attribute-catalog/05-implementation-review.md)
   Finding 1). That is a code change, not a doc change.
3. **Wiring `.my/uat/todos/scenarios/*.uat.md` to a runnable Todo head** —
   review follow-up 2. Affects what `12 authoring pipeline.md` can claim as a
   worked example; check its state before writing that doc.
