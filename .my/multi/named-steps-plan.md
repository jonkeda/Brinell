# Named users in UAT steps, declared rather than inferred: plan

Status: **done** 2026-09-27 (section 5). Replaces D1 of [plan.md](plan.md) (the verb rewrite) and
amends AD-015.

## 1. The change

Today `Daan taps Save` binds because the engine **infers** it: it removes the subject, tries
first-person forms of the verb (`taps`, `tap`, `tape`...), and looks for a matching `I ...`
phrase (`UatSubjects.FirstPersonCandidates`, `UatBinder`). This plan replaces that inference with
a **declaration**. A phrase that can be said of a named user says so, with a second keyword
attribute whose phrase starts with `{user}`:

```csharp
[UatWhen("I tap {control}", CommandId = "Control.Tap")]
[UatWhen("{user} taps {control}", CommandId = "Control.Tap")]
void Click(int? timeoutMs = null);
```

The engine then does no grammar at all. A step binds to exactly the text some attribute
declares. `{user}` in first place already makes that user's app current (plan.md D3), so the
switching, stickiness, the `Apps` table and the catalogs per head stay as they are.

**Why:**

- **What you read is what binds.** The phrase list (Presenter, discovery report, the catalog in
  `__result.json`) shows `{user} taps {control}`. Nobody has to know what the engine will rewrite.
- **No wrong guesses.** Candidate verbs could bind a step the author did not mean. The author
  now writes the one form, including irregular ones (`{user} is on the {page} page`,
  `{user} has ...`), without an irregular-verb table.
- **The author decides which phrases a user can say.** A phrase with no `{user}` form cannot be
  said of a named user, which is a design choice rather than an accident of the grammar.
- **Other languages.** A Dutch vocabulary (`{user} tikt op {control}`) works the same way. The
  English rewrite could never do that.

**Cost:** each phrase that should have a named form needs a second attribute. That is about 20
in `Brinell.Core` plus the app's own. AD-011's "declared once" still holds, because each phrase
text is declared once, and one method carries two phrases (`AllowMultiple` is already true).

## 2. Decisions

| # | Decision |
| --- | --- |
| N1 | **No inference.** `UatSubjects` and the candidate search go. A step starting with a name binds only to a phrase declared with `{user}` (or `{app}`) in first place. |
| N2 | **A named form is a second keyword attribute** (`[UatGiven]`, `[UatWhen]`, `[UatThen]`) on the same method, starting with `{user}`. It shares the method, and for a control verb its `CommandId` and `Literal`. No new attribute type. |
| N3 | **`{user}` is not an argument of the method.** For control verbs, `UatCatalogBuilder.ResolveValueArgument` skips `user` as it skips `control`. For phrase methods, `user` is passed only when the method has a `user` parameter. |
| N4 | **The built-in page steps get named forms** in `UatReflectionRuntime.CreateCommandCatalog`: `{user} is on the {page} page` (Given), `{user} should be on the {page} page` (Then), `{user} should see {text}` (Then). The spec catalog gets the same. |
| N5 | **Which Core verbs get a named form:** every `I ...` phrase in `Brinell.Core/Interfaces` (tap, enter, clear, set, select, check, uncheck). The `{control} should ...` assertions have no actor and get none. |
| N6 | **A helpful miss.** A step that starts with a name and binds to nothing fails with `UATB001`, and the message lists the declared `{user} ...` phrases of that keyword. It does not guess a verb. |
| N7 | **A pair check, as a test, not as engine behaviour.** `UatCatalogParityTests` (or a new test) asserts that every Core `I ...` control verb has a `{user}` twin with the same `CommandId`, so the built-in pairs cannot drift. Apps may leave phrases without a twin on purpose. |
| N8 | **Everything else in plan.md stays:** subjects from the `Apps` table only, a subject that sticks, a user with several apps, `UATB005`-`UATB007`, and the catalog per head. |

## 3. Steps

