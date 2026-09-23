# 03 — Migration, back-compat, risks

The goal is to reach the attribute-driven catalog **without changing a single
`.uat.md` file** and with every existing UAT test green at each step. The two
hand-written tables are deleted last, only once a test proves the discovered
catalog is identical to them.

---

## Staged rollout

### Stage 0 — Pin the current behaviour

Add a characterisation test that snapshots the catalog as it is today: every
`(keyword, phrase, commandId)` triple from both
`UatReflectionRuntime.CreateCommandCatalog()` and
`UatSpecCommandCatalog.RegisterDefault()`. This snapshot is the oracle for every
later stage.

```csharp
[Fact]
public void Builtin_catalog_matches_snapshot()
{
    var runtime = /* runtime over a minimal root */;
    var triples = runtime.CreateCommandCatalog().Patterns
        .Select(p => (p.Keyword, p.Phrase, p.CommandId))
        .OrderBy(t => t.ToString());

    Assert.Equal(ExpectedBuiltinTriples, triples); // committed snapshot
}
```

### Stage 1 — Introduce the attribute and the builder, additively

- Add `UatStepAttribute` ([02-attribute-catalog.md](02-attribute-catalog.md) §1).
- Add `UatCatalogBuilder` with the discovery algorithm.
- Decorate the control base types (`ControlObject`, `Entry`, `Picker`, ...) with
  `[UatStep]` for the 15 control verbs.
- Do **not** yet remove the hardcoded rows. Instead, have the builder run and
  assert (in a test) that its discovered set equals the Stage 0 snapshot. This
  proves the attributes are complete and correct while the old table still
  drives production.

### Stage 2 — Switch the runtime catalog to the builder

Replace the 15 control-verb `Register` calls in `CreateCommandCatalog()` with a
single `UatCatalogBuilder.RegisterControlVerbs(catalog, controlTypes)`. Keep the
three page-graph verbs (`{page}` / `I should see {text}`) as explicit handler
registrations — they are not control methods. Run the full `Brinell.Uat.Tests`
suite; the Stage 0 snapshot test must stay green.

### Stage 3 — Switch the spec catalog to the builder

Replace the body of `UatSpecCommandCatalog.RegisterDefault()` with
`UatCatalogBuilder.RegisterPhrasesOnly(...)`. Add the "two projections agree"
test from [02-attribute-catalog.md](02-attribute-catalog.md) §3.

### Stage 4 — Delete the snapshots' redundancy

Once Stages 2–3 are green, the Stage 0 snapshot is still useful as a
regression guard but no longer describes hand-written code. Keep it; it is cheap
and it is the thing that catches an accidental phrase change.

---

## Back-compat guarantees

| Surface | Guarantee |
| --- | --- |
| `.uat.md` files | Unchanged. Same phrases, same tags, same tables. |
| `[UatPhrase]` custom phrases | Unchanged. Discovered exactly as today. |
| `[UatName]` / inference | Unchanged. Still the lexicon (layer 3). |
| `UatSpecCommandCatalog.CreateDefault()` | Same public signature; body now projected from attributes. |
| `CreateCommandCatalog()` | Same public signature and returned phrases. |

Per the workspace preference, **no compatibility shim is kept beyond what is
needed for the staged rollout** — once Stage 3 lands, the hand-written rows are
gone, not deprecated.

---

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| A control verb is missed when decorating base types | Stage 1 equality test against the Stage 0 snapshot fails loudly until every phrase is accounted for. |
| A phrase is declared on a base type and double-registers via every subclass | Builder de-dups on `(keyword, phrase)` exactly like the current root-phrase guard. |
| Literal variants (`visible` / `not visible`) bind ambiguously | Unchanged from today: specificity ranking already disambiguates; covered by existing binder tests. |
| Trimming / AOT: reflection over control methods | Discovery already reflects over control *properties*; extending to attributed *methods* is the same trust boundary. Annotate the control base types with `[DynamicallyAccessedMembers]` if a trimmed head is targeted. |
| Third-party / vendor controls without source | They can still be given phrases via a `[UatPhrase]` phrase class or a thin wrapper control — the escape hatch is unchanged. |
| Command-id churn breaks a test asserting a specific id | Ids default to `Type.Method`; where a test pins the old `Builtin.Control.SetText` string, set `[UatStep(..., CommandId = "Builtin.Control.SetText")]` to preserve it. |

---

## Test matrix

- `Brinell.Uat.Tests` — parser, binder, catalog. Add: snapshot equality (Stage
  0), discovered-equals-hardcoded (Stage 1), two-projections-agree (Stage 3).
- One MAUI UAT scenario run end to end (e.g. the Todo "needs a title" scenario)
  to prove the discovered catalog drives a real app identically. Use the
  smallest UI tier, not the full suite (per AGENTS.md).

Verification commands (from the Brinell root):

```powershell
dotnet build srcnew\Brinell.sln -v:minimal /nr:false
dotnet test testsnew\Brinell.Uat.Tests\Brinell.Uat.Tests.csproj -v:minimal /nr:false
```

---

## Decision to record if adopted

If this ships, record one decision in `.docs/` (per the decision-record skill):

> The built-in UAT step vocabulary is declared by `[UatStep]` attributes on
> control-object methods and discovered into the catalog; it is **not**
> hand-registered. Page-graph verbs (`{page}`, `I should see`) are the only
> centrally registered phrases. The spec catalog and the runtime catalog are two
> projections of the same attribute set and must not diverge.

That closes the door on re-growing a hand-written phrase table — which is the
whole reason for the change.