| # | Step | Verification |
| --- | --- | --- |
| 1 | `UatCatalogBuilder`: `user` is never the value argument (N3). Phrase methods receive `user` only when they ask for it. | `UatCatalogParityTests`: a `{user} sets {control} to {value}` twin passes the value, not the user. |
| 2 | Core: the `{user}` twins on the interfaces in `Brinell.Core/Interfaces` (N5). Built-in page steps (N4) in the runtime and spec catalogs. | Parity tests; the pair check (N7). |
| 3 | `UatBinder`: remove the candidate search; keep "first parameter equals the subject" as the only subject path; the N6 message. Delete `UatSubjects`. | `UatSubjectTests` rewritten: each named form binds through its twin and switches the app; `Eva frobnicates` fails with the declared named phrases listed; no `advanc`/`press`/`try` rules left to test. `UatSessionTests`: `Bob notes the app` needs a `{user} notes the app` twin on the test phrase. |
| 4 | Samples: twins for the app-specific `I ...` phrases the scenarios use by name (the Todo suites' `TodoScreenPhrases`, `UatSessionTests` phrases). | Todo web UAT 33 (TOD.07.3 uses `Alice taps Add`, `Bob taps Sync`); mixed UAT 3 + 1 skip; Todo MAUI UAT 32. |
| 5 | Docs: AD-015 rewritten (the named form is declared; ruled out: inferring the verb). `uat-phrases-and-flows.md` "Several Users And Apps" (a table of `I` / `{user}` pairs instead of the rewrite rules). `invariants.md` line. CHANGELOG. plan.md D1 marked replaced. | Links valid. |

## 4. Open questions

| # | Question | Proposal |
| --- | --- | --- |
| Q1 | One attribute per form, or one attribute with both (`[UatWhen("I tap {control}", AsUser = "{user} taps {control}")]`)? | Two attributes, as asked: each form is a keyword attribute that reads the same as every other, and the discovery pass needs no new property. The property form keeps a pair on one line, and can be added later if the pairs drift in practice. |
| Q2 | Should the engine warn when an app's `I ...` phrase has no `{user}` twin? | No. That is the app's choice (N7). A scenario that needs it fails at bind time with the declared named phrases listed (N6). |
| Q3 | Should `{app}` twins exist too (`Daan's PC taps Save`)? | No: `{user}` accepts an app name already (it resolves through the `Apps` table), so one twin covers both. |

## 5. Progress

| Step | Status | Evidence |
| --- | --- | --- |
| 1 | Done | `UatCatalogBuilder.ResolveValueArgument` skips `user`. Phrase methods already received only the arguments they declare. Test: `ANamedTwin_PassesTheValue_NotTheUser` runs `Eva sets the level of Gadget to "7"` and the control gets `7`. |
| 2 | Done | Twins on the 7 Core `I ...` verbs (`IClickable`, `IEditableText` x3, `ISelector`, `IToggle` x2) and the 3 page steps in the runtime and spec catalogs. Test: `EveryCoreIVerb_HasANamedTwin_WithTheSameCommand`. |
| 3 | Done | `UatSubjects` deleted; the binder binds a subject only through a declared `{user}`/`{app}` phrase and lists the declared named phrases when none matches. **Addition:** a `{user}` step in a suite without an `Apps` table is `UATB007` (otherwise `Dan taps OK` would bind and ignore Dan). `UatSubjectTests` rewritten. Uat.Tests 172 passed. |
| 4 | Done | No sample needed an app-specific twin: the scenarios name users only with Core verbs and page steps, and the mixed phrases already start with `{user}`. Test phrases in `UatSessionTests` and `UatAppsHostTests` got `{user} notes the app`. Todo web UAT 33, mixed UAT 4 (3 + the Android skip), MAUI UAT 32. |
| 5 | Done | AD-015 rewritten; `invariants.md`; `uat-phrases-and-flows.md` (the pair table); `uat-template-guide.md`; CHANGELOG; plan.md D1 marked replaced. Core 27, Presenter.Uat 49, Html 39, Blazor 173, Maui 186 (1 skipped); `Brinell.sln` builds. |
